using System.Net;
using McpSdk.Adapter.StreamableHttpServer;
using McpSdk.Adapter.System.Text.Json;
using McpSdk.Protocol;
using McpSdk.Protocol.Models;
using McpSdk.Server;
using McpSdk.Shared;
using ZGF.Gui.Desktop.Automation;

namespace ZGF.Gui.Desktop;

/// <summary>
/// An in-process Model Context Protocol server (Streamable HTTP, bound to 127.0.0.1) through which an
/// MCP client — an LLM, an agent, a script — reaches the running app. What it serves comes from
/// <see cref="McpServerOptions"/>: the app's own tool sources and prompts, model-facing instructions,
/// and optionally the framework's <c>gui_*</c> debug tools, which read the laid-out view trees of every
/// live window, inject mouse/keyboard input, and capture screenshots. The <c>gui_*</c> tools marshal
/// onto the UI thread and block for the result; app tools run where the transport calls them and may
/// be genuinely asynchronous. A constructed server is listening; <see cref="Dispose"/> ends every open
/// session and stops it.
/// </summary>
public sealed class GuiMcpServer : IDisposable
{
    private readonly McpServerOptions _options;
    private readonly GuiDriver _driver;
    private readonly SystemJson _json = new();
    private readonly McpConsoleLogger _loggerFactory = new();
    private readonly StreamableHttpListener _listener;

    private readonly object _sessionsGate = new();
    private readonly HashSet<HttpServerTransport> _sessions = new();
    private bool _disposed;

    /// <summary>Binds <see cref="McpServerOptions.Port"/> and starts accepting sessions. Throws
    /// <see cref="HttpListenerException"/> when the port cannot be bound.</summary>
    public GuiMcpServer(McpServerOptions options, GuiDriver driver)
    {
        _options = options;
        _driver = driver;
        var baseUrl = $"http://127.0.0.1:{options.Port}";
        var path = options.PathToken is { } token ? $"/mcp/{token.Value}" : "/mcp";
        Endpoint = new Uri(baseUrl + path);
        _listener = new StreamableHttpListener(baseUrl, path, _json, _loggerFactory, OnSession);
        // The listener binds synchronously and hands its accept loop to the thread pool, so this
        // returns as soon as the port is ours — or throws because it is not.
        _listener.Start().GetAwaiter().GetResult();
    }

    /// <summary>The URL a client connects to — <c>http://127.0.0.1:{port}/mcp</c>, with the path
    /// token appended when one is set.</summary>
    public Uri Endpoint { get; }

    private async Task OnSession(ITransport transport)
    {
        var http = SessionTransport(transport);
        var session = new McpSession(http.SessionId, http.Lifetime);
        Track(http);

        var builder = new ServerBuilder()
            .WithName(_options.ServerName)
            .WithVersion("1.0.0")
            .WithLogger(_loggerFactory)
            .WithStreamableHttpTransport(transport)
            .WithDefaultToolsCapability(_json, tools => RegisterTools(session, tools));
        if (_options.Instructions is { } instructions) builder.WithInstructions(instructions);
        if (_options.Prompts is { } prompts) builder.WithPromptsCapability(prompts);
        await builder.Build().Start();
    }

    // The listener's contract is that every session transport is an HttpServerTransport — the type
    // that carries the session id and the teardown token — but its callback is typed as the
    // transport interface. This is the one place that narrows it, and it fails loudly rather than
    // serve a session whose end nothing could observe.
    private static HttpServerTransport SessionTransport(ITransport transport) =>
        transport as HttpServerTransport
        ?? throw new InvalidOperationException($"Expected an {nameof(HttpServerTransport)} session, got {transport.GetType().Name}.");

    private void Track(HttpServerTransport http)
    {
        lock (_sessionsGate)
        {
            if (!_disposed)
            {
                _sessions.Add(http);
                http.Lifetime.Register(() => { lock (_sessionsGate) _sessions.Remove(http); });
                return;
            }
        }
        // Raced with Dispose: the listener no longer accepts, but this session got in first.
        _ = http.Stop();
    }

    private void RegisterTools(McpSession session, DefaultToolsController tools)
    {
        if (_options.IncludeGuiTools) RegisterGuiTools(tools);
        foreach (var source in _options.ToolSources)
            source.Register(session, tools);
    }

    // ---- gui_* debug tools ----

    private void RegisterGuiTools(DefaultToolsController tools)
    {
        tools.AddTool(Def(
            "gui_snapshot",
            "Read the laid-out view tree of every live window — the main window plus any open context menu, tooltip, or secondary window — each under a \"=== window: ROLE [x,y wxh] ===\" header. Call this first to discover what is on screen (incl. context menus, which are separate windows) and how to target it.",
            new ObjectSchema().AddOption("format", new StringSchema
            {
                Description = "\"text\" (default, compact and human-readable) or \"json\" (machine-readable).",
                Options = ["text", "json"],
            }),
            readOnly: true,
            (a, _) => Run(() => Text(Snapshot(EqualsIc(Str(a, "format"), "json"))))));

        tools.AddTool(Def(
            "gui_screenshot",
            "Capture a PNG screenshot of a rendered window. Defaults to the topmost open context menu if one is open, else the main window; pass \"window\" (a role like \"context-menu\" or \"main\", or a snapshot window index) to target another. Returns image content, or an error if the active render backend cannot read back its framebuffer.",
            new ObjectSchema()
                .AddOption("window", new StringSchema { Description = "Window to capture: a role (\"main\", \"context-menu\", \"tooltip\", \"secondary\") or a snapshot window index. Defaults to the topmost context menu, else main." }),
            readOnly: true,
            (a, _) => Run(() => Image(Screenshot(Str(a, "window"))))));

        tools.AddTool(Def(
            "gui_click",
            "Click a view in any live window (searches open context menus first, then secondary windows, then main). Target it by \"id\", \"label\", or \"text\", or click absolute GUI coordinates with \"x\" and \"y\" (in the \"window\" you name, default main).",
            new ObjectSchema()
                .AddOption("id", new StringSchema { Description = "View id to click." })
                .AddOption("label", new StringSchema { Description = "Clickable label / accessible name to match." })
                .AddOption("text", new StringSchema { Description = "Visible text to match." })
                .AddOption("exact", new BooleanSchema { Description = "Match id/label/text exactly (default true); false matches substrings." })
                .AddOption("button", new StringSchema { Description = "Mouse button (default left).", Options = ["left", "right", "middle"] })
                .AddOption("x", new NumberSchema { Description = "Absolute GUI x (use with y to click a coordinate)." })
                .AddOption("y", new NumberSchema { Description = "Absolute GUI y (use with x to click a coordinate)." })
                .AddOption("window", new StringSchema { Description = "For x/y clicks, which window's coordinate space: a role or snapshot index (default \"main\")." }),
            readOnly: false,
            (a, _) => Run(() => Text(Click(
                Str(a, "id"), Str(a, "label"), Str(a, "text"),
                Bool(a, "exact", true), Num(a, "x"), Num(a, "y"), Str(a, "button"), Str(a, "window"))))));

        tools.AddTool(Def(
            "gui_move",
            "Move the pointer without clicking, so hover behaviour can be driven: tooltips, hover cards, cursor changes. Target a view by \"id\", \"label\" or \"text\", or absolute GUI coordinates with \"x\" and \"y\". Anything that waits for the pointer to settle needs a moment after this before it appears.",
            new ObjectSchema()
                .AddOption("id", new StringSchema { Description = "View id to move onto." })
                .AddOption("label", new StringSchema { Description = "Clickable label / accessible name to match." })
                .AddOption("text", new StringSchema { Description = "Visible text to match." })
                .AddOption("exact", new BooleanSchema { Description = "Match id/label/text exactly (default true); false matches substrings." })
                .AddOption("x", new NumberSchema { Description = "Absolute GUI x (use with y)." })
                .AddOption("y", new NumberSchema { Description = "Absolute GUI y (use with x)." })
                .AddOption("window", new StringSchema { Description = "For x/y moves, which window's coordinate space: a role or snapshot index (default \"main\")." }),
            readOnly: false,
            (a, _) => Run(() => Text(Move(
                Num(a, "x"), Num(a, "y"), Str(a, "id"), Str(a, "label"), Str(a, "text"),
                Bool(a, "exact", true), Str(a, "window"))))));

        tools.AddTool(Def(
            "gui_type",
            "Type ASCII text into the focused view, one key at a time. Use gui_key for non-printable keys (Enter, Tab, ...).",
            new ObjectSchema().Add("text", new StringSchema { Description = "ASCII text to type." }),
            readOnly: false,
            (a, _) => Run(() => Text(Type(Str(a, "text") ?? throw Required("text"))))));

        tools.AddTool(Def(
            "gui_key",
            "Press a single key, optionally with modifiers — e.g. Enter, Escape, Tab, or A with Control.",
            new ObjectSchema()
                .Add("key", new StringSchema { Description = "Key name (e.g. Enter, Escape, Tab, A, Left)." })
                .AddOption("mods", new StringSchema { Description = "Comma-separated modifiers (e.g. \"Control,Shift\")." })
                .AddOption("action", new StringSchema { Description = "press (default), down, or up.", Options = ["press", "down", "up"] }),
            readOnly: false,
            (a, _) => Run(() => Text(Key(Str(a, "key") ?? throw Required("key"), Str(a, "mods"), Str(a, "action"))))));
    }

    // ---- actions ----
    //
    // Every tool is a thin adapter over GuiDriver: parse the MCP arguments, hand them over, wrap what
    // comes back. The driver owns view resolution, input injection and UI-thread marshaling, so an
    // MCP-driven run and a scripted one go down exactly the same path.

    private string Snapshot(bool asJson) => _driver.Snapshot(asJson);

    private string Click(string? id, string? label, string? text, bool exact, float? x, float? y, string? button, string? window) =>
        _driver.ClickTool(id, label, text, exact, x, y, button, window);

    private string Move(float? x, float? y, string? id, string? label, string? text, bool exact, string? window) =>
        _driver.MoveTool(x, y, id, label, text, exact, window);

    private string Type(string text) => _driver.TypeTool(text);

    private string Key(string keyName, string? mods, string? action) => _driver.KeyTool(keyName, mods, action);

    private byte[] Screenshot(string? window) => _driver.ScreenshotTool(window);

    // ---- MCP tool plumbing ----

    private static IToolHandler Def(string name, string description, ObjectSchema schema, bool readOnly,
        Func<IJsonObject, McpRequestContext, Task<CallToolResult>> call)
    {
        var tool = new Tool(name, description, schema);
        if (readOnly) tool.Annotations = new ToolAnnotations { ReadOnlyHint = true };
        return new DelegateTool(tool, call);
    }

    private static Task<CallToolResult> Run(Func<CallToolResult> body)
    {
        try { return Task.FromResult(body()); }
        catch (Exception ex) { return Task.FromResult(CallToolResult.Error(new TextContent(ex.Message))); }
    }

    private static CallToolResult Text(string text) => CallToolResult.Ok(new TextContent(text));
    private static CallToolResult Image(byte[] png) => CallToolResult.Ok([new ImageContent("image/png", png)]);

    private static Exception Required(string name) => new InvalidOperationException($"'{name}' is required.");

    private static string? Str(IJsonObject? args, string key)
    {
        if (args is null) return null;
        foreach (var kv in args)
            if (EqualsIc(kv.Key, key) && kv.Value.IsString)
                return kv.Value.AsString();
        return null;
    }

    private static float? Num(IJsonObject? args, string key)
    {
        if (args is null) return null;
        foreach (var kv in args)
            if (EqualsIc(kv.Key, key))
                return kv.Value.AsFloat();
        return null;
    }

    private static bool Bool(IJsonObject? args, string key, bool fallback)
    {
        if (args is null) return fallback;
        foreach (var kv in args)
            if (EqualsIc(kv.Key, key))
                return kv.Value.AsBool();
        return fallback;
    }

    private static bool EqualsIc(string? a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private sealed class DelegateTool : IToolHandler
    {
        private readonly Func<IJsonObject, McpRequestContext, Task<CallToolResult>> _call;
        public DelegateTool(Tool tool, Func<IJsonObject, McpRequestContext, Task<CallToolResult>> call)
        {
            Tool = tool;
            _call = call;
        }

        public Tool Tool { get; }
        public Task<CallToolResult> Call(IJsonObject arguments, McpRequestContext context) => _call(arguments, context);
    }

    /// <summary>Stops listening and ends every open session, so a tool waiting on a person sees its
    /// <see cref="McpSession.Ended"/> fire instead of waiting for a client that will never answer.</summary>
    public void Dispose()
    {
        List<HttpServerTransport> sessions;
        lock (_sessionsGate)
        {
            if (_disposed) return;
            _disposed = true;
            sessions = [.. _sessions];
            _sessions.Clear();
        }
        _listener.Stop().GetAwaiter().GetResult();
        foreach (var session in sessions)
            session.Stop().GetAwaiter().GetResult();
    }
}

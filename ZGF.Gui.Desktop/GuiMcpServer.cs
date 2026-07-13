using System.Text;
using McpSdk.Adapter.StreamableHttpServer;
using McpSdk.Adapter.System.Text.Json;
using McpSdk.Protocol;
using McpSdk.Protocol.Models;
using McpSdk.Server;
using McpSdk.Shared;
using ZGF.Gui.Desktop.Automation;

namespace ZGF.Gui.Desktop;

/// <summary>An in-process Model Context Protocol server (Streamable HTTP) that lets an MCP client —
/// an LLM, an agent, a script — drive the live app across every window: read the laid-out view trees
/// (main window plus open context menus, tooltips, and secondary windows), inject mouse/keyboard
/// input into the right window, and grab a per-window screenshot. Each tool marshals its work onto
/// the UI thread via the <see cref="IUiDispatcher"/> and blocks for the result, so nothing races the
/// renderer. Debug aid only — bound to 127.0.0.1 and opt-in (<c>GuiAppBuilder.UseMcpServer</c> or the
/// <c>ZGF_GUI_MCP</c> env var).</summary>
public sealed class GuiMcpServer : IDisposable
{
    private readonly GuiDriver _driver;

    private StreamableHttpListener? _listener;
    private Thread? _thread;

    public GuiMcpServer(GuiDriver driver) => _driver = driver;

    public void Start(int port)
    {
        var json = new SystemJson();
        var loggerFactory = new NullLoggerFactory();
        var baseUrl = $"http://127.0.0.1:{port}";
        var listener = new StreamableHttpListener(baseUrl, "/mcp", json, loggerFactory, onSession: async transport =>
        {
            var server = new ServerBuilder()
                .WithName("ZGF GUI")
                .WithVersion("1.0.0")
                .WithLogger(loggerFactory)
                .WithStreamableHttpTransport(transport)
                .WithDefaultToolsCapability(json, RegisterTools)
                .Build();
            await server.Start();
        });
        _listener = listener;
        _thread = new Thread(() =>
        {
            try { listener.Start().GetAwaiter().GetResult(); }
            catch (Exception ex) { Console.WriteLine($"[GuiMcpServer] listener stopped: {ex.Message}"); }
        }) { IsBackground = true, Name = "ZGF-GuiMcpServer" };
        _thread.Start();
        Console.WriteLine($"[GuiMcpServer] MCP (Streamable HTTP) listening on {baseUrl}/mcp  (tools: gui_snapshot, gui_screenshot, gui_click, gui_type, gui_key)");
    }

    // ---- tool registration ----

    private void RegisterTools(DefaultToolsController tools)
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

    public void Dispose()
    {
        var listener = _listener;
        _listener = null;
        try { _ = listener?.Stop(); } catch { /* already gone */ }
    }
}

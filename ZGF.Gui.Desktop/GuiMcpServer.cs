using McpSdk.Adapter.StreamableHttpServer;
using McpSdk.Adapter.System.Text.Json;
using McpSdk.Protocol;
using McpSdk.Protocol.Models;
using McpSdk.Server;
using McpSdk.Shared;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Inspection;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Desktop;

/// <summary>An in-process Model Context Protocol server (Streamable HTTP) that lets an MCP client —
/// an LLM, an agent, a script — drive the live main window: read the laid-out view tree, inject
/// mouse/keyboard input, and grab a screenshot. Each tool marshals its work onto the UI thread via
/// the <see cref="IUiDispatcher"/> and blocks for the result, so nothing races the renderer. Debug
/// aid only — bound to 127.0.0.1 and opt-in (<c>GuiAppBuilder.UseMcpServer</c> or the
/// <c>ZGF_GUI_MCP</c> env var).</summary>
public sealed class GuiMcpServer : IDisposable
{
    private readonly Func<View?> _getRoot;
    private readonly DesktopInputSystem _input;
    private readonly IUiDispatcher _dispatcher;
    private readonly Action<string, Action?> _captureScreenshot;

    private StreamableHttpListener? _listener;
    private Thread? _thread;

    public GuiMcpServer(
        Func<View?> getRoot,
        DesktopInputSystem input,
        IUiDispatcher dispatcher,
        Action<string, Action?> captureScreenshot)
    {
        _getRoot = getRoot;
        _input = input;
        _dispatcher = dispatcher;
        _captureScreenshot = captureScreenshot;
    }

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
            "Read the live main window's laid-out view tree (ids, labels, text, positions). Call this first to discover what is on screen and how to target it.",
            new ObjectSchema().AddOption("format", new StringSchema
            {
                Description = "\"text\" (default, compact and human-readable) or \"json\" (machine-readable).",
                Options = ["text", "json"],
            }),
            readOnly: true,
            (a, _) => Run(() => Text(Snapshot(EqualsIc(Str(a, "format"), "json"))))));

        tools.AddTool(Def(
            "gui_screenshot",
            "Capture a PNG screenshot of the rendered main window. Returns image content, or an error if the active render backend cannot read back its framebuffer.",
            new ObjectSchema(),
            readOnly: true,
            (a, _) => Run(() => Image(Screenshot()))));

        tools.AddTool(Def(
            "gui_click",
            "Click a view in the main window. Target it by \"id\", \"label\", or \"text\", or click absolute GUI coordinates with \"x\" and \"y\".",
            new ObjectSchema()
                .AddOption("id", new StringSchema { Description = "View id to click." })
                .AddOption("label", new StringSchema { Description = "Clickable label / accessible name to match." })
                .AddOption("text", new StringSchema { Description = "Visible text to match." })
                .AddOption("exact", new BooleanSchema { Description = "Match id/label/text exactly (default true); false matches substrings." })
                .AddOption("button", new StringSchema { Description = "Mouse button (default left).", Options = ["left", "right", "middle"] })
                .AddOption("x", new NumberSchema { Description = "Absolute GUI x (use with y to click a coordinate)." })
                .AddOption("y", new NumberSchema { Description = "Absolute GUI y (use with x to click a coordinate)." }),
            readOnly: false,
            (a, _) => Run(() => Text(Click(
                Str(a, "id"), Str(a, "label"), Str(a, "text"),
                Bool(a, "exact", true), Num(a, "x"), Num(a, "y"), Str(a, "button"))))));

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

    // ---- actions (run on the UI thread) ----

    private string Snapshot(bool asJson) => RunOnUi(() =>
    {
        var root = RequireRoot();
        root.LayoutSelf();
        var snap = SnapshotBuilder.Build(root, _input.InputSystem);
        return asJson ? snap.ToJson() : snap.ToText();
    });

    private string Click(string? id, string? label, string? text, bool exact, float? x, float? y, string? button) => RunOnUi(() =>
    {
        var root = RequireRoot();
        root.LayoutSelf();
        var mouseButton = ParseMouseButton(button);

        PointF point;
        string target;
        if (x is { } px && y is { } py)
        {
            point = new PointF(px, py);
            target = $"({px:0},{py:0})";
        }
        else
        {
            var view = ResolveView(root, id, label, text, exact)
                       ?? throw NotFound(root, id ?? label ?? text);
            point = view.Position.Center;
            target = Describe(view);
        }

        InjectClick(point, mouseButton);
        return $"clicked {target} at ({point.X:0},{point.Y:0})";
    });

    private string Type(string text) => RunOnUi(() =>
    {
        var sys = _input.InputSystem;
        foreach (var ch in text)
        {
            if (ch == '\r') continue;
            if (!AsciiKeyMap.Map.TryGetValue(ch, out var mapped))
                throw new InvalidOperationException($"Cannot type '{ch}' (U+{(int)ch:X4}); not in the ASCII key map. Use gui_key for special keys.");
            SendKey(sys, mapped.Key, mapped.Shift ? InputModifiers.Shift : InputModifiers.None, press: true, release: true);
        }
        return $"typed {text.Length} char(s)";
    });

    private string Key(string keyName, string? mods, string? action) => RunOnUi(() =>
    {
        if (!Enum.TryParse<KeyboardKey>(keyName, ignoreCase: true, out var key))
            throw new InvalidOperationException($"Unknown key '{keyName}'.");
        var modifiers = ParseModifiers(mods);
        var act = (action ?? "press").ToLowerInvariant();
        var sys = _input.InputSystem;
        switch (act)
        {
            case "down": SendKey(sys, key, modifiers, press: true, release: false); break;
            case "up": SendKey(sys, key, modifiers, press: false, release: true); break;
            default: SendKey(sys, key, modifiers, press: true, release: true); break;
        }
        return $"key {key} {act}";
    });

    private byte[] Screenshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zgf-mcp-{Guid.NewGuid():N}.png");
        using var done = new ManualResetEventSlim(false);
        _dispatcher.Post(() => _captureScreenshot(path, () => done.Set()));

        if (!done.Wait(5000) || !File.Exists(path))
            throw new InvalidOperationException(
                "Screenshot unavailable — the active render backend does not support framebuffer read-back, or it timed out.");

        try { return File.ReadAllBytes(path); }
        finally { try { File.Delete(path); } catch { /* best effort */ } }
    }

    private void InjectClick(PointF point, MouseButton button)
    {
        var mouse = _input.Mouse;
        var sys = _input.InputSystem;

        mouse.Point = point;
        var move = new MouseMoveEvent { Mouse = mouse, Phase = EventPhase.Capturing };
        sys.SendMouseMovedEvent(ref move);

        mouse.Press(button);
        var down = new MouseButtonEvent
        {
            Mouse = mouse, Button = button, State = InputState.Pressed,
            Modifiers = InputModifiers.None, Phase = EventPhase.Capturing,
        };
        sys.SendMouseButtonEvent(ref down);

        mouse.Release(button);
        var up = new MouseButtonEvent
        {
            Mouse = mouse, Button = button, State = InputState.Released,
            Modifiers = InputModifiers.None, Phase = EventPhase.Capturing,
        };
        sys.SendMouseButtonEvent(ref up);
    }

    private static void SendKey(InputSystem sys, KeyboardKey key, InputModifiers mods, bool press, bool release)
    {
        if (press)
        {
            var e = new KeyboardKeyEvent { Key = key, State = InputState.Pressed, Modifiers = mods, Phase = EventPhase.Capturing };
            sys.SendKeyboardKeyEvent(ref e);
        }
        if (release)
        {
            var e = new KeyboardKeyEvent { Key = key, State = InputState.Released, Modifiers = mods, Phase = EventPhase.Capturing };
            sys.SendKeyboardKeyEvent(ref e);
        }
    }

    private static View? ResolveView(View root, string? id, string? label, string? text, bool exact)
    {
        if (id is not null)
            return root.FindById(id);
        if (label is not null)
            return root.FindClickable(label, exact)
                   ?? root.Find(v => NameMatches(v.AccessibleName(), label, exact));
        if (text is not null)
            return root.FindByText(text, exact);
        return null;
    }

    // ---- UI-thread marshaling ----

    private T RunOnUi<T>(Func<T> fn, int timeoutMs = 10000)
    {
        Exception? error = null;
        T result = default!;
        using var done = new ManualResetEventSlim(false);
        _dispatcher.Post(() =>
        {
            try { result = fn(); }
            catch (Exception ex) { error = ex; }
            finally { done.Set(); }
        });
        if (!done.Wait(timeoutMs))
            throw new TimeoutException("The UI thread did not run the MCP tool within the timeout.");
        if (error != null) throw error;
        return result;
    }

    private View RequireRoot() => _getRoot() ?? throw new InvalidOperationException("No root view is mounted.");

    private Exception NotFound(View root, string? query)
    {
        var snap = SnapshotBuilder.Build(root, _input.InputSystem).ToText();
        return new InvalidOperationException($"No view matched \"{query ?? "(none)"}\".\n--- snapshot ---\n{snap}");
    }

    private static string Describe(View view) =>
        view.Id != null ? "#" + view.Id : view.AccessibleName() ?? view.GetType().Name;

    private static bool NameMatches(string? name, string query, bool exact) =>
        name != null && (exact
            ? string.Equals(name, query, StringComparison.OrdinalIgnoreCase)
            : name.Contains(query, StringComparison.OrdinalIgnoreCase));

    private static MouseButton ParseMouseButton(string? s) => s?.ToLowerInvariant() switch
    {
        "right" => MouseButton.Right,
        "middle" => MouseButton.Middle,
        _ => MouseButton.Left,
    };

    private static InputModifiers ParseModifiers(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return InputModifiers.None;
        var mods = InputModifiers.None;
        foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (Enum.TryParse<InputModifiers>(part, ignoreCase: true, out var m)) mods |= m;
        return mods;
    }

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

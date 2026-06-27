using System.Net;
using System.Text;
using System.Text.Json;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Inspection;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Desktop;

/// <summary>A localhost HTTP surface for driving the live main window from outside the process
/// (an LLM, a script, curl): read the laid-out view tree, inject mouse/keyboard input, and grab a
/// screenshot. Every request marshals its work onto the UI thread via the <see cref="IUiDispatcher"/>
/// and blocks for the result, so nothing races the renderer. Debug aid only — bound to 127.0.0.1
/// and opt-in (<c>GuiAppBuilder.UseDebugServer</c> or the <c>ZGF_GUI_DEBUG</c> env var).</summary>
public sealed class GuiDebugServer : IDisposable
{
    private readonly Func<View?> _getRoot;
    private readonly DesktopInputSystem _input;
    private readonly IUiDispatcher _dispatcher;
    private readonly Action<string, Action?> _captureScreenshot;

    private HttpListener? _listener;
    private Thread? _thread;

    public GuiDebugServer(
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
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        _listener = listener;
        _thread = new Thread(Loop) { IsBackground = true, Name = "ZGF-GuiDebugServer" };
        _thread.Start();
        Console.WriteLine($"[GuiDebugServer] listening on http://127.0.0.1:{port}/  (GET /snapshot, /screenshot · POST /click, /type, /key)");
    }

    private void Loop()
    {
        while (_listener is { IsListening: true } listener)
        {
            HttpListenerContext ctx;
            try { ctx = listener.GetContext(); }
            catch { break; }

            try { Handle(ctx); }
            catch (Exception ex) { TryWrite(() => WriteText(ctx.Response, 400, ex.Message)); }
            finally { try { ctx.Response.Close(); } catch { /* client gone */ } }
        }
    }

    private void Handle(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        var path = (req.Url?.AbsolutePath ?? "/").TrimEnd('/').ToLowerInvariant();

        switch (path)
        {
            case "":
            case "/help":
                WriteText(res, 200, HelpText());
                return;

            case "/health":
                WriteText(res, 200, "ok");
                return;

            case "/snapshot":
            {
                var json = string.Equals(req.QueryString["format"], "json", StringComparison.OrdinalIgnoreCase);
                var body = RunOnUi(() =>
                {
                    var root = RequireRoot();
                    root.LayoutSelf();
                    var snap = SnapshotBuilder.Build(root, _input.InputSystem);
                    return json ? snap.ToJson() : snap.ToText();
                });
                WriteText(res, 200, body, json ? "application/json" : "text/plain");
                return;
            }

            case "/screenshot":
                WriteScreenshot(res);
                return;

            case "/click":
            {
                var b = ReadJson(req);
                WriteText(res, 200, RunOnUi(() => DoClick(b)));
                return;
            }

            case "/type":
            {
                var b = ReadJson(req);
                WriteText(res, 200, RunOnUi(() => DoType(b)));
                return;
            }

            case "/key":
            {
                var b = ReadJson(req);
                WriteText(res, 200, RunOnUi(() => DoKey(b)));
                return;
            }

            default:
                WriteText(res, 404, $"Unknown endpoint '{path}'.\n\n{HelpText()}");
                return;
        }
    }

    // ---- actions (run on the UI thread) ----

    private string DoClick(JsonElement body)
    {
        var root = RequireRoot();
        root.LayoutSelf();
        var button = ParseMouseButton(GetString(body, "button"));

        PointF point;
        string target;
        if (TryGetFloat(body, "x", out var x) && TryGetFloat(body, "y", out var y))
        {
            point = new PointF(x, y);
            target = $"({x:0},{y:0})";
        }
        else
        {
            var view = ResolveView(root, body) ?? throw NotFound(root, body);
            point = view.Position.Center;
            target = Describe(view);
        }

        InjectClick(point, button);
        return $"clicked {target} at ({point.X:0},{point.Y:0})";
    }

    private string DoType(JsonElement body)
    {
        var text = GetString(body, "text") ?? throw new InvalidOperationException("'text' is required.");
        var sys = _input.InputSystem;
        foreach (var ch in text)
        {
            if (ch == '\r') continue;
            if (!AsciiKeyMap.Map.TryGetValue(ch, out var mapped))
                throw new InvalidOperationException($"Cannot type '{ch}' (U+{(int)ch:X4}); not in the ASCII key map. Use /key for special keys.");
            SendKey(sys, mapped.Key, mapped.Shift ? InputModifiers.Shift : InputModifiers.None, press: true, release: true);
        }
        return $"typed {text.Length} char(s)";
    }

    private string DoKey(JsonElement body)
    {
        var keyName = GetString(body, "key") ?? throw new InvalidOperationException("'key' is required (e.g. Enter, Escape, Tab, A).");
        if (!Enum.TryParse<KeyboardKey>(keyName, ignoreCase: true, out var key))
            throw new InvalidOperationException($"Unknown key '{keyName}'.");
        var mods = ParseModifiers(GetString(body, "mods"));
        var action = (GetString(body, "action") ?? "press").ToLowerInvariant();
        var sys = _input.InputSystem;
        switch (action)
        {
            case "down": SendKey(sys, key, mods, press: true, release: false); break;
            case "up": SendKey(sys, key, mods, press: false, release: true); break;
            default: SendKey(sys, key, mods, press: true, release: true); break;
        }
        return $"key {key} {action}";
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

    private static View? ResolveView(View root, JsonElement body)
    {
        var exact = !TryGetBool(body, "exact", out var e) || e;
        if (GetString(body, "id") is { } id)
            return root.FindById(id);
        if (GetString(body, "label") is { } label)
            return root.FindClickable(label, exact)
                   ?? root.Find(v => NameMatches(v.AccessibleName(), label, exact));
        if (GetString(body, "text") is { } text)
            return root.FindByText(text, exact);
        return null;
    }

    // ---- screenshot ----

    private void WriteScreenshot(HttpListenerResponse res)
    {
        var path = Path.Combine(Path.GetTempPath(), $"zgf-debug-{Guid.NewGuid():N}.png");
        using var done = new ManualResetEventSlim(false);
        _dispatcher.Post(() => _captureScreenshot(path, () => done.Set()));

        if (!done.Wait(5000) || !File.Exists(path))
        {
            WriteText(res, 501, "Screenshot unavailable — the active render backend does not support framebuffer read-back, or it timed out.");
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            res.StatusCode = 200;
            res.ContentType = "image/png";
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
        }
        finally
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
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
            throw new TimeoutException("The UI thread did not run the debug command within the timeout.");
        if (error != null) throw error;
        return result;
    }

    private View RequireRoot() => _getRoot() ?? throw new InvalidOperationException("No root view is mounted.");

    private Exception NotFound(View root, JsonElement body)
    {
        var query = GetString(body, "id") ?? GetString(body, "label") ?? GetString(body, "text") ?? "(none)";
        var snap = SnapshotBuilder.Build(root, _input.InputSystem).ToText();
        return new InvalidOperationException($"No view matched \"{query}\".\n--- snapshot ---\n{snap}");
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

    // ---- HTTP/JSON plumbing ----

    private static JsonElement ReadJson(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        var body = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(body)) return default;
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.Clone();
    }

    private static string? GetString(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static bool TryGetFloat(JsonElement e, string name, out float value)
    {
        value = 0f;
        if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number)
        {
            value = (float)v.GetDouble();
            return true;
        }
        return false;
    }

    private static bool TryGetBool(JsonElement e, string name, out bool value)
    {
        value = false;
        if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) &&
            (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False))
        {
            value = v.GetBoolean();
            return true;
        }
        return false;
    }

    private static void WriteText(HttpListenerResponse res, int status, string body, string contentType = "text/plain")
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        res.StatusCode = status;
        res.ContentType = contentType + "; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        res.OutputStream.Write(bytes, 0, bytes.Length);
    }

    private static void TryWrite(Action write)
    {
        try { write(); } catch { /* client gone */ }
    }

    private static string HelpText() =>
        """
        ZGF GuiDebugServer — drive the live main window.

        GET  /snapshot[?format=json]   the laid-out view tree as text (or JSON)
        GET  /screenshot               PNG of the rendered window
        GET  /health                   "ok"
        POST /click    {"id"|"label"|"text": "...", "exact": true, "button": "left|right|middle"}
                       {"x": 120, "y": 64}            click absolute GUI coords
        POST /type     {"text": "hello"}              type ASCII text
        POST /key      {"key": "Enter", "mods": "Control,Shift", "action": "press|down|up"}

        Examples:
          curl localhost:PORT/snapshot
          curl -X POST localhost:PORT/click -d '{"label":"Push"}'
          curl localhost:PORT/screenshot -o shot.png
        """;

    public void Dispose()
    {
        var listener = _listener;
        _listener = null;
        try { listener?.Stop(); listener?.Close(); } catch { /* already gone */ }
    }
}

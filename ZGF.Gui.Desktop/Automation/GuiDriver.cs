using System.Diagnostics;
using System.Text;
using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Inspection;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Automation;

/// <summary>
/// Drives a running app from a script: find a view, click it, type into it, wait for something to
/// appear, grab a screenshot — across every live window (main, secondary, popups, context menus).
/// The Playwright-shaped counterpart to the headless test harness, and the engine behind
/// <see cref="GuiMcpServer"/>.
///
/// Every call marshals onto the UI thread and blocks for the result, so <b>call this from a script
/// thread, never from the UI thread</b> — from the UI thread it would deadlock against itself, and
/// nothing would repaint anyway.
///
/// Keystrokes take one of two routes. By default they're injected straight into the
/// <see cref="InputSystem"/>: reliable, focus-independent, works on every platform. Set
/// <see cref="UseOsKeyboard"/> and they instead go out through the OS (<see cref="OsKeyboard"/>),
/// entering the app through GLFW's real callbacks — slower and it demands the window stay focused,
/// but it exercises the platform layer that injection skips over.
/// </summary>
public sealed class GuiDriver : ITypeSink
{
    private readonly Func<IReadOnlyList<GuiSurface>> _getSurfaces;
    private readonly IUiDispatcher _dispatcher;
    private readonly Action<IWindow, string, Action?> _captureScreenshot;

    private IntPtr _osTarget;

    public GuiDriver(
        Func<IReadOnlyList<GuiSurface>> getSurfaces,
        IUiDispatcher dispatcher,
        Action<IWindow, string, Action?> captureScreenshot)
    {
        _getSurfaces = getSurfaces;
        _dispatcher = dispatcher;
        _captureScreenshot = captureScreenshot;
    }

    /// <summary>Route keystrokes through the OS instead of injecting them. See the class remarks.</summary>
    public bool UseOsKeyboard { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    // ---- queries ----

    /// <summary>Every live window's view tree, rendered as text (or JSON) — roles, labels, bounds,
    /// focus. The thing to print when a lookup fails and you want to know why.</summary>
    public string Snapshot(bool asJson = false) => RunOnUi(() =>
        new MultiWindowSnapshot(BuildWindowSnapshots()).Render(asJson));

    /// <summary>True when a view with this id (or clickable label, or visible text) is on screen.</summary>
    public bool Exists(string selector) => RunOnUi(() => Resolve(selector, exact: true) != null);

    /// <summary>The text currently in a text field — the ground truth a screenshot can only hint at.
    /// Accepts the field itself or a wrapper around one.</summary>
    public string TextOf(string selector) => RunOnUi(() =>
    {
        var hit = Resolve(selector, exact: true) ?? throw NotFound(selector);
        var field = hit.View as TextInputView
                    ?? hit.View.Find(v => v is TextInputView) as TextInputView;
        if (field == null)
            throw new InvalidOperationException($"\"{selector}\" is not a text field ({hit.View.GetType().Name}).");
        return field.Text.ToString();
    });

    /// <summary>Blocks until <paramref name="selector"/> is on screen, or throws with a snapshot.</summary>
    public void WaitFor(string selector, TimeSpan? timeout = null) =>
        WaitUntil(() => Exists(selector), $"\"{selector}\" to appear", timeout);

    /// <summary>Blocks until <paramref name="condition"/> holds. Polls from the script thread, so the
    /// UI keeps running while we wait.</summary>
    public void WaitUntil(Func<bool> condition, string what, TimeSpan? timeout = null)
    {
        var limit = timeout ?? Timeout;
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < limit)
        {
            if (condition()) return;
            Thread.Sleep(50);
        }
        throw new TimeoutException($"Timed out after {limit.TotalSeconds:0.#}s waiting for {what}.\n{Snapshot()}");
    }

    // ---- actions ----

    /// <summary>Clicks the view matching <paramref name="selector"/> — an id, a clickable label, or
    /// visible text — wherever it lives across the open windows.</summary>
    public void Click(string selector, MouseButton? button = null) =>
        ClickTool(id: selector, label: null, text: null, exact: true, x: null, y: null,
            button: button?.ToString(), window: null);

    /// <summary>Types <paramref name="text"/> into whatever holds focus. Unicode-safe: it sends the
    /// characters themselves, not decoded key positions, so Cyrillic and accented Latin type fine.</summary>
    public void Type(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\r') continue;
            TypeRune(rune);
        }
    }

    public void TypeRune(Rune rune)
    {
        if (UseOsKeyboard)
        {
            RequireOsFocus();
            OsKeyboard.TypeRune(rune);
            return;
        }

        RunOnUi(() =>
        {
            var sys = FocusedSurface().Input.InputSystem;
            SendRune(sys, rune);
            return 0;
        });
    }

    public void PressKey(KeyboardKey key, InputModifiers modifiers = InputModifiers.None)
    {
        if (UseOsKeyboard)
        {
            RequireOsFocus();
            OsKeyboard.PressKey(key, modifiers);
            return;
        }

        RunOnUi(() =>
        {
            var sys = FocusedSurface().Input.InputSystem;
            SendKey(sys, key, modifiers, press: true, release: true);
            return 0;
        });
    }

    /// <summary>A typist bound to this driver, pacing itself by sleeping the script thread.</summary>
    public Typist Typist() => new(this);

    /// <summary>The OS window handle a scripted OS keystroke would land on — what
    /// <see cref="OsKeyboard.IsForeground"/> needs to check, and <see cref="OsKeyboard.Focus"/> to grab.</summary>
    public IntPtr FocusedWindowHandle => RunOnUi(() => FocusedSurface().Window.NativeHandle);

    /// <summary>
    /// Brings the app's window to the front and blocks until the OS agrees it's the foreground window.
    /// Mandatory before scripting OS keystrokes: SendInput has no notion of a target — it goes wherever
    /// focus happens to be, so without this the script types into whatever the user was last using.
    /// Windows often refuses a background process's focus grab outright, which is why this waits for
    /// the result instead of assuming it worked.
    /// </summary>
    public void FocusApp(TimeSpan? timeout = null)
    {
        _osTarget = RunOnUi(() => _getSurfaces()[0].Window.NativeHandle);
        OsKeyboard.Focus(_osTarget);
        WaitUntil(
            () => OsKeyboard.IsForeground(_osTarget),
            "the app window to become the OS foreground window (click it if Windows refused the focus grab)",
            timeout ?? TimeSpan.FromSeconds(20));
    }

    // Refuse to type rather than let keystrokes leak into whatever the user alt-tabbed to. Silent loss
    // is the worst outcome here: the script "passes" while half the text lands in someone's editor.
    private void RequireOsFocus()
    {
        if (_osTarget == IntPtr.Zero)
            throw new InvalidOperationException("Call FocusApp() before scripting OS keystrokes.");
        if (!OsKeyboard.IsForeground(_osTarget))
            throw new InvalidOperationException(
                "The app window lost OS focus, so scripted keystrokes would land in another application. " +
                "Refocus it, or drive the driver with injected input (UseOsKeyboard = false).");
    }

    public void SaveScreenshot(string path, string? window = null)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        using var done = new ManualResetEventSlim(false);
        var surface = RunOnUi(() => window is { Length: > 0 }
            ? ResolveWindow(window) ?? throw NoWindow(window)
            : DefaultScreenshotSurface());

        _dispatcher.Post(() => _captureScreenshot(surface.Window, path, () => done.Set()));

        if (!done.Wait(5000) || !File.Exists(path))
            throw new InvalidOperationException(
                "Screenshot unavailable — the active render backend does not support framebuffer read-back, or it timed out.");
    }

    // ---- the full-selector surface, shared with the MCP tools ----

    internal string ClickTool(
        string? id, string? label, string? text, bool exact,
        float? x, float? y, string? button, string? window) => RunOnUi(() =>
    {
        var mouseButton = ParseMouseButton(button);

        GuiSurface surface;
        PointF point;
        string target;
        if (x is { } px && y is { } py)
        {
            surface = ResolveWindow(window) ?? throw NoWindow(window);
            point = new PointF(px, py);
            target = $"{surface.Role} ({px:0},{py:0})";
        }
        else
        {
            var hit = ResolveAcross(id, label, text, exact) ?? throw NotFound(id ?? label ?? text);
            surface = hit.Surface;
            point = hit.View.Position.Center;
            target = $"{surface.Role} {Describe(hit.View)}";
        }

        InjectClick(surface.Input, point, mouseButton);
        return $"clicked {target} at ({point.X:0},{point.Y:0})";
    });

    internal string TypeTool(string text)
    {
        Type(text);
        return $"typed {text.Length} char(s)";
    }

    internal string KeyTool(string keyName, string? mods, string? action)
    {
        if (!Enum.TryParse<KeyboardKey>(keyName, ignoreCase: true, out var key))
            throw new InvalidOperationException($"Unknown key '{keyName}'.");
        var modifiers = ParseModifiers(mods);
        var act = (action ?? "press").ToLowerInvariant();

        return RunOnUi(() =>
        {
            var sys = FocusedSurface().Input.InputSystem;
            switch (act)
            {
                case "down": SendKey(sys, key, modifiers, press: true, release: false); break;
                case "up": SendKey(sys, key, modifiers, press: false, release: true); break;
                default: SendKey(sys, key, modifiers, press: true, release: true); break;
            }
            return $"key {key} {act}";
        });
    }

    internal byte[] ScreenshotTool(string? window)
    {
        var path = Path.Combine(Path.GetTempPath(), $"zgf-driver-{Guid.NewGuid():N}.png");
        SaveScreenshot(path, window);
        try { return File.ReadAllBytes(path); }
        finally { try { File.Delete(path); } catch { /* best effort */ } }
    }

    // ---- input injection ----

    private static void SendRune(InputSystem sys, Rune rune)
    {
        // Mirror what a real keyboard produces: the physical key (when the character has one on a US
        // layout) plus the text event carrying the character. The key half drives nothing on its own —
        // it's there so scripted typing has the same shape as the OS's.
        (KeyboardKey Key, bool Shift) mapped = default;
        var hasPhysicalKey = rune.IsBmp && AsciiKeyMap.Map.TryGetValue((char)rune.Value, out mapped);
        var mods = mapped.Shift ? InputModifiers.Shift : InputModifiers.None;

        if (Rune.IsControl(rune))
        {
            if (hasPhysicalKey) SendKey(sys, mapped.Key, mods, press: true, release: true);
            return;
        }

        if (hasPhysicalKey) SendKey(sys, mapped.Key, mods, press: true, release: false);
        var e = new TextInputEvent { Rune = rune, Phase = EventPhase.Capturing };
        sys.SendTextInputEvent(ref e);
        if (hasPhysicalKey) SendKey(sys, mapped.Key, mods, press: false, release: true);
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

    private static void InjectClick(DesktopInputSystem input, PointF point, MouseButton button)
    {
        var mouse = input.Mouse;
        var sys = input.InputSystem;

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

    // ---- resolution ----

    private List<WindowSnapshot> BuildWindowSnapshots()
    {
        var surfaces = _getSurfaces();
        var windows = new List<WindowSnapshot>(surfaces.Count);
        foreach (var s in surfaces)
        {
            if (s.Root is not { } root) continue;
            root.LayoutSelf();
            s.Window.GetPosition(out var x, out var y);
            var bounds = new RectI(x, y, s.Window.Width, s.Window.Height);
            windows.Add(new WindowSnapshot(
                s.Role, bounds, s.Window.IsFocused, SnapshotBuilder.Build(root, s.Input.InputSystem)));
        }
        return windows;
    }

    // A bare selector could be any of the three handles a view offers; try them in order of precision.
    private (GuiSurface Surface, View View)? Resolve(string selector, bool exact) =>
        ResolveAcross(selector, null, null, exact)
        ?? ResolveAcross(null, selector, null, exact)
        ?? ResolveAcross(null, null, selector, exact);

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

    // Search every interactive window topmost-first (last-opened popup wins — it's modal and on
    // top), skipping pass-through tooltip windows which receive no clicks.
    private (GuiSurface Surface, View View)? ResolveAcross(string? id, string? label, string? text, bool exact)
    {
        var surfaces = _getSurfaces();
        for (var i = surfaces.Count - 1; i >= 0; i--)
        {
            var s = surfaces[i];
            if (s.Role == "tooltip" || s.Root is not { } root) continue;
            root.LayoutSelf();
            if (ResolveView(root, id, label, text, exact) is { } v) return (s, v);
        }
        return null;
    }

    // A window named by role (topmost match) or by snapshot index; null/empty => main.
    private GuiSurface? ResolveWindow(string? selector)
    {
        var surfaces = _getSurfaces();
        if (surfaces.Count == 0) return null;
        if (string.IsNullOrWhiteSpace(selector)) return surfaces[0];
        if (int.TryParse(selector, out var idx) && idx >= 0 && idx < surfaces.Count) return surfaces[idx];
        for (var i = surfaces.Count - 1; i >= 0; i--)
            if (EqualsIc(surfaces[i].Role, selector)) return surfaces[i];
        return null;
    }

    // Keyboard goes to the focused window (an open menu popup is the key window); fall back to main
    // when nothing reports focus.
    private GuiSurface FocusedSurface()
    {
        var surfaces = _getSurfaces();
        if (surfaces.Count == 0) throw new InvalidOperationException("No window is mounted.");
        for (var i = surfaces.Count - 1; i >= 0; i--)
            if (surfaces[i].Window.IsFocused) return surfaces[i];
        return surfaces[0];
    }

    // The most useful default for "show me the menu": the topmost open context menu, else main.
    private GuiSurface DefaultScreenshotSurface()
    {
        var surfaces = _getSurfaces();
        if (surfaces.Count == 0) throw new InvalidOperationException("No window is mounted.");
        for (var i = surfaces.Count - 1; i >= 0; i--)
            if (surfaces[i].Role == "context-menu") return surfaces[i];
        return surfaces[0];
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
            throw new TimeoutException("The UI thread did not run the driver action within the timeout.");
        if (error != null) throw error;
        return result;
    }

    private Exception NotFound(string? query)
    {
        var snap = new MultiWindowSnapshot(BuildWindowSnapshots()).ToText();
        return new InvalidOperationException($"No view matched \"{query ?? "(none)"}\".\n--- snapshot ---\n{snap}");
    }

    private Exception NoWindow(string? selector)
    {
        var roles = string.Join(", ", _getSurfaces().Select(s => s.Role));
        return new InvalidOperationException($"No window matched \"{selector ?? "(none)"}\". Open windows: {roles}");
    }

    private static string Describe(View view) =>
        view.Id != null ? "#" + view.Id : view.AccessibleName() ?? view.GetType().Name;

    private static bool NameMatches(string? name, string query, bool exact) =>
        name != null && (exact
            ? string.Equals(name, query, StringComparison.OrdinalIgnoreCase)
            : name.Contains(query, StringComparison.OrdinalIgnoreCase));

    private static bool EqualsIc(string? a, string? b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

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
}

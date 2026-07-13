using System.Text;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Inspection;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Testing;

/// <summary>Mounts a widget tree headlessly, lays it out at a viewport, drives input through the
/// real <see cref="InputSystem"/> dispatch path, controls the frame clock, and captures draws —
/// so GUI behaviour can be tested without a window or GPU.
/// <para>
/// To debug a screen as text, the loop is: <b>Create → <see cref="Settle"/> → <see cref="Snapshot"/>
/// → act by label → diff</b>. The snapshot is the screen rendered as text (roles, labels, bounds,
/// focus/hover); act with <see cref="Click(string,bool,MouseButton?)"/> / <see cref="Type"/>; then
/// <c>before.DiffTo(harness.Snapshot())</c> shows what changed. Lookups that miss throw with
/// candidates + the snapshot, so a wrong label is self-correcting. <see cref="HarnessAssertions"/>
/// adds intent checks; <see cref="CreateRaster"/> + <see cref="SaveScreenshot"/> add a real PNG for
/// genuinely visual bugs.
/// </para>
/// <example><code>
/// using var h = GuiTestHarness.Create(ctx => new MyScreen().BuildView(ctx));
/// h.Settle();
/// var before = h.Snapshot();
/// h.Click("Stage All");
/// h.Settle();
/// Console.Write(before.DiffTo(h.Snapshot()));   // ~ #stage-all label "Stage All" -> "Unstage All"
/// </code></example></summary>
public sealed class GuiTestHarness : IDisposable
{
    private readonly RecordingCanvas? _canvas;
    private readonly RasterCanvas? _raster;
    private readonly InputSystem _input;
    private readonly Mouse _mouse;
    private readonly FrameTicker _ticker;
    private readonly Context _context;
    private readonly View _root;
    private readonly HeadlessContextMenuHost _menuHost;
    private int _redrawCount;

    public View Root => _root;

    /// <summary>The draw-capture canvas. Available in the default (synthetic) mode; in raster mode
    /// (<see cref="CreateRaster"/>) there is no capture canvas — use <see cref="SaveScreenshot"/>.</summary>
    public RecordingCanvas Canvas => _canvas
        ?? throw new InvalidOperationException(
            "This harness is in raster mode (CreateRaster); Canvas/Render are only available from Create. Use SaveScreenshot.");

    public InputSystem Input => _input;
    public Context Context => _context;
    public int RedrawCount => _redrawCount;

    private GuiTestHarness(
        Context context, RecordingCanvas? canvas, RasterCanvas? raster,
        InputSystem input, Mouse mouse, FrameTicker ticker, View root, HeadlessContextMenuHost menuHost)
    {
        _context = context;
        _canvas = canvas;
        _raster = raster;
        _input = input;
        _mouse = mouse;
        _ticker = ticker;
        _root = root;
        _menuHost = menuHost;
    }

    public static GuiTestHarness Create(
        Func<Context, View> content,
        int width = 800,
        int height = 600,
        Action<Context>? configure = null,
        ITextMeasurer? measurer = null)
    {
        var ctx = new Context();
        var canvas = new RecordingCanvas(measurer);
        ctx.Canvas = canvas;
        var input = new InputSystem();
        ctx.AddService(input);
        var mouse = new Mouse();
        var ticker = new FrameTicker();
        ctx.AddService<IFrameTicker>(ticker);
        ctx.AddService(new SvgImageCache(new SvgImageCacheOptions()));
        var menuHost = new HeadlessContextMenuHost(ctx);
        ctx.AddService<IContextMenuHost>(menuHost);
        configure?.Invoke(ctx);

        var root = content(ctx);
        root.Width = width;
        root.Height = height;

        var harness = new GuiTestHarness(ctx, canvas, null, input, mouse, ticker, root, menuHost);
        root.OnRedrawNeeded = () => harness._redrawCount++;
        root.Mount();
        root.LayoutSelf();
        return harness;
    }

    /// <summary>Like <see cref="Create"/> but backed by a <see cref="RasterCanvas"/>, so layout uses
    /// real font metrics and <see cref="SaveScreenshot"/> can render a PNG. Provide a
    /// <see cref="FreeTypeFontBackend"/> with at least the default font loaded (e.g.
    /// <c>fonts.LoadFontFromFile(interPath, 16)</c>). If the screen uses extra families (icon/mono),
    /// register them in <paramref name="configure"/> by casting the canvas:
    /// <c>((RenderedCanvasBase)ctx.Canvas).RegisterFont(family, fonts.LoadFontFromFile(path, size))</c>.</summary>
    public static GuiTestHarness CreateRaster(
        Func<Context, View> content,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        int width = 800,
        int height = 600,
        Action<Context>? configure = null,
        uint clearColor = 0xFF1E1E1Eu)
    {
        var ctx = new Context();
        var canvas = new RasterCanvas(width, height, fonts, defaultFont, dpiScale: 1f, clearColor);
        ctx.Canvas = canvas;
        var input = new InputSystem();
        ctx.AddService(input);
        var mouse = new Mouse();
        var ticker = new FrameTicker();
        ctx.AddService<IFrameTicker>(ticker);
        ctx.AddService(new SvgImageCache(new SvgImageCacheOptions()));
        var menuHost = new HeadlessContextMenuHost(ctx);
        ctx.AddService<IContextMenuHost>(menuHost);
        configure?.Invoke(ctx);

        var root = content(ctx);
        root.Width = width;
        root.Height = height;

        var harness = new GuiTestHarness(ctx, null, canvas, input, mouse, ticker, root, menuHost);
        root.OnRedrawNeeded = () => harness._redrawCount++;
        root.Mount();
        root.LayoutSelf();
        return harness;
    }

    public void Layout() => _root.LayoutSelf();

    /// <summary>Reduces the current laid-out tree to a <see cref="UiSnapshot"/> — the textual,
    /// diffable view of what's on screen, with live focus/hover merged in from the input system.
    /// Lay out (or <see cref="Settle"/>) first so bounds and state are current.</summary>
    public UiSnapshot Snapshot() => SnapshotBuilder.Build(_root, _input);

    /// <summary>Open context menus, oldest first (last = topmost). Each is its own headless window
    /// with its own input system — context menus opened through the harness's
    /// <see cref="ZGF.Gui.Desktop.Components.ContextMenu.IContextMenuHost"/> land here, not in the
    /// main tree. Empty when no menu is open.</summary>
    public IReadOnlyList<OpenMenu> Menus => _menuHost.OpenMenus;

    public int OpenMenuCount => _menuHost.OpenMenus.Count;

    /// <summary>The topmost open context menu's root, or null when none is open.</summary>
    public ContextMenu? TopMenu
    {
        get
        {
            var menus = _menuHost.OpenMenus;
            return menus.Count > 0 ? menus[^1].Menu : null;
        }
    }

    /// <summary>Like <see cref="Snapshot"/> but a forest: the main root plus every open context menu,
    /// each under a <c>=== window: ROLE [x,y wxh] ===</c> header — so a context menu (a separate
    /// window in the live app) is visible and diffable alongside the main window, exactly as the live
    /// MCP server renders it. Lay out / <see cref="Settle"/> first so bounds are current.</summary>
    public MultiWindowSnapshot SnapshotWindows()
    {
        Layout();
        var menus = _menuHost.OpenMenus;
        var windows = new List<WindowSnapshot>(menus.Count + 1);
        var mainBounds = new RectI(0, 0, (int)_root.Position.Width, (int)_root.Position.Height);
        windows.Add(new WindowSnapshot("main", mainBounds, Focused: menus.Count == 0,
            SnapshotBuilder.Build(_root, _input)));
        for (var i = 0; i < menus.Count; i++)
        {
            var menu = menus[i].Menu;
            menu.LayoutSelf();
            var p = menu.Position;
            var bounds = new RectI((int)p.Left, (int)p.Bottom, (int)p.Width, (int)p.Height);
            windows.Add(new WindowSnapshot("context-menu", bounds, Focused: i == menus.Count - 1,
                SnapshotBuilder.Build(menu, menus[i].Input)));
        }
        return new MultiWindowSnapshot(windows);
    }

    public void Resize(int width, int height)
    {
        _root.Width = width;
        _root.Height = height;
        Layout();
    }

    public RecordingCanvas Render()
    {
        var canvas = Canvas;
        canvas.Reset();
        _root.LayoutSelf();
        _root.DrawSelf(canvas);
        return canvas;
    }

    /// <summary>Renders the current tree to a PNG (raster mode only). Lay out / <see cref="Settle"/>
    /// first so the frame is at rest. The image uses real fonts; fills are 1-sample and box
    /// shadows/images are omitted (see <see cref="RasterCanvas"/>) — the text snapshot stays the
    /// precise source of truth, this is for spotting visual bugs.</summary>
    public void SaveScreenshot(string path)
    {
        var raster = _raster
            ?? throw new InvalidOperationException(
                "SaveScreenshot requires raster mode. Create the harness with GuiTestHarness.CreateRaster.");
        raster.BeginFrame();
        _root.LayoutSelf();
        _root.DrawSelf(raster);
        raster.EndFrame();
        raster.SavePng(path);
    }

    public void MoveTo(float x, float y)
    {
        Layout();
        DispatchMove(_input, x, y);
    }

    public void Press(MouseButton? button = null) =>
        DispatchButton(_input, button ?? MouseButton.Left, InputState.Pressed);

    public void Release(MouseButton? button = null) =>
        DispatchButton(_input, button ?? MouseButton.Left, InputState.Released);

    public void Click(float x, float y, MouseButton? button = null)
    {
        MoveTo(x, y);
        Press(button);
        Release(button);
    }

    // Targeted dispatch — same path as the public Move/Press/Release but against an arbitrary input
    // system, so a click can be routed into an open menu's own input system rather than the main one.
    private void DispatchMove(InputSystem input, float x, float y)
    {
        _mouse.Point = new PointF(x, y);
        var e = new MouseMoveEvent { Mouse = _mouse, Phase = EventPhase.Capturing };
        input.SendMouseMovedEvent(ref e);
    }

    private void DispatchButton(InputSystem input, MouseButton button, InputState state)
    {
        if (state == InputState.Pressed) _mouse.Press(button); else _mouse.Release(button);
        var e = new MouseButtonEvent
        {
            Mouse = _mouse,
            Button = button,
            State = state,
            Modifiers = InputModifiers.None,
            Phase = EventPhase.Capturing,
        };
        input.SendMouseButtonEvent(ref e);
    }

    private void ClickAt(InputSystem input, float x, float y, MouseButton? button)
    {
        var b = button ?? MouseButton.Left;
        DispatchMove(input, x, y);
        DispatchButton(input, b, InputState.Pressed);
        DispatchButton(input, b, InputState.Released);
    }

    public void ClickOn(View view, MouseButton? button = null)
    {
        var center = view.Position.Center;
        Click(center.X, center.Y, button);
    }

    public void ClickOn(string id, MouseButton? button = null)
    {
        var view = _root.FindById(id)
            ?? throw NotFound("view with Id", id, IdCandidates());
        ClickOn(view, button);
    }

    /// <summary>Clicks the button-role view whose accessible label matches — the intent-level action
    /// ("click Push"). Case-insensitive; pass <paramref name="exact"/> false for a substring match.
    /// On no match it throws with the nearest labels and the full snapshot, so a failed click is
    /// self-diagnosing rather than a dead end.</summary>
    public void Click(string label, bool exact = true, MouseButton? button = null)
    {
        var view = _root.FindClickable(label, exact)
            ?? throw NotFound("clickable view labeled", label, ClickableCandidates());
        ClickOn(view, button);
    }

    /// <summary>Clicks a context-menu item by its text, in the topmost open menu (searching submenus
    /// first). Routes the click into that menu's own input system, so the item's controller runs
    /// exactly as it would over a real popup — a leaf item dismisses the menu, a disabled item
    /// consumes the press and keeps it open. Throws with the window forest when nothing matches.</summary>
    public void ClickMenuItem(string text, bool exact = true, MouseButton? button = null)
    {
        var hit = ResolveMenuItem(text, exact) ?? throw MenuItemNotFound(text);
        var center = hit.View.Position.Center;
        ClickAt(hit.Input, center.X, center.Y, button);
    }

    /// <summary>Moves the pointer over a context-menu item by its text (topmost menu first) — the
    /// gesture that opens a submenu or selects a row, without clicking.</summary>
    public void HoverMenuItem(string text, bool exact = true)
    {
        var hit = ResolveMenuItem(text, exact) ?? throw MenuItemNotFound(text);
        var center = hit.View.Position.Center;
        DispatchMove(hit.Input, center.X, center.Y);
    }

    // Resolve a menu item by text across open menus, topmost-first (a menu is modal and on top).
    // Tries the item's own text, then a clickable/accessible-name match for items with custom labels.
    private (InputSystem Input, View View)? ResolveMenuItem(string text, bool exact)
    {
        var menus = _menuHost.OpenMenus;
        for (var i = menus.Count - 1; i >= 0; i--)
        {
            var menu = menus[i].Menu;
            menu.LayoutSelf();
            var view = menu.FindByText(text, exact)
                ?? menu.FindClickable(text, exact)
                ?? menu.Find(v => NameMatches(v.AccessibleName(), text, exact));
            if (view != null) return (menus[i].Input, view);
        }
        return null;
    }

    private InvalidOperationException MenuItemNotFound(string text)
    {
        var sb = new StringBuilder();
        sb.Append("No context-menu item with text \"").Append(text).Append("\" found in any open menu.");
        sb.Append("\n--- windows ---\n").Append(SnapshotWindows().ToText());
        return new InvalidOperationException(sb.ToString());
    }

    private static bool NameMatches(string? name, string query, bool exact) =>
        name != null && (exact
            ? string.Equals(name, query, StringComparison.OrdinalIgnoreCase)
            : name.Contains(query, StringComparison.OrdinalIgnoreCase));

    /// <summary>Resolves a view by <see cref="View.Id"/> first, then by accessible label (any role).
    /// Throws with candidates + snapshot when nothing matches.</summary>
    public View Get(string idOrLabel)
    {
        return _root.FindById(idOrLabel)
            ?? _root.Find(v => string.Equals(v.AccessibleName(), idOrLabel, StringComparison.OrdinalIgnoreCase))
            ?? throw NotFound("view with id or label", idOrLabel, AllCandidates());
    }

    public void Scroll(float dx, float dy)
    {
        var e = new MouseWheelScrolledEvent
        {
            Mouse = _mouse,
            DeltaX = dx,
            DeltaY = dy,
            Phase = EventPhase.Capturing,
        };
        _input.SendMouseScrollEvent(ref e);
    }

    public void KeyDown(KeyboardKey key, InputModifiers mods = InputModifiers.None)
    {
        var e = new KeyboardKeyEvent
        {
            Key = key,
            State = InputState.Pressed,
            Modifiers = mods,
            Phase = EventPhase.Capturing,
        };
        _input.SendKeyboardKeyEvent(ref e);
    }

    public void KeyUp(KeyboardKey key, InputModifiers mods = InputModifiers.None)
    {
        var e = new KeyboardKeyEvent
        {
            Key = key,
            State = InputState.Released,
            Modifiers = mods,
            Phase = EventPhase.Capturing,
        };
        _input.SendKeyboardKeyEvent(ref e);
    }

    public void PressKey(KeyboardKey key, InputModifiers mods = InputModifiers.None)
    {
        KeyDown(key, mods);
        KeyUp(key, mods);
    }

    /// <summary>Dispatches one OS text-input event — the harness stand-in for GLFW's character
    /// callback. This is the only path that inserts text; <see cref="PressKey"/> carries physical
    /// keys, which drive shortcuts and navigation.</summary>
    public void SendText(Rune rune)
    {
        var e = new TextInputEvent
        {
            Rune = rune,
            Phase = EventPhase.Capturing,
        };
        _input.SendTextInputEvent(ref e);
    }

    /// <summary>
    /// Types <paramref name="text"/> the way a real keyboard does: each character dispatches a
    /// physical key press/release <em>and</em> a separate text-input event carrying the character
    /// itself. Any character can be typed, not just ASCII.
    ///
    /// The split mirrors the OS. Physical keys are layout-independent, so only characters that sit
    /// on a US layout get one (via <see cref="KeyMap"/>); a Cyrillic or accented character arrives
    /// as text alone — which is exactly what the app sees on a non-US layout, where the key at the
    /// 'q' position reports as 'q' but the OS commits 'й'. Control characters (Enter, Tab) are
    /// key-only: the OS never delivers them as text.
    /// </summary>
    public void Type(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\r') continue;

            (KeyboardKey Key, bool Shift) mapped = default;
            var hasPhysicalKey = rune.IsBmp && KeyMap.TryGetValue((char)rune.Value, out mapped);
            var mods = mapped.Shift ? InputModifiers.Shift : InputModifiers.None;

            if (Rune.IsControl(rune))
            {
                if (hasPhysicalKey) PressKey(mapped.Key, mods);
                continue;
            }

            if (hasPhysicalKey) KeyDown(mapped.Key, mods);
            SendText(rune);
            if (hasPhysicalKey) KeyUp(mapped.Key, mods);
        }
    }

    public void Tick(float seconds)
    {
        _ticker.Tick(seconds);
        Layout();
    }

    public void Advance(float seconds, float step = 1f / 60f)
    {
        var remaining = seconds;
        while (remaining > 0f)
        {
            var dt = Math.Min(step, remaining);
            _ticker.Tick(dt);
            remaining -= dt;
        }
        Layout();
    }

    /// <summary>Ticks the clock until no animation is driving the loop (<see cref="FrameTicker.ActiveCount"/>
    /// reaches zero) or <paramref name="maxSeconds"/> elapses, then lays out — so a following
    /// <see cref="Snapshot"/> or <see cref="SaveScreenshot"/> sees a settled, resting frame instead of a
    /// mid-transition one. The cap guards against an animation that never parks (e.g. a spinner).</summary>
    public void Settle(float maxSeconds = 5f, float step = 1f / 60f)
    {
        var elapsed = 0f;
        while (_ticker.ActiveCount > 0 && elapsed < maxSeconds)
        {
            _ticker.Tick(step);
            elapsed += step;
        }
        Layout();
    }

    public void Dispose()
    {
        _menuHost.CloseAllImmediately();
        _root.Unmount();
        _context.Dispose();
    }

    // ---------- Failure diagnostics ----------
    // A failed lookup is where an LLM gets stuck. Every miss reports what *is* there (candidates),
    // the closest names by edit distance, and the full snapshot — enough to recover in one step.

    private InvalidOperationException NotFound(string what, string query, IReadOnlyList<string> candidates)
    {
        var sb = new StringBuilder();
        sb.Append("No ").Append(what).Append(" \"").Append(query).Append("\" found.");

        var nearest = Nearest(query, candidates);
        if (nearest.Count > 0)
            sb.Append("\nNearest: ").Append(string.Join(", ", nearest));
        if (candidates.Count > 0)
            sb.Append("\nAvailable: ").Append(string.Join(", ", candidates));

        sb.Append("\n--- snapshot ---\n").Append(Snapshot().ToText());
        return new InvalidOperationException(sb.ToString());
    }

    private IReadOnlyList<string> IdCandidates() =>
        _root.SelfAndDescendants().Where(v => v.Id != null).Select(v => v.Id!).Distinct().ToList();

    private IReadOnlyList<string> ClickableCandidates() =>
        _root.FindAllByRole(AccessibilityRole.Button)
            .Select(v => v.AccessibleName()).Where(n => !string.IsNullOrEmpty(n)).Select(n => n!)
            .Distinct().ToList();

    private IReadOnlyList<string> AllCandidates() =>
        IdCandidates()
            .Concat(_root.SelfAndDescendants().Select(v => v.AccessibleName()).Where(n => !string.IsNullOrEmpty(n)).Select(n => n!))
            .Distinct().ToList();

    private static IReadOnlyList<string> Nearest(string query, IReadOnlyList<string> candidates, int take = 3)
    {
        return candidates
            .Select(c => (c, d: Levenshtein(query.ToLowerInvariant(), c.ToLowerInvariant())))
            .Where(x => x.d <= Math.Max(2, query.Length / 2))
            .OrderBy(x => x.d)
            .Take(take)
            .Select(x => x.c)
            .ToList();
    }

    private static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }

    /// <summary>Character → the physical key that produces it on a US layout, for synthesizing the
    /// key half of <see cref="Type"/>. Shared with the GUI MCP server via <see cref="AsciiKeyMap"/>.
    /// Characters outside this map still type fine — they arrive as text events only.</summary>
    public static readonly IReadOnlyDictionary<char, (KeyboardKey Key, bool Shift)> KeyMap = AsciiKeyMap.Map;
}

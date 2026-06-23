using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Testing;

/// <summary>Mounts a widget tree headlessly, lays it out at a viewport, drives input through the
/// real <see cref="InputSystem"/> dispatch path, controls the frame clock, and captures draws —
/// so GUI behaviour can be tested without a window or GPU.</summary>
public sealed class GuiTestHarness : IDisposable
{
    private readonly RecordingCanvas _canvas;
    private readonly InputSystem _input;
    private readonly Mouse _mouse;
    private readonly FrameTicker _ticker;
    private readonly Context _context;
    private readonly View _root;
    private int _redrawCount;

    public View Root => _root;
    public RecordingCanvas Canvas => _canvas;
    public InputSystem Input => _input;
    public Context Context => _context;
    public int RedrawCount => _redrawCount;

    private GuiTestHarness(
        Context context, RecordingCanvas canvas, InputSystem input, Mouse mouse, FrameTicker ticker, View root)
    {
        _context = context;
        _canvas = canvas;
        _input = input;
        _mouse = mouse;
        _ticker = ticker;
        _root = root;
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
        configure?.Invoke(ctx);

        var root = content(ctx);
        root.Width = width;
        root.Height = height;

        var harness = new GuiTestHarness(ctx, canvas, input, mouse, ticker, root);
        root.OnRedrawNeeded = () => harness._redrawCount++;
        root.Mount();
        root.LayoutSelf();
        return harness;
    }

    public void Layout() => _root.LayoutSelf();

    public void Resize(int width, int height)
    {
        _root.Width = width;
        _root.Height = height;
        Layout();
    }

    public RecordingCanvas Render()
    {
        _canvas.Reset();
        _root.LayoutSelf();
        _root.DrawSelf(_canvas);
        return _canvas;
    }

    public void MoveTo(float x, float y)
    {
        Layout();
        _mouse.Point = new PointF(x, y);
        var e = new MouseMoveEvent { Mouse = _mouse, Phase = EventPhase.Capturing };
        _input.SendMouseMovedEvent(ref e);
    }

    public void Press(MouseButton? button = null)
    {
        var b = button ?? MouseButton.Left;
        _mouse.Press(b);
        var e = new MouseButtonEvent
        {
            Mouse = _mouse,
            Button = b,
            State = InputState.Pressed,
            Modifiers = InputModifiers.None,
            Phase = EventPhase.Capturing,
        };
        _input.SendMouseButtonEvent(ref e);
    }

    public void Release(MouseButton? button = null)
    {
        var b = button ?? MouseButton.Left;
        _mouse.Release(b);
        var e = new MouseButtonEvent
        {
            Mouse = _mouse,
            Button = b,
            State = InputState.Released,
            Modifiers = InputModifiers.None,
            Phase = EventPhase.Capturing,
        };
        _input.SendMouseButtonEvent(ref e);
    }

    public void Click(float x, float y, MouseButton? button = null)
    {
        MoveTo(x, y);
        Press(button);
        Release(button);
    }

    public void ClickOn(View view, MouseButton? button = null)
    {
        var center = view.Position.Center;
        Click(center.X, center.Y, button);
    }

    public void ClickOn(string id, MouseButton? button = null)
    {
        var view = _root.FindById(id)
            ?? throw new InvalidOperationException($"No view with Id '{id}' found in the tree.");
        ClickOn(view, button);
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

    /// <summary>Synthesizes key events for each character via <see cref="KeyMap"/>. Best-effort over
    /// the ASCII subset; throws for an unmapped character — use <see cref="PressKey"/> for those.</summary>
    public void Type(string text)
    {
        foreach (var ch in text)
        {
            if (ch == '\r') continue;
            if (!KeyMap.TryGetValue(ch, out var mapped))
                throw new NotSupportedException(
                    $"Type cannot synthesize '{ch}' (U+{(int)ch:X4}); use PressKey for unmapped keys.");
            PressKey(mapped.Key, mapped.Shift ? InputModifiers.Shift : InputModifiers.None);
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

    public void Dispose()
    {
        _root.Unmount();
        _context.Dispose();
    }

    /// <summary>Character → (key, shift) for <see cref="Type"/>. Round-trips against
    /// <see cref="KeyboardKeyExtensions.ToChar"/> (asserted by a test), so it can't drift from the
    /// controller's decoding.</summary>
    public static readonly IReadOnlyDictionary<char, (KeyboardKey Key, bool Shift)> KeyMap =
        new Dictionary<char, (KeyboardKey, bool)>
        {
            ['1'] = (KeyboardKey.Alpha1, false), ['!'] = (KeyboardKey.Alpha1, true),
            ['2'] = (KeyboardKey.Alpha2, false), ['@'] = (KeyboardKey.Alpha2, true),
            ['3'] = (KeyboardKey.Alpha3, false), ['#'] = (KeyboardKey.Alpha3, true),
            ['4'] = (KeyboardKey.Alpha4, false), ['$'] = (KeyboardKey.Alpha4, true),
            ['5'] = (KeyboardKey.Alpha5, false), ['%'] = (KeyboardKey.Alpha5, true),
            ['6'] = (KeyboardKey.Alpha6, false), ['^'] = (KeyboardKey.Alpha6, true),
            ['7'] = (KeyboardKey.Alpha7, false), ['&'] = (KeyboardKey.Alpha7, true),
            ['8'] = (KeyboardKey.Alpha8, false), ['*'] = (KeyboardKey.Alpha8, true),
            ['9'] = (KeyboardKey.Alpha9, false), ['('] = (KeyboardKey.Alpha9, true),
            ['0'] = (KeyboardKey.Alpha0, false), [')'] = (KeyboardKey.Alpha0, true),
            ['a'] = (KeyboardKey.A, false), ['A'] = (KeyboardKey.A, true),
            ['b'] = (KeyboardKey.B, false), ['B'] = (KeyboardKey.B, true),
            ['c'] = (KeyboardKey.C, false), ['C'] = (KeyboardKey.C, true),
            ['d'] = (KeyboardKey.D, false), ['D'] = (KeyboardKey.D, true),
            ['e'] = (KeyboardKey.E, false), ['E'] = (KeyboardKey.E, true),
            ['f'] = (KeyboardKey.F, false), ['F'] = (KeyboardKey.F, true),
            ['g'] = (KeyboardKey.G, false), ['G'] = (KeyboardKey.G, true),
            ['h'] = (KeyboardKey.H, false), ['H'] = (KeyboardKey.H, true),
            ['i'] = (KeyboardKey.I, false), ['I'] = (KeyboardKey.I, true),
            ['j'] = (KeyboardKey.J, false), ['J'] = (KeyboardKey.J, true),
            ['k'] = (KeyboardKey.K, false), ['K'] = (KeyboardKey.K, true),
            ['l'] = (KeyboardKey.L, false), ['L'] = (KeyboardKey.L, true),
            ['m'] = (KeyboardKey.M, false), ['M'] = (KeyboardKey.M, true),
            ['n'] = (KeyboardKey.N, false), ['N'] = (KeyboardKey.N, true),
            ['o'] = (KeyboardKey.O, false), ['O'] = (KeyboardKey.O, true),
            ['p'] = (KeyboardKey.P, false), ['P'] = (KeyboardKey.P, true),
            ['q'] = (KeyboardKey.Q, false), ['Q'] = (KeyboardKey.Q, true),
            ['r'] = (KeyboardKey.R, false), ['R'] = (KeyboardKey.R, true),
            ['s'] = (KeyboardKey.S, false), ['S'] = (KeyboardKey.S, true),
            ['t'] = (KeyboardKey.T, false), ['T'] = (KeyboardKey.T, true),
            ['u'] = (KeyboardKey.U, false), ['U'] = (KeyboardKey.U, true),
            ['v'] = (KeyboardKey.V, false), ['V'] = (KeyboardKey.V, true),
            ['w'] = (KeyboardKey.W, false), ['W'] = (KeyboardKey.W, true),
            ['x'] = (KeyboardKey.X, false), ['X'] = (KeyboardKey.X, true),
            ['y'] = (KeyboardKey.Y, false), ['Y'] = (KeyboardKey.Y, true),
            ['z'] = (KeyboardKey.Z, false), ['Z'] = (KeyboardKey.Z, true),
            [' '] = (KeyboardKey.Space, false),
            ['\''] = (KeyboardKey.Apostrophe, false), ['"'] = (KeyboardKey.Apostrophe, true),
            [','] = (KeyboardKey.Comma, false), ['<'] = (KeyboardKey.Comma, true),
            ['.'] = (KeyboardKey.Period, false), ['>'] = (KeyboardKey.Period, true),
            ['/'] = (KeyboardKey.Slash, false), ['?'] = (KeyboardKey.Slash, true),
            [';'] = (KeyboardKey.SemiColon, false), [':'] = (KeyboardKey.SemiColon, true),
            ['='] = (KeyboardKey.Equals, false), ['+'] = (KeyboardKey.Equals, true),
            ['-'] = (KeyboardKey.Minus, false), ['_'] = (KeyboardKey.Minus, true),
            ['['] = (KeyboardKey.LeftBracket, false), ['{'] = (KeyboardKey.LeftBracket, true),
            [']'] = (KeyboardKey.RightBracket, false), ['}'] = (KeyboardKey.RightBracket, true),
            ['\\'] = (KeyboardKey.Backslash, false), ['|'] = (KeyboardKey.Backslash, true),
            ['`'] = (KeyboardKey.GraveAccent, false), ['~'] = (KeyboardKey.GraveAccent, true),
            ['\t'] = (KeyboardKey.Tab, false),
            ['\n'] = (KeyboardKey.Enter, false),
        };
}

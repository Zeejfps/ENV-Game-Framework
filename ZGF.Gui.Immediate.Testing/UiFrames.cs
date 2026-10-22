using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>Drives a <see cref="Ui"/> frame by frame against a recording canvas with synthetic
/// text metrics (8px per char, 16px per line) unless given others, in a window of
/// <see cref="Width"/> by <see cref="Height"/>. Input queued between frames arrives in the next one.
/// Popups draw over the window unless a <see cref="TestPopupHost"/> is given.</summary>
public sealed class UiFrames
{
    private readonly RecordingCanvas _canvas;
    private readonly Ui _ui;
    private UiInput _frame = new();
    private UiInput _pending = new();

    public UiFrames(ITextMeasurer? measurer = null, TestPopupHost? popups = null, float width = 400f, float height = 300f)
    {
        _canvas = new RecordingCanvas(measurer ?? new SyntheticTextMeasurer());
        Popups = popups;
        Width = width;
        Height = height;
        _ui = new Ui(_canvas, Clipboard, popups);
    }

    public TestClipboard Clipboard { get; } = new();
    public TestFilePicker FilePicker { get; } = new();
    public TestPopupHost? Popups { get; }
    public RecordingCanvas Canvas => _canvas;
    public float Width { get; set; }
    public float Height { get; set; }
    public double RealTime { get; private set; }
    public bool RequestedFrame { get; private set; }
    public double WakeAt { get; private set; }

    /// <summary>The cursor the last frame asked for.</summary>
    public MouseCursor Cursor { get; private set; }

    /// <summary>Whether a widget that takes text held focus at the end of the last frame.</summary>
    public bool WantsTextInput { get; private set; }

    /// <summary>Where the last frame put the IME caret, in UI points.</summary>
    public UiRect? ImeCaret { get; private set; }

    /// <summary>How many frames discarded the in-flight composition.</summary>
    public int CompositionResets { get; private set; }

    /// <summary>Whether a frame asked the app to close with <see cref="Ui.Quit"/>.</summary>
    public bool QuitRequested { get; private set; }

    /// <summary>The window background the last frame asked for.</summary>
    public uint WindowBackground { get; private set; }

    /// <summary>What <see cref="Ui.WindowBounds"/> reports from the next frame.</summary>
    public WindowBounds WindowBounds { get; set; }

    public T Run<T>(float deltaTime, Func<Ui, T> body)
    {
        (_frame, _pending) = (_pending, _frame);
        _frame.Seal();
        _pending.BeginAfter(_frame);
        RealTime += deltaTime;
        _canvas.Reset();
        Popups?.BeginFrame();

        _ui.WindowBounds = WindowBounds;
        _ui.BeginFrame(_frame, deltaTime, RealTime, Width, Height);
        var value = body(_ui);
        _ui.EndFrame();
        RequestedFrame = _ui.FrameRequested;
        WakeAt = _ui.WakeAt;
        Cursor = _ui.Cursor;
        WantsTextInput = _ui.WantsTextInput;
        ImeCaret = _ui.ImeCaret;
        if (_ui.CompositionReset)
        {
            CompositionResets++;
            _pending.Compose(PreeditText.Empty);
        }
        QuitRequested = _ui.QuitRequested;
        WindowBackground = _ui.WindowBackground;
        if (_ui.ShowDialogs(FilePicker))
            _pending.DismissPopups();
        return value;
    }

    public void Run(float deltaTime, Action<Ui> body) => Run(deltaTime, ui =>
    {
        body(ui);
        return 0;
    });

    public int RectCount => _canvas.Rects.Count;

    public DrawRectInputs FirstRect => _canvas.Rects[0].Inputs;

    public uint TextColor(string text) => _canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style.TextColor;

    public bool Drew(string text) => _canvas.Texts.Any(t => t.Inputs.Text == text);

    public void Wait(double seconds) => RealTime += seconds;

    public void MovePointer(float x, float y) => _pending.MovePointer(new PointF(x, y));

    public void Press(KeyModifiers modifiers = KeyModifiers.None) => Press(PointerButton.Left, modifiers);

    public void Press(PointerButton button, KeyModifiers modifiers = KeyModifiers.None) => _pending.Button((int)button, InputAction.Press, modifiers);

    public void Release(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        _pending.Button((int)button, InputAction.Release, modifiers);

    public void Wheel(double y, double x = 0) => _pending.Scroll(x, y);

    public void DismissPopups() => _pending.DismissPopups();

    /// <summary>Another app takes the focus, as when the user switches to it.</summary>
    public void Background() => _pending.SetForeground(false);

    /// <summary>The app takes the focus back.</summary>
    public void Foreground() => _pending.SetForeground(true);

    /// <summary>The app lost focus: held buttons come up without the user releasing them.</summary>
    public void LoseFocus()
    {
        _pending.ReleaseAll();
        _pending.DismissPopups();
    }

    public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => _pending.Key(key, InputAction.Press, modifiers);

    public void Type(string text)
    {
        foreach (var rune in text.EnumerateRunes())
            _pending.Character((uint)rune.Value);
    }

    /// <summary>The IME replaces its composition with <paramref name="text"/>, caret at
    /// <paramref name="caret"/> (the end by default); an empty text ends it.</summary>
    public void Compose(string text, int? caret = null, IReadOnlyList<PreeditBlock>? blocks = null, int focusedBlock = -1) =>
        _pending.Compose(text.Length == 0 ? PreeditText.Empty : new PreeditText(text, caret ?? text.Length, blocks ?? [], focusedBlock));

    /// <summary>Runs a frame of <paramref name="body"/> and returns where the element with the id
    /// <paramref name="id"/> gives was laid out on the frame before.</summary>
    public UiRect RectOf(Action<Ui> body, Func<Ui, int> id) => Run(0f, ui =>
    {
        body(ui);
        return ui.TryGetRect(id(ui), out var rect) ? rect : throw new InvalidOperationException("Not laid out.");
    });

    /// <summary>Clicks the middle of the element <paramref name="id"/> gives, as <see cref="Click"/>.</summary>
    public void ClickOn(Action<Ui> body, Func<Ui, int> id, PointerButton button = PointerButton.Left)
    {
        var rect = RectOf(body, id);
        Click(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, body, button);
    }

    /// <summary>Moves to (x, y), lets a frame see the pointer there, then presses and releases
    /// <paramref name="button"/> over two more frames, running <paramref name="body"/> each time.</summary>
    public void Click(float x, float y, Action<Ui> body, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        MovePointer(x, y);
        Run(1f / 60f, body);
        Press(button, modifiers);
        Run(1f / 60f, body);
        Release(button, modifiers);
        Run(1f / 60f, body);
    }
}

using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>Drives a <see cref="Ui"/> frame by frame against a recording canvas with synthetic
/// text metrics (8px per char, 16px per line) unless given others. Input queued between frames
/// arrives in the next one. Popups draw over the window unless a <see cref="TestPopupHost"/> is
/// given.</summary>
internal sealed class UiFrames
{
    private readonly RecordingCanvas _canvas;
    private readonly Ui _ui;
    private UiInput _frame = new();
    private UiInput _pending = new();

    public UiFrames(ITextMeasurer? measurer = null, TestPopupHost? popups = null)
    {
        _canvas = new RecordingCanvas(measurer ?? new SyntheticTextMeasurer());
        Popups = popups;
        _ui = new Ui(_canvas, Clipboard, popups);
    }

    public TestClipboard Clipboard { get; } = new();
    public TestFilePicker FilePicker { get; } = new();
    public TestPopupHost? Popups { get; }
    public RecordingCanvas Canvas => _canvas;
    public double RealTime { get; private set; }
    public bool RequestedFrame { get; private set; }
    public double WakeAt { get; private set; }

    public T Run<T>(float deltaTime, Func<Ui, T> body)
    {
        (_frame, _pending) = (_pending, _frame);
        _frame.Seal();
        _pending.BeginAfter(_frame);
        RealTime += deltaTime;
        _canvas.Reset();
        Popups?.BeginFrame();

        _ui.BeginFrame(_frame, deltaTime, RealTime, 400f, 300f);
        var value = body(_ui);
        _ui.EndFrame();
        RequestedFrame = _ui.FrameRequested;
        WakeAt = _ui.WakeAt;
        _ui.ShowDialogs(FilePicker);
        return value;
    }

    public void Run(float deltaTime, Action<Ui> body) => Run(deltaTime, ui =>
    {
        body(ui);
        return 0;
    });

    public uint WindowBackground => _canvas.Rects[0].Inputs.Style.BackgroundColor;

    public uint TextColor(string text) => _canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style.TextColor;

    public bool Drew(string text) => _canvas.Texts.Any(t => t.Inputs.Text == text);

    public void Wait(double seconds) => RealTime += seconds;

    public void MovePointer(float x, float y) => _pending.MovePointer(new PointF(x, y));

    public void Press(KeyModifiers modifiers = KeyModifiers.None) => Press(PointerButton.Left, modifiers);

    public void Press(PointerButton button, KeyModifiers modifiers = KeyModifiers.None) => _pending.Button((int)button, InputAction.Press, modifiers);

    public void Release(PointerButton button = PointerButton.Left) => _pending.Button((int)button, InputAction.Release, KeyModifiers.None);

    public void Wheel(double y, double x = 0) => _pending.Scroll(x, y);

    public void DismissPopups() => _pending.DismissPopups();

    public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => _pending.Key(key, InputAction.Press, modifiers);

    public void Type(string text)
    {
        foreach (var rune in text.EnumerateRunes())
            _pending.Character((uint)rune.Value);
    }

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
    public void Click(float x, float y, Action<Ui> body, PointerButton button = PointerButton.Left)
    {
        MovePointer(x, y);
        Run(1f / 60f, body);
        Press(button);
        Run(1f / 60f, body);
        Release(button);
        Run(1f / 60f, body);
    }
}

internal sealed class TestClipboard : IClipboard
{
    public string? Text { get; set; }

    public void SetText(string text) => Text = text;

    public string? GetText() => Text;
}

internal sealed class TestFilePicker : IFilePicker
{
    public string? Answer { get; set; }
    public List<string> Titles { get; } = new();

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) => Pick(title, onPicked);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) => Pick(title, onPicked);

    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(title, onPicked);

    private void Pick(string title, Action<string> onPicked)
    {
        Titles.Add(title);
        if (Answer != null)
            onPicked(Answer);
    }
}

/// <summary>Gives every popup a recording canvas of its own, as windows would, inside a screen
/// <see cref="Area"/> in UI space.</summary>
internal sealed class TestPopupHost : IPopupHost
{
    private readonly Dictionary<int, RecordingCanvas> _canvases = new();

    public UiRect Area { get; set; } = new(-200f, -200f, 800f, 700f);
    public Dictionary<int, UiRect> Shown { get; } = new();

    public RecordingCanvas CanvasOf(int id) => _canvases[id];

    public void BeginFrame()
    {
        Shown.Clear();
        foreach (var canvas in _canvases.Values)
            canvas.Reset();
    }

    public UiRect Place(UiRect preferred, UiRect fallback) => PopupPlacement.Fit(preferred, fallback, Area);

    public ICanvas Show(int id, UiRect rect, bool passThrough)
    {
        Shown[id] = rect;
        if (!_canvases.TryGetValue(id, out var canvas))
            _canvases[id] = canvas = new RecordingCanvas(new SyntheticTextMeasurer());
        return canvas;
    }
}

using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>Drives a <see cref="Ui"/> frame by frame against a recording canvas with synthetic
/// text metrics (8px per char, 16px per line). Input queued between frames arrives in the next
/// one.</summary>
internal sealed class UiFrames
{
    private readonly RecordingCanvas _canvas = new(new SyntheticTextMeasurer());
    private readonly Ui _ui;
    private UiInput _frame = new();
    private UiInput _pending = new();

    public UiFrames()
    {
        _ui = new Ui(_canvas, Clipboard);
    }

    public TestClipboard Clipboard { get; } = new();
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

        _ui.BeginFrame(_frame, deltaTime, RealTime, 400f, 300f);
        var value = body(_ui);
        _ui.EndFrame();
        RequestedFrame = _ui.FrameRequested;
        WakeAt = _ui.WakeAt;
        return value;
    }

    public void Run(float deltaTime, Action<Ui> body) => Run(deltaTime, ui =>
    {
        body(ui);
        return 0;
    });

    public uint WindowBackground => _canvas.Rects[0].Inputs.Style.BackgroundColor;

    public uint TextColor(string text) => _canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style.TextColor;

    public void Wait(double seconds) => RealTime += seconds;

    public void MovePointer(float x, float y) => _pending.MovePointer(new PointF(x, y));

    public void Press(KeyModifiers modifiers = KeyModifiers.None) => _pending.Button(0, InputAction.Press, modifiers);

    public void Release() => _pending.Button(0, InputAction.Release, KeyModifiers.None);

    public void Wheel(double y) => _pending.Scroll(0, y);

    public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => _pending.Key(key, InputAction.Press, modifiers);

    public void Type(string text)
    {
        foreach (var rune in text.EnumerateRunes())
            _pending.Character((uint)rune.Value);
    }
}

internal sealed class TestClipboard : IClipboard
{
    public string? Text { get; set; }

    public void SetText(string text) => Text = text;

    public string? GetText() => Text;
}

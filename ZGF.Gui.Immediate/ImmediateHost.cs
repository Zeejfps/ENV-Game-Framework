using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

internal sealed class ImmediateHost : IDisposable
{
    private const float MaxFrameStep = 0.1f;

    private readonly RenderHost _render;
    private readonly IWindow _window;
    private readonly Action<Ui> _renderUi;
    private readonly Ui _ui;
    private UiInput _pending = new();
    private UiInput _frame = new();
    private readonly long _startTimestamp = Stopwatch.GetTimestamp();
    private long _lastFrameTimestamp = Stopwatch.GetTimestamp();
    private double _wakeAt = double.PositiveInfinity;
    private bool _textInputFocus;
    private bool _animating;
    private MouseCursor _cursor;

    public ImmediateHost(StartupConfig config, RenderBackendKind backend, UiTheme theme, Action<Ui> renderUi)
    {
        _render = RenderHost.Create(config, backend);
        _window = _render.App.MainWindow;
        _renderUi = renderUi;
        _ui = new Ui(_render.Canvas, theme, new WindowClipboard(_window));
        _render.SetFrame(DrawFrame, theme.Background);

        _window.OnKey += HandleKey;
        _window.OnText += HandleText;
        _window.OnMouseButton += HandleMouseButton;
        _window.OnScroll += HandleScroll;
        _window.OnFocusChanged += HandleFocusChanged;
        _window.OnResize += HandleResize;
        _window.OnFramebufferResize += HandleResize;
        _window.OnContentScaleChanged += HandleContentScaleChanged;
        _render.App.OnTick += Tick;
        HotReload.Applied += Invalidate;

        SyncScale();
    }

    public void Run() => _render.App.Run();

    public void Invalidate()
    {
        _window.RequestRedraw();
        _render.App.Wake();
    }

    private void DrawFrame()
    {
        (_frame, _pending) = (_pending, _frame);
        _frame.Seal();
        _pending.BeginAfter(_frame);

        var now = Stopwatch.GetTimestamp();
        var dt = _animating ? (float)((now - _lastFrameTimestamp) / (double)Stopwatch.Frequency) : 0f;
        _lastFrameTimestamp = now;

        _ui.BeginFrame(_frame, MathF.Min(dt, MaxFrameStep), Seconds(now), _render.Canvas.Width, _render.Canvas.Height);
        _renderUi(_ui);
        _ui.EndFrame();

        if (_ui.Cursor != _cursor)
        {
            _cursor = _ui.Cursor;
            _window.SetCursor(_cursor);
        }
        if (_ui.WantsTextInput != _textInputFocus)
        {
            _textInputFocus = _ui.WantsTextInput;
            _window.SetTextInputFocus(_textInputFocus);
        }
        _animating = _ui.FrameRequested;
        _wakeAt = _ui.WakeAt;
        if (_frame.HasEvents || _ui.FrameRequested)
            _window.RequestRedraw();
    }

    private double Seconds(long timestamp) => (timestamp - _startTimestamp) / (double)Stopwatch.Frequency;

    private void Tick()
    {
        if (_wakeAt <= Seconds(Stopwatch.GetTimestamp()))
        {
            _wakeAt = double.PositiveInfinity;
            _window.RequestRedraw();
        }
        PollPointer();
    }

    private void PollPointer()
    {
        _window.GetCursorPosition(out var x, out var y);
        var inside = x >= 0 && y >= 0 && x <= _window.Width && y <= _window.Height;
        var dragging = _pending.IsDown(PointerButton.Left) || _pending.IsDown(PointerButton.Right) || _pending.IsDown(PointerButton.Middle);
        var point = inside || dragging ? ToUiSpace(x, y) : UiInput.Offscreen;
        if (point == _pending.Pointer) return;
        _pending.MovePointer(point);
        _window.RequestRedraw();
    }

    private PointF ToUiSpace(double windowX, double windowY)
    {
        var canvas = _render.Canvas;
        return new PointF(
            (float)(windowX * canvas.Width / Math.Max(1, _window.Width)),
            (float)(windowY * canvas.Height / Math.Max(1, _window.Height)));
    }

    private void HandleKey(KeyboardKey key, InputAction action, KeyModifiers modifiers)
    {
        _pending.Key(key, action, modifiers);
        _window.RequestRedraw();
    }

    private void HandleText(uint codePoint)
    {
        _pending.Character(codePoint);
        _window.RequestRedraw();
    }

    private void HandleMouseButton(int button, InputAction action, KeyModifiers modifiers)
    {
        PollPointer();
        _pending.Button(button, action, modifiers);
        _window.RequestRedraw();
    }

    private void HandleScroll(double x, double y)
    {
        _pending.Scroll(x, y);
        _window.RequestRedraw();
    }

    private void HandleFocusChanged(bool focused)
    {
        if (!focused)
            _pending.ReleaseAll();
        _window.RequestRedraw();
    }

    private void HandleResize(int width, int height)
    {
        SyncScale();
        _render.RenderNow();
    }

    private void HandleContentScaleChanged(float scale)
    {
        SyncScale();
        _window.RequestRedraw();
    }

    private void SyncScale()
    {
        var scale = _window.ContentScale > 0f ? _window.ContentScale : 1f;
        var canvas = _render.Canvas;
        canvas.UpdateDpiScale(scale);
        canvas.Resize(
            (int)MathF.Max(1f, MathF.Round(Math.Max(1, _window.FramebufferWidth) / scale)),
            (int)MathF.Max(1f, MathF.Round(Math.Max(1, _window.FramebufferHeight) / scale)));
    }

    public void Dispose()
    {
        HotReload.Applied -= Invalidate;
        _render.App.OnTick -= Tick;
        _window.OnKey -= HandleKey;
        _window.OnText -= HandleText;
        _window.OnMouseButton -= HandleMouseButton;
        _window.OnScroll -= HandleScroll;
        _window.OnFocusChanged -= HandleFocusChanged;
        _window.OnResize -= HandleResize;
        _window.OnFramebufferResize -= HandleResize;
        _window.OnContentScaleChanged -= HandleContentScaleChanged;
        _render.Dispose();
    }
}

internal sealed class WindowClipboard(IWindow window) : IClipboard
{
    public void SetText(string text) => window.SetClipboardText(text);

    public string? GetText() => window.GetClipboardText();
}

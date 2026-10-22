using System.Collections.Concurrent;
using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

internal sealed class ImmediateHost : IDisposable, IPopupHost
{
    private const float MaxFrameStep = 0.1f;
    private const int PopupPoolSize = 8;

    private sealed class Popup(PopupSurface surface)
    {
        public readonly PopupSurface Surface = surface;
        public int Id;
        public UiRect Rect;
        public bool PassThrough;
        public bool Used;
        public bool Shown;
        public (int X, int Y, int Width, int Height) Screen;
    }

    private readonly RenderHost _render;
    private readonly IWindow _window;
    private readonly Action<Ui> _renderUi;
    private readonly Ui _ui;
    private readonly IFilePicker _filePicker;
    private readonly IPopupNativeDecorator _decorator;
    private readonly List<Popup> _popups = new();
    private readonly List<Popup> _pool = new();
    private UiInput _pending = new();
    private UiInput _frame = new();
    private readonly long _startTimestamp = Stopwatch.GetTimestamp();
    private long _lastFrameTimestamp = Stopwatch.GetTimestamp();
    private double _wakeAt = double.PositiveInfinity;
    private bool _textInputFocus;
    private (int X, int Y, int Width, int Height)? _imeCaret;
    private bool _animating;
    private bool _popupWindowsUnavailable;
    private bool _backgroundPending;
    private MouseCursor _cursor;
    private uint _clearColor;
    private bool? _titleBarDark;
    private volatile bool _disposed;
    private (int X, int Y, int Width, int Height) _normalBounds;

    public ImmediateHost(
        StartupConfig config,
        RenderBackendKind backend,
        IReadOnlyList<(string Family, byte[] Data, bool Bold)> fonts,
        ConcurrentQueue<Action> posted,
        Action<Ui> renderUi)
    {
        _render = RenderHost.Create(config, backend);
        foreach (var (family, data, bold) in fonts)
            _render.RegisterFont(family, data, bold);
        _window = _render.App.MainWindow;
        _renderUi = renderUi;
        _decorator = PopupDecorator.ForCurrentPlatform();
        _ui = new Ui(_render.Canvas, new WindowClipboard(_window), this, posted) { Wake = Invalidate };
        _filePicker = NativeFilePicker.Create(_ui.Post);
        _clearColor = _ui.WindowBackground;
        _render.SetFrame(DrawFrame, _clearColor);
        _normalBounds = (config.WindowX ?? 0, config.WindowY ?? 0, config.WindowWidth, config.WindowHeight);

        Subscribe(_window);
        _window.OnResize += HandleResize;
        _window.OnFramebufferResize += HandleResize;
        _window.OnContentScaleChanged += HandleContentScaleChanged;
        _window.OnMove += HandleMove;
        _render.App.OnForegroundChanged += HandleForegroundChanged;
        _render.App.OnTick += Tick;
        _decorator.WatchWindowNonClientPress(_window.NativeHandle, HandleNonClientPress);
        HotReload.Applied += Invalidate;

        SyncScale();
        LoadSystemFallbackFonts();
    }

    public void Run()
    {
        TrackBounds();
        _render.RenderNow();
        _render.App.Run();
    }

    /// <summary>The main window's bounds: where it sits and how big it is when not maximized.</summary>
    public WindowBounds Bounds => new(_normalBounds.X, _normalBounds.Y, _normalBounds.Width, _normalBounds.Height, _window.IsMaximized);

    public void Invalidate()
    {
        if (_disposed) return;
        _window.RequestRedraw();
        _render.App.Wake();
    }

    public void RegisterFont(string family, byte[] data, bool bold)
    {
        _render.RegisterFont(family, data, bold);
        _ui.InvalidateMeasurements();
        foreach (var popup in _popups.Concat(_pool))
            popup.Surface.Canvas.CopyFontsFrom(_render.Canvas);
        Invalidate();
    }

    private void LoadSystemFallbackFonts() => Task.Run(() =>
    {
        foreach (var font in SystemFallbackFonts.Find())
        {
            if (_disposed) return;
            byte[] data;
            try
            {
                data = File.ReadAllBytes(font.Path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ImmediateHost] Could not read fallback font {font.Path}: {e.Message}");
                continue;
            }
            _ui.Post(() => RegisterFallbackFont(data, font));
        }
    });

    private void RegisterFallbackFont(byte[] data, SystemFontFile font)
    {
        if (_disposed) return;
        try
        {
            _render.RegisterFallbackFont(data, font.FaceIndex);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ImmediateHost] Could not load fallback font {font.Path}: {e.Message}");
            return;
        }
        _ui.InvalidateMeasurements();
    }

    private void DrawFrame()
    {
        (_frame, _pending) = (_pending, _frame);
        _frame.Seal();
        _pending.BeginAfter(_frame);

        var now = Stopwatch.GetTimestamp();
        var dt = _animating ? (float)((now - _lastFrameTimestamp) / (double)Stopwatch.Frequency) : 0f;
        _lastFrameTimestamp = now;

        _ui.WindowBounds = Bounds;
        _ui.BeginFrame(_frame, MathF.Min(dt, MaxFrameStep), Seconds(now), _render.Canvas.Width, _render.Canvas.Height);
        _renderUi(_ui);
        _ui.EndFrame();
        if (_ui.WindowBackground != _clearColor)
        {
            _clearColor = _ui.WindowBackground;
            _render.SetFrame(DrawFrame, _clearColor);
        }
        var dark = UiColor.IsDark(_clearColor);
        if (dark != _titleBarDark)
        {
            _titleBarDark = dark;
            _window.SetTitleBarTheme(dark);
        }
        PresentPopups();
        if (_ui.QuitRequested)
            _render.App.Quit();

        if (_ui.Cursor != _cursor)
        {
            _cursor = _ui.Cursor;
            _window.SetCursor(_cursor);
            foreach (var popup in _popups)
                popup.Surface.Window.SetCursor(_cursor);
        }
        if (_ui.CompositionReset)
        {
            _window.ResetPreedit();
            _pending.Compose(PreeditText.Empty);
        }
        if (_ui.WantsTextInput != _textInputFocus)
        {
            _textInputFocus = _ui.WantsTextInput;
            _window.SetTextInputFocus(_textInputFocus);
            _imeCaret = null;
        }
        if (_textInputFocus && _ui.ImeCaret is { } caret)
            PlaceImeCaret(caret);
        _animating = _ui.FrameRequested;
        _wakeAt = _ui.WakeAt;
        if (_frame.HasEvents || _ui.FrameRequested)
            _window.RequestRedraw();
    }

    private void PlaceImeCaret(UiRect caret)
    {
        var s = ScreenPerPoint;
        var x = (int)MathF.Floor(caret.X * s);
        var y = (int)MathF.Floor(caret.Y * s);
        var rect = (X: x, Y: y,
            Width: Math.Max(1, (int)MathF.Ceiling(caret.Right * s) - x),
            Height: Math.Max(1, (int)MathF.Ceiling(caret.Bottom * s) - y));
        if (rect == _imeCaret) return;
        _imeCaret = rect;
        _window.SetPreeditCursorRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private double Seconds(long timestamp) => (timestamp - _startTimestamp) / (double)Stopwatch.Frequency;

    private void Tick()
    {
        if (_backgroundPending)
        {
            _backgroundPending = false;
            if (!_render.App.IsForeground)
            {
                _pending.ReleaseAll();
                _pending.DismissPopups();
                _window.RequestRedraw();
            }
        }
        if (_ui.HasDialogs && _ui.ShowDialogs(_filePicker))
        {
            _pending.DismissPopups();
            _window.RequestRedraw();
        }
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
        var ui = ToUiSpace(x, y);
        var point = inside || _pending.AnyDown || OverPopup(ui) ? ui : UiInput.Offscreen;
        if (point == _pending.Pointer) return;
        _pending.MovePointer(point);
        _window.RequestRedraw();
    }

    private bool OverPopup(PointF point)
    {
        foreach (var popup in _popups)
        {
            if (!popup.PassThrough && popup.Rect.Contains(point))
                return true;
        }
        return false;
    }

    private float ScreenPerPoint => _window.Width / (float)Math.Max(1, _render.Canvas.Width);

    private PointF ToUiSpace(double windowX, double windowY)
    {
        var canvas = _render.Canvas;
        return new PointF(
            (float)(windowX * canvas.Width / Math.Max(1, _window.Width)),
            (float)(windowY * canvas.Height / Math.Max(1, _window.Height)));
    }

    public UiRect Place(UiRect preferred, UiRect fallback)
    {
        var window = new UiRect(0f, 0f, _render.Canvas.Width, _render.Canvas.Height);
        if (_popupWindowsUnavailable)
            return PopupPlacement.Fit(preferred, fallback, window);

        var area = MonitorArea(preferred) ?? window;
        return PopupPlacement.SnapToPixels(PopupPlacement.Fit(preferred, fallback, area), _render.Canvas.DpiScale);
    }

    private UiRect? MonitorArea(UiRect rect)
    {
        var monitors = _render.App.Monitors;
        if (monitors.Count == 0) return null;

        var s = ScreenPerPoint;
        _window.GetPosition(out var originX, out var originY);
        var anchorX = originX + rect.X * s;
        var anchorY = originY + rect.Y * s;
        var best = monitors[0];
        var bestDistance = float.MaxValue;
        foreach (var monitor in monitors)
        {
            var dx = MathF.Max(0f, MathF.Max(monitor.X - anchorX, anchorX - (monitor.X + monitor.Width - 1)));
            var dy = MathF.Max(0f, MathF.Max(monitor.Y - anchorY, anchorY - (monitor.Y + monitor.Height - 1)));
            var distance = dx * dx + dy * dy;
            if (distance >= bestDistance) continue;
            best = monitor;
            bestDistance = distance;
        }
        return new UiRect((best.X - originX) / s, (best.Y - originY) / s, best.Width / s, best.Height / s);
    }

    public ICanvas? Show(int id, UiRect rect, UiRect surface, bool passThrough)
    {
        if (_popupWindowsUnavailable) return null;

        var popup = Find(id) ?? Acquire();
        if (popup == null) return null;

        popup.Id = id;
        popup.Used = true;
        popup.Rect = rect;
        if (popup.PassThrough != passThrough || !popup.Shown)
        {
            popup.PassThrough = passThrough;
            popup.Surface.Window.SetMousePassThrough(passThrough);
        }

        var s = ScreenPerPoint;
        var dpi = _render.Canvas.DpiScale;
        var pixelsPerScreen = _window.DpiScale > 0f ? _window.DpiScale : 1f;
        var widthPoints = Math.Max(1, (int)MathF.Ceiling(surface.Width));
        var heightPoints = Math.Max(1, (int)MathF.Ceiling(surface.Height));
        _window.GetPosition(out var originX, out var originY);
        var screen = (
            X: originX + (int)MathF.Round(surface.X * s),
            Y: originY + (int)MathF.Round(surface.Y * s),
            Width: PopupPlacement.ScreenSize(widthPoints, dpi, pixelsPerScreen),
            Height: PopupPlacement.ScreenSize(heightPoints, dpi, pixelsPerScreen));
        var window = popup.Surface.Window;
        if (screen.Width != popup.Screen.Width || screen.Height != popup.Screen.Height)
            window.SetSize(screen.Width, screen.Height);
        if (screen.X != popup.Screen.X || screen.Y != popup.Screen.Y || !popup.Shown)
            window.SetPosition(screen.X, screen.Y);
        popup.Screen = screen;

        _render.ResizePopup(popup.Surface, widthPoints, heightPoints, dpi);
        popup.Surface.Canvas.BeginFrame();
        return popup.Surface.Canvas;
    }

    private Popup? Find(int id)
    {
        foreach (var popup in _popups)
        {
            if (popup.Id == id && !popup.Used)
                return popup;
        }
        return null;
    }

    private Popup? Acquire()
    {
        Popup popup;
        if (_pool.Count > 0)
        {
            popup = _pool[^1];
            _pool.RemoveAt(_pool.Count - 1);
            popup.Surface.Window.SetCursor(_cursor);
        }
        else
        {
            PopupSurface surface;
            try
            {
                surface = _render.CreatePopup(passThrough: false);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ImmediateHost] Popup windows are unavailable, drawing popups in the window: {e.Message}");
                _popupWindowsUnavailable = true;
                return null;
            }
            _decorator.DecoratePopup(surface.Window.NativeHandle);
            Subscribe(surface.Window);
            surface.Window.SetCursor(_cursor);
            popup = new Popup(surface);
        }
        _popups.Add(popup);
        return popup;
    }

    public void HideAll()
    {
        foreach (var popup in _popups)
            Release(popup);
        _popups.Clear();
    }

    private void PresentPopups()
    {
        for (var i = _popups.Count - 1; i >= 0; i--)
        {
            var popup = _popups[i];
            if (popup.Used) continue;
            _popups.RemoveAt(i);
            Release(popup);
        }

        foreach (var popup in _popups)
        {
            _render.RenderPopup(popup.Surface);
            if (!popup.Shown)
            {
                popup.Surface.Window.Show();
                popup.Shown = true;
            }
            popup.Used = false;
        }
    }

    private void Release(Popup popup)
    {
        var window = popup.Surface.Window;
        var hadFocus = window.IsFocused;
        window.Hide();
        popup.Shown = false;
        popup.Id = 0;
        popup.Screen = default;
        if (hadFocus)
            _window.Focus();

        if (_pool.Count < PopupPoolSize)
        {
            _pool.Add(popup);
            return;
        }
        DisposePopup(popup);
    }

    private void DisposePopup(Popup popup)
    {
        Unsubscribe(popup.Surface.Window);
        _render.DisposePopup(popup.Surface);
    }

    private void Subscribe(IWindow window)
    {
        window.OnKey += HandleKey;
        window.OnText += HandleText;
        window.OnPreedit += HandlePreedit;
        window.OnMouseButton += HandleMouseButton;
        window.OnScroll += HandleScroll;
    }

    private void Unsubscribe(IWindow window)
    {
        window.OnKey -= HandleKey;
        window.OnText -= HandleText;
        window.OnPreedit -= HandlePreedit;
        window.OnMouseButton -= HandleMouseButton;
        window.OnScroll -= HandleScroll;
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

    private void HandlePreedit(PreeditText preedit)
    {
        _pending.Compose(preedit);
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

    private void HandleForegroundChanged(bool foreground)
    {
        _pending.SetForeground(foreground);
        _backgroundPending = !foreground;
        _window.RequestRedraw();
    }

    private void HandleNonClientPress()
    {
        _pending.DismissPopups();
        _window.RequestRedraw();
    }

    private void HandleMove(int x, int y)
    {
        TrackBounds();
        _pending.DismissPopups();
        _window.RequestRedraw();
    }

    private void TrackBounds()
    {
        if (_window.IsMaximized || _window.Width <= 0 || _window.Height <= 0) return;
        _window.GetPosition(out var x, out var y);
        _normalBounds = (x, y, _window.Width, _window.Height);
    }

    private void HandleResize(int width, int height)
    {
        TrackBounds();
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
        _disposed = true;
        _ui.Wake = null;
        HotReload.Applied -= Invalidate;
        _decorator.UnwatchWindow(_window.NativeHandle);
        _render.App.OnTick -= Tick;
        _render.App.OnForegroundChanged -= HandleForegroundChanged;
        Unsubscribe(_window);
        _window.OnResize -= HandleResize;
        _window.OnFramebufferResize -= HandleResize;
        _window.OnContentScaleChanged -= HandleContentScaleChanged;
        _window.OnMove -= HandleMove;
        foreach (var popup in _popups.Concat(_pool))
            DisposePopup(popup);
        _popups.Clear();
        _pool.Clear();
        _render.Dispose();
    }
}

internal sealed class WindowClipboard(IWindow window) : IClipboard
{
    public void SetText(string text) => window.SetClipboardText(text);

    public string? GetText() => window.GetClipboardText();
}

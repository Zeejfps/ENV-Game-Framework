using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

internal sealed class ImmediateHost : IDisposable, IPopupHost
{
    private const float MaxFrameStep = 0.1f;
    private const int PopupPoolSize = 8;

    private sealed class Popup(PopupSurface surface, bool passThrough)
    {
        public readonly PopupSurface Surface = surface;
        public int Id;
        public UiRect Rect;
        public bool PassThrough = passThrough;
        public bool Used;
        public bool Shown;
        public (int X, int Y, int Width, int Height) Screen;
    }

    private readonly IGuiRenderBackend _render;
    private readonly IWindow _window;
    private readonly Action<Ui> _renderUi;
    private readonly Ui _ui;
    private readonly IFilePicker _filePicker;
    private readonly List<Popup> _popups = new();
    private readonly PopupPool _pool;
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
        Action<Ui> renderUi)
    {
        _render = CreateRenderBackend(config, backend);
        RegisterDefaultBold();
        foreach (var (family, data, bold) in fonts)
            AddFont(family, data, bold);
        _window = _render.App.MainWindow;
        _pool = new PopupPool(_render, PopupPoolSize);
        _renderUi = renderUi;
        _ui = new Ui(_render.MainCanvas, new WindowClipboard(_window), this) { Wake = Invalidate };
        _filePicker = NativeFilePicker.Create(_ui.Post);
        _clearColor = _ui.WindowBackground;
        WireMainFrame();
        _normalBounds = (config.WindowX ?? 0, config.WindowY ?? 0, config.WindowWidth, config.WindowHeight);

        Subscribe(_window);
        _window.OnResize += HandleResize;
        _window.OnFramebufferResize += HandleResize;
        _window.OnContentScaleChanged += HandleContentScaleChanged;
        _window.OnMove += HandleMove;
        _render.App.OnForegroundChanged += HandleForegroundChanged;
        _render.App.OnTick += Tick;
        _window.OnNonClientPress += HandleNonClientPress;
        HotReload.Applied += Invalidate;

        SyncScale();
        LoadSystemFallbackFonts();
    }

    private static IGuiRenderBackend CreateRenderBackend(StartupConfig config, RenderBackendKind kind) => kind switch
    {
        RenderBackendKind.OpenGl => new GlRenderBackend(config),
        RenderBackendKind.Metal => new MetalRenderBackend(config),
        _ => IGuiRenderBackend.CreateForPlatform(config),
    };

    /// <summary>Makes the default font's bold text Inter SemiBold rather than synthesized.</summary>
    private void RegisterDefaultBold() =>
        _render.MainCanvas.RegisterBoldFont(null, LoadFont(EmbeddedAssets.LoadFontBytes("Inter-SemiBold.ttf")));

    private void AddFont(string family, byte[] data, bool bold)
    {
        var handle = LoadFont(data);
        if (bold)
            _render.MainCanvas.RegisterBoldFont(family, handle);
        else
            _render.MainCanvas.RegisterFont(family, handle);
    }

    private FontHandle LoadFont(byte[] data, int faceIndex = 0) =>
        _render.Fonts.LoadFontFromMemory(data, GuiFonts.PixelSize(_render.MainCanvas.DpiScale), faceIndex);

    private void WireMainFrame() =>
        _render.WireRenderLoop(_window, _render.MainCanvas, DrawFrame, ClearColorOf(_clearColor));

    private static (float R, float G, float B, float A) ClearColorOf(uint argb) => (
        ((argb >> 16) & 0xFF) / 255f,
        ((argb >> 8) & 0xFF) / 255f,
        (argb & 0xFF) / 255f,
        ((argb >> 24) & 0xFF) / 255f);

    public void Run()
    {
        TrackBounds();
        _render.RenderWindowNow(_window);
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

    public void Post(Action action)
    {
        _ui.Post(action);
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
            _render.Fonts.RegisterFallbackFont(LoadFont(data, font.FaceIndex));
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
        _ui.BeginFrame(_frame, MathF.Min(dt, MaxFrameStep), Seconds(now), _render.MainCanvas.Width, _render.MainCanvas.Height);
        _renderUi(_ui);
        _ui.EndFrame();
        if (_ui.WindowBackground != _clearColor)
        {
            _clearColor = _ui.WindowBackground;
            WireMainFrame();
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

    private float ScreenPerPoint => _window.Width / (float)Math.Max(1, _render.MainCanvas.Width);

    private PointF ToUiSpace(double windowX, double windowY)
    {
        var canvas = _render.MainCanvas;
        return new PointF(
            (float)(windowX * canvas.Width / Math.Max(1, _window.Width)),
            (float)(windowY * canvas.Height / Math.Max(1, _window.Height)));
    }

    public UiRect Place(UiRect preferred, UiRect fallback)
    {
        var window = new UiRect(0f, 0f, _render.MainCanvas.Width, _render.MainCanvas.Height);
        if (_popupWindowsUnavailable)
            return PopupPlacement.Fit(preferred, fallback, window);

        var area = MonitorArea(preferred) ?? window;
        return PopupPlacement.SnapToPixels(PopupPlacement.Fit(preferred, fallback, area), _render.MainCanvas.DpiScale);
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

        var popup = Find(id) ?? Acquire(passThrough);
        if (popup == null) return null;

        popup.Id = id;
        popup.Used = true;
        popup.Rect = rect;
        if (popup.PassThrough != passThrough)
        {
            popup.PassThrough = passThrough;
            popup.Surface.Window.SetMousePassThrough(passThrough);
        }

        var s = ScreenPerPoint;
        var dpi = _render.MainCanvas.DpiScale;
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

        ResizeCanvas(popup.Surface.Canvas, widthPoints, heightPoints, dpi);
        popup.Surface.Canvas.BeginFrame();
        return popup.Surface.Canvas;
    }

    /// <summary>Sizes a popup's canvas, in UI points, and its scale.</summary>
    private static void ResizeCanvas(RenderedCanvasBase canvas, int width, int height, float dpiScale)
    {
        if (canvas.Width == width && canvas.Height == height && canvas.DpiScale == dpiScale) return;
        canvas.UpdateDpiScale(dpiScale);
        canvas.Resize(width, height);
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

    private Popup? Acquire(bool passThrough)
    {
        PopupSurface surface;
        try
        {
            surface = _pool.Acquire(passThrough);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ImmediateHost] Popup windows are unavailable, drawing popups in the window: {e.Message}");
            _popupWindowsUnavailable = true;
            return null;
        }
        _render.WirePresentLoop(surface.Window, surface.Canvas);
        Subscribe(surface.Window);
        surface.Window.SetCursor(_cursor);
        var popup = new Popup(surface, passThrough);
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
            _render.RenderWindowNow(popup.Surface.Window);
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
        Unsubscribe(window);
        _pool.Release(popup.Surface);
        if (hadFocus)
            _window.Focus();
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
        _render.RenderWindowNow(_window);
    }

    private void HandleContentScaleChanged(float scale)
    {
        SyncScale();
        _window.RequestRedraw();
    }

    private void SyncScale()
    {
        var scale = _window.ContentScale > 0f ? _window.ContentScale : 1f;
        var canvas = _render.MainCanvas;
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
        _window.OnNonClientPress -= HandleNonClientPress;
        _render.App.OnTick -= Tick;
        _render.App.OnForegroundChanged -= HandleForegroundChanged;
        Unsubscribe(_window);
        _window.OnResize -= HandleResize;
        _window.OnFramebufferResize -= HandleResize;
        _window.OnContentScaleChanged -= HandleContentScaleChanged;
        _window.OnMove -= HandleMove;
        HideAll();
        _pool.Dispose();
        _render.Dispose();
    }
}

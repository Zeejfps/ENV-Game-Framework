using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using FreeTypeSharp;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

/// <summary>The window, render loop and UI thread a <see cref="ImmediateGuiApp{TUiState}"/> runs in. While it runs,
/// <c>await</c> on the UI thread continues before the next frame.</summary>
public sealed class ImmediateGuiAppHost<TUiState> : IPopupHost where TUiState : class, IUiState
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

    private sealed class DialogWindow(int id, IWindow window, RenderedCanvasBase canvas, Action wake)
    {
        public readonly int Id = id;
        public readonly IWindow Window = window;
        public readonly RenderedCanvasBase Canvas = canvas;
        public UiRect Rect;
        public bool Used;
        public bool Shown;
        public bool CloseRequested;

        /// <summary>Keeps the window, which closes once the UI stops showing it.</summary>
        public void HandleClose()
        {
            Window.CancelClose();
            CloseRequested = true;
            wake();
        }
    }

    private readonly IGuiRenderBackend _render;
    private readonly IWindow _window;
    private readonly Action<Ui<TUiState>> _updateUi;
    private readonly Ui<TUiState> _ui;
    private readonly IFilePicker _filePicker;
    private readonly ISystemFonts _systemFonts = NativeSystemFonts.Create();
    private readonly FontRegistry _fontRegistry;
    private readonly List<Popup> _popups = new();
    private readonly List<DialogWindow> _windows = new();
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
    private bool _dialogWindowsUnavailable;
    private bool _drawing;
    private bool _backgroundPending;
    private MouseCursor _cursor;
    private uint _clearColor;
    private bool? _titleBarDark;
    private float _scale = 1f;
    private volatile bool _disposed;
    private (int X, int Y, int Width, int Height) _normalBounds;

    internal ImmediateGuiAppHost(
        StartupConfig config,
        RenderBackendKind backend,
        TUiState uiState,
        Action<Ui<TUiState>> updateUi)
    {
        _render = CreateRenderBackend(config, backend);
        _fontRegistry = new FontRegistry(_render.Fonts, _render.MainCanvas, LoadFont);
        RegisterDefaultBold();
        _window = _render.App.MainWindow;
        _pool = new PopupPool(_render, PopupPoolSize);
        _updateUi = updateUi;
        _ui = new Ui<TUiState>(uiState, _render.MainCanvas, new WindowClipboard(_window), this);
        _ui.Wake = Invalidate;
        _filePicker = NativeFilePicker.Create(_ui.Post);
        _clearColor = _ui.WindowBackground;
        WireMainFrame();
        _normalBounds = (config.WindowX ?? 0, config.WindowY ?? 0, config.WindowWidth, config.WindowHeight);

        Subscribe(_window);
        _window.OnResize += HandleResize;
        _window.OnFramebufferResize += HandleResize;
        _window.OnContentScaleChanged += HandleContentScaleChanged;
        _window.OnMove += HandleMove;
        _window.OnFocusChanged += HandleFocusChanged;
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

    private FontHandle LoadFont(byte[] data, int faceIndex = 0) =>
        _render.Fonts.LoadFontFromMemory(data, GuiFonts.PixelSize(_render.MainCanvas.DpiScale), faceIndex);

    private void WireMainFrame() =>
        _render.WireRenderLoop(_window, _render.MainCanvas, DrawFrame, ClearColorOf(_clearColor));

    private static (float R, float G, float B, float A) ClearColorOf(uint argb) => (
        ((argb >> 16) & 0xFF) / 255f,
        ((argb >> 8) & 0xFF) / 255f,
        (argb & 0xFF) / 255f,
        ((argb >> 24) & 0xFF) / 255f);

    internal void RunUntilClosed()
    {
        TrackBounds();
        _render.RenderWindowNow(_window);
        _render.App.Run();
    }

    /// <summary>The window's bounds: where it sits and how big it is when not maximized, and once it
    /// has closed, where it was left — save them and reopen with <see cref="WindowBounds.ApplyTo"/>.</summary>
    public WindowBounds WindowBounds => new(_normalBounds.X, _normalBounds.Y, _normalBounds.Width, _normalBounds.Height, _window.IsMaximized);

    /// <summary>Schedules an update after the app's state changed outside a frame. Safe from any thread,
    /// and does nothing once the window has closed.</summary>
    public void Invalidate()
    {
        if (_disposed) return;
        _window.RequestRedraw();
        _render.App.Wake();
    }

    /// <summary>Makes <c>await</c> on this thread continue on the UI thread before the next frame,
    /// until the returned scope is disposed.</summary>
    internal UiSynchronizationContext.Scope InstallSynchronizationContext()
    {
        var context = new UiSynchronizationContext(_ui.Post);
        return context.Install();
    }

    /// <summary>Shows <paramref name="icon"/> on the window where the platform draws one, such as the
    /// title bar and taskbar on Windows; macOS takes the app's icon from its bundle instead.</summary>
    public void SetIcon(WindowIcon icon)
    {
        if (_disposed) return;
        _window.SetIcon(icon.Images);
    }

    /// <summary>Makes <paramref name="font"/> drawable as its family, for <c>ui.Text(family: ...)</c>
    /// and <see cref="UiTheme.FontFamily"/>. A bold face is the one the family's bold text is drawn
    /// in, which is otherwise synthesized from the regular face; the default font's bold is Inter
    /// SemiBold. Register italic faces under families of their own. Call it on the UI thread, such as
    /// from <see cref="ImmediateGuiApp{TUiState}.OnStartup"/> or after awaiting a background load there; it
    /// does nothing once the window has closed, or when the data is no font that can be loaded, which
    /// leaves the family drawn as before. Registering the bytes a face was loaded from again reuses it;
    /// the face a family was drawn with before is released once no family draws with it, between frames.</summary>
    public void RegisterFont(FontFace font)
    {
        if (_disposed) return;
        if (!TryAddFont(font)) return;
        FollowFontChange();
    }

    /// <summary>Draws text in <paramref name="family"/> in the default font again, as before it was
    /// registered, releasing its faces between frames once no family draws with them. Call it on the
    /// UI thread, as <see cref="RegisterFont"/>.</summary>
    public void UnregisterFont(string family)
    {
        if (_disposed) return;
        _fontRegistry.Unregister(family);
        FollowFontChange();
    }

    /// <summary>Measures text anew, has every popup and dialog draw with the main canvas's fonts and
    /// draws again.</summary>
    private void FollowFontChange()
    {
        _ui.InvalidateMeasurements();
        foreach (var popup in _popups)
            popup.Surface.Canvas.CopyFontsFrom(_render.MainCanvas);
        foreach (var dialog in _windows)
            dialog.Canvas.CopyFontsFrom(_render.MainCanvas);
        Invalidate();
    }

    private bool TryAddFont(FontFace font)
    {
        try
        {
            AddFont(font);
            return true;
        }
        catch (FreeTypeException e)
        {
            Console.WriteLine($"[ImmediateGuiAppHost] Could not load the font of {font.Family}: {e.Message}");
            return false;
        }
    }

    private void AddFont(FontFace font)
    {
        _fontRegistry.Register(font);
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
                Console.WriteLine($"[ImmediateGuiAppHost] Could not read fallback font {font.Path}: {e.Message}");
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
            Console.WriteLine($"[ImmediateGuiAppHost] Could not load fallback font {font.Path}: {e.Message}");
            return;
        }
        _ui.InvalidateMeasurements();
    }

    private void DrawFrame()
    {
        (_frame, _pending) = (_pending, _frame);
        _frame.Seal();
        _pending.BeginAfter(_frame);

        _fontRegistry.ReleaseUnused();
        _render.Fonts.AdvanceFrame();
        var now = Stopwatch.GetTimestamp();
        var dt = _animating ? (float)((now - _lastFrameTimestamp) / (double)Stopwatch.Frequency) : 0f;
        _lastFrameTimestamp = now;

        _ui.WindowBounds = WindowBounds;
        _drawing = true;
        _ui.BeginFrame(_frame, MathF.Min(dt, MaxFrameStep), Seconds(now), _render.MainCanvas.Width, _render.MainCanvas.Height);
        _updateUi(_ui);
        _ui.EndFrame();
        _drawing = false;
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
        PresentWindows();
        TakeScale();
        if (_ui.QuitRequested)
            _render.App.Quit();

        if (_ui.Cursor != _cursor)
        {
            _cursor = _ui.Cursor;
            _window.SetCursor(_cursor);
            foreach (var popup in _popups)
                popup.Surface.Window.SetCursor(_cursor);
            foreach (var dialog in _windows)
                dialog.Window.SetCursor(_cursor);
        }
        if (_ui.CompositionReset)
        {
            _window.ResetPreedit();
            foreach (var dialog in _windows)
                dialog.Window.ResetPreedit();
            _pending.Compose(PreeditText.Empty);
        }
        if (_ui.WantsTextInput != _textInputFocus)
        {
            _textInputFocus = _ui.WantsTextInput;
            _window.SetTextInputFocus(_textInputFocus);
            foreach (var dialog in _windows)
                dialog.Window.SetTextInputFocus(_textInputFocus);
            _imeCaret = null;
        }
        if (_textInputFocus && _ui.ImeCaret is { } caret)
            PlaceImeCaret(caret);
        _animating = _ui.UpdateRequested;
        _wakeAt = _ui.WakeAt;
        if (_frame.HasEvents || _ui.UpdateRequested)
            _window.RequestRedraw();
    }

    /// <summary>Puts the OS candidate window at <paramref name="caret"/>, in the window the caret is
    /// drawn in.</summary>
    private void PlaceImeCaret(UiRect caret)
    {
        var dialog = WindowAt(new PointF(caret.X, caret.Y));
        var window = _window;
        if (dialog != null)
        {
            window = dialog.Window;
            caret = caret with { X = caret.X - dialog.Rect.X, Y = caret.Y - dialog.Rect.Y };
        }
        var s = ScreenPerPoint;
        var x = (int)MathF.Floor(caret.X * s);
        var y = (int)MathF.Floor(caret.Y * s);
        var rect = (X: x, Y: y,
            Width: Math.Max(1, (int)MathF.Ceiling(caret.Right * s) - x),
            Height: Math.Max(1, (int)MathF.Ceiling(caret.Bottom * s) - y));
        if (rect == _imeCaret) return;
        _imeCaret = rect;
        window.SetPreeditCursorRect(rect.X, rect.Y, rect.Width, rect.Height);
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
        if (_ui.HasFoldersToOpen)
            OpenFolders();
        if (_ui.HasFilesToReveal)
            RevealFiles();
        if (_ui.HasFilesToOpen)
            OpenFiles();
        if (_ui.HasUrlsToOpen)
            OpenUrls();
        if (_ui.HasFontChanges)
            ApplyAskedFontChanges();
        if (_ui.HasFontListings)
            ListSystemFonts();
        if (_wakeAt <= Seconds(Stopwatch.GetTimestamp()))
        {
            _wakeAt = double.PositiveInfinity;
            _window.RequestRedraw();
        }
        PollPointer();
    }

    private void OpenFolders()
    {
        foreach (var folder in _ui.TakeFoldersToOpen())
            FileManagerWindow.Open(folder);
    }

    private void RevealFiles()
    {
        foreach (var file in _ui.TakeFilesToReveal())
            FileManagerWindow.Reveal(file);
    }

    private void OpenFiles()
    {
        foreach (var file in _ui.TakeFilesToOpen())
            DefaultApp.OpenFile(file);
    }

    private void OpenUrls()
    {
        foreach (var url in _ui.TakeUrlsToOpen())
            WebBrowser.Open(url);
    }

    private void ApplyAskedFontChanges()
    {
        foreach (var change in _ui.TakeFontChanges())
            ApplyFontChange(change);
    }

    private void ApplyFontChange(FontChange change)
    {
        if (change.Face != null)
            RegisterFont(change.Face);
        else
            UnregisterFont(change.Family);
    }

    /// <summary>Lists the installed fonts on a worker thread and hands them to every listing asked for.</summary>
    private void ListSystemFonts()
    {
        var listings = _ui.TakeFontListings();
        Task.Run(() =>
        {
            var families = ListFamiliesOrNone();
            foreach (var onListed in listings)
                _ui.Post(() => onListed(families));
        });
    }

    private IReadOnlyList<SystemFontFamily> ListFamiliesOrNone()
    {
        try
        {
            return _systemFonts.ListFamilies();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ImmediateGuiAppHost] Could not list the system fonts: {e.Message}");
            return [];
        }
    }

    private void PollPointer()
    {
        _window.GetCursorPosition(out var x, out var y);
        var inside = x >= 0 && y >= 0 && x <= _window.Width && y <= _window.Height;
        var ui = ToUiSpace(x, y);
        var point = inside || _pending.FollowsPointerOutside || OverPopup(ui) || WindowAt(ui) != null ? ui : UiInput.Offscreen;
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

    UiRect IPopupHost.Place(UiRect preferred, UiRect fallback)
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

    ICanvas? IPopupHost.Show(int id, UiRect rect, UiRect surface, bool passThrough)
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
            Console.WriteLine($"[ImmediateGuiAppHost] Popup windows are unavailable, drawing popups in the window: {e.Message}");
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

    WindowSurface? IPopupHost.ShowWindow(int id, string title, float width, float height, WindowBounds? openAt, UiRect? placeAt)
    {
        if (_dialogWindowsUnavailable) return null;

        var dialog = FindWindow(id) ?? OpenWindow(id, title, width, height, openAt);
        if (dialog == null) return null;

        if (placeAt != null)
            PlaceWindow(dialog.Window, placeAt.Value);
        dialog.Used = true;
        dialog.Rect = RectOf(dialog.Window);
        ResizeCanvas(dialog.Canvas, (int)dialog.Rect.Width, (int)dialog.Rect.Height, _render.MainCanvas.DpiScale);
        dialog.Canvas.BeginFrame();
        var closeRequested = dialog.CloseRequested;
        dialog.CloseRequested = false;
        return new WindowSurface(dialog.Canvas, dialog.Rect, BoundsOf(dialog.Window), closeRequested);
    }

    private DialogWindow? FindWindow(int id)
    {
        foreach (var dialog in _windows)
        {
            if (dialog.Id == id)
                return dialog;
        }
        return null;
    }

    private DialogWindow? WindowAt(PointF point)
    {
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i].Rect.Contains(point))
                return _windows[i];
        }
        return null;
    }

    /// <summary>Moves and sizes <paramref name="window"/> to <paramref name="rect"/>, in the main
    /// window's UI space, touching only what changed.</summary>
    private void PlaceWindow(IWindow window, UiRect rect)
    {
        var s = ScreenPerPoint;
        _window.GetPosition(out var originX, out var originY);
        var x = originX + (int)MathF.Round(rect.X * s);
        var y = originY + (int)MathF.Round(rect.Y * s);
        window.GetPosition(out var currentX, out var currentY);
        if (x != currentX || y != currentY)
            window.SetPosition(x, y);

        var dpi = _render.MainCanvas.DpiScale;
        var pixelsPerScreen = _window.DpiScale > 0f ? _window.DpiScale : 1f;
        var width = PopupPlacement.ScreenSize((int)MathF.Round(rect.Width), dpi, pixelsPerScreen);
        var height = PopupPlacement.ScreenSize((int)MathF.Round(rect.Height), dpi, pixelsPerScreen);
        if (width != window.Width || height != window.Height)
            window.SetSize(width, height);
    }

    private static WindowBounds BoundsOf(IWindow window)
    {
        window.GetPosition(out var x, out var y);
        return new WindowBounds(x, y, window.Width, window.Height, false);
    }

    /// <summary>Where <paramref name="window"/>'s client area is, in the main window's UI space.</summary>
    private UiRect RectOf(IWindow window)
    {
        var s = ScreenPerPoint;
        var dpi = _render.MainCanvas.DpiScale;
        _window.GetPosition(out var originX, out var originY);
        window.GetPosition(out var x, out var y);
        var width = MathF.Max(1f, MathF.Round(window.FramebufferWidth / dpi));
        var height = MathF.Max(1f, MathF.Round(window.FramebufferHeight / dpi));
        return new UiRect((x - originX) / s, (y - originY) / s, width, height);
    }

    /// <summary>A hidden window without a frame, owned by the main window, which takes no input until
    /// it closes: at <paramref name="openAt"/> kept on a monitor, or else <paramref name="width"/> by
    /// <paramref name="height"/> points centered on the main window and kept on its monitor.</summary>
    private DialogWindow? OpenWindow(int id, string title, float width, float height, WindowBounds? openAt)
    {
        var dpi = _render.MainCanvas.DpiScale;
        var pixelsPerScreen = _window.DpiScale > 0f ? _window.DpiScale : 1f;
        var screenWidth = PopupPlacement.ScreenSize((int)MathF.Ceiling(width), dpi, pixelsPerScreen);
        var screenHeight = PopupPlacement.ScreenSize((int)MathF.Ceiling(height), dpi, pixelsPerScreen);
        if (openAt != null)
        {
            screenWidth = openAt.Value.Width;
            screenHeight = openAt.Value.Height;
        }
        var window = CreateWindow(title, screenWidth, screenHeight);
        if (window == null) return null;

        if (openAt != null)
            PutBack(window, openAt.Value);
        else
            CenterOnMainWindow(window, screenWidth, screenHeight);
        var canvas = _render.CreateCanvas(window, (int)MathF.Ceiling(width), (int)MathF.Ceiling(height));
        var dialog = new DialogWindow(id, window, canvas, Invalidate);
        _render.WirePresentLoop(window, canvas);
        Subscribe(window);
        window.OnClose += dialog.HandleClose;
        window.OnMove += HandleWindowMove;
        window.OnResize += HandleWindowResize;
        window.OnFramebufferResize += HandleWindowResize;
        window.SetCursor(_cursor);
        window.SetTextInputFocus(_textInputFocus);
        window.SetOwner(_window);
        _window.SetInputEnabled(false);
        _windows.Add(dialog);
        return dialog;
    }

    private IWindow? CreateWindow(string title, int width, int height)
    {
        try
        {
            return _render.App.CreateWindow(new WindowOptions { WidthPoints = width, HeightPoints = height, Title = title, IsUndecorated = true });
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ImmediateGuiAppHost] Dialog windows are unavailable, drawing dialogs in the window: {e.Message}");
            _dialogWindowsUnavailable = true;
            return null;
        }
    }

    /// <summary>Centers <paramref name="window"/> on the main window, shrunk to fit and kept on the
    /// monitor.</summary>
    private void CenterOnMainWindow(IWindow window, int width, int height)
    {
        var monitor = MonitorUnderMainWindow();
        if (monitor != null)
        {
            width = Math.Min(width, monitor.Value.Width);
            height = Math.Min(height, monitor.Value.Height);
            window.SetSize(width, height);
        }
        var (centerX, centerY) = MainWindowCenter();
        var (x, y) = WindowPlacement.Compute(_render.App.Monitors, width, height, centerX - width / 2, centerY - height / 2);
        window.SetPosition(x, y);
    }

    /// <summary>Puts <paramref name="window"/> where <paramref name="bounds"/> left it, pulled back onto
    /// a monitor if it is no longer on one.</summary>
    private void PutBack(IWindow window, WindowBounds bounds)
    {
        var (x, y) = WindowPlacement.Compute(_render.App.Monitors, bounds.Width, bounds.Height, bounds.X, bounds.Y);
        window.SetPosition(x, y);
    }

    private MonitorWorkArea? MonitorUnderMainWindow()
    {
        var monitors = _render.App.Monitors;
        if (monitors.Count == 0) return null;

        var (x, y) = MainWindowCenter();
        foreach (var monitor in monitors)
        {
            if (Contains(monitor, x, y))
                return monitor;
        }
        return monitors[0];
    }

    private (int X, int Y) MainWindowCenter()
    {
        _window.GetPosition(out var x, out var y);
        return (x + _window.Width / 2, y + _window.Height / 2);
    }

    private static bool Contains(MonitorWorkArea monitor, int x, int y) =>
        x >= monitor.X && x < monitor.X + monitor.Width && y >= monitor.Y && y < monitor.Y + monitor.Height;

    private void PresentWindows()
    {
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            var dialog = _windows[i];
            if (dialog.Used) continue;
            _windows.RemoveAt(i);
            CloseWindow(dialog);
        }

        foreach (var dialog in _windows)
        {
            _render.RenderWindowNow(dialog.Window);
            if (!dialog.Shown)
            {
                dialog.Window.Show();
                dialog.Window.Focus();
                dialog.Shown = true;
            }
            dialog.Used = false;
        }
    }

    /// <summary>Destroys <paramref name="dialog"/>'s window. When it was the last, the main window
    /// takes input again first, so the OS can activate it.</summary>
    private void CloseWindow(DialogWindow dialog)
    {
        var window = dialog.Window;
        Unsubscribe(window);
        window.OnClose -= dialog.HandleClose;
        window.OnMove -= HandleWindowMove;
        window.OnResize -= HandleWindowResize;
        window.OnFramebufferResize -= HandleWindowResize;
        var last = _windows.Count == 0;
        if (last)
            _window.SetInputEnabled(true);
        _render.DisposeWindow(window, dialog.Canvas);
        if (last)
            _window.Focus();
    }

    private void CloseWindows()
    {
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            var dialog = _windows[i];
            _windows.RemoveAt(i);
            CloseWindow(dialog);
        }
    }

    void IPopupHost.HideAll()
    {
        ReleasePopups();
    }

    private void ReleasePopups()
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
        window.OnFileDrop += HandleFileDrop;
    }

    private void Unsubscribe(IWindow window)
    {
        window.OnKey -= HandleKey;
        window.OnText -= HandleText;
        window.OnPreedit -= HandlePreedit;
        window.OnMouseButton -= HandleMouseButton;
        window.OnScroll -= HandleScroll;
        window.OnFileDrop -= HandleFileDrop;
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

    private void HandleFileDrop(IReadOnlyList<string> paths)
    {
        PollPointer();
        _pending.DropFiles(paths);
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

    private void HandleWindowMove(int x, int y)
    {
        _pending.DismissPopups();
        _window.RequestRedraw();
    }

    /// <summary>Draws the frame now, and with it the dialog window at its new size, so a live resize
    /// does not show stretched content. A resize asked for by the frame being drawn only asks for
    /// another.</summary>
    private void HandleWindowResize(int width, int height)
    {
        if (_drawing)
        {
            _window.RequestRedraw();
            return;
        }
        _render.RenderWindowNow(_window);
    }

    /// <summary>Hands focus on to the open dialog window when the main window gets it.</summary>
    private void HandleFocusChanged(bool focused)
    {
        if (!focused) return;
        if (_windows.Count == 0) return;
        _windows[^1].Window.Focus();
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

    /// <summary>Draws at the scale the frame asked for from the next frame on, when it changed.</summary>
    private void TakeScale()
    {
        if (_ui.Scale <= 0f)
            return;
        if (_ui.Scale == _scale)
            return;
        _scale = _ui.Scale;
        SyncScale();
        _window.RequestRedraw();
    }

    /// <summary>Sizes the main canvas to the window at the monitor's scale times the app's own.</summary>
    private void SyncScale()
    {
        var monitor = _window.ContentScale > 0f ? _window.ContentScale : 1f;
        var scale = monitor * _scale;
        var canvas = _render.MainCanvas;
        canvas.UpdateDpiScale(scale);
        canvas.Resize(
            (int)MathF.Max(1f, MathF.Round(Math.Max(1, _window.FramebufferWidth) / scale)),
            (int)MathF.Max(1f, MathF.Round(Math.Max(1, _window.FramebufferHeight) / scale)));
    }

    internal void Close()
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
        _window.OnFocusChanged -= HandleFocusChanged;
        ReleasePopups();
        CloseWindows();
        _pool.Dispose();
        _render.Dispose();
    }
}

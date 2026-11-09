using System.Collections.Concurrent;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using FreeTypeSharp;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Immediate;

/// <summary>The window, render loop and UI thread a <see cref="ImmediateGuiApp{TUiState}"/> runs in. While it runs,
/// <c>await</c> on the UI thread continues before the next frame.</summary>
public sealed class ImmediateGuiAppHost<TUiState> : IPopupHost, IAutomatedHost, IInspectedHost where TUiState : class, IUiState
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

    private sealed class TickDispatcher(ImmediateGuiAppHost<TUiState> host) : IUiDispatcher
    {
        public void Post(Action action)
        {
            host._ticked.Enqueue(action);
            host._render.App.Wake();
        }
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
    private readonly ImmediateGuiApp<TUiState> _app;
    private readonly Ui<TUiState> _ui;
    private readonly IClipboard _clipboard;
    private readonly PlatformClipboard? _platformClipboard;
    private readonly IFilePicker _filePicker;
    private readonly ImmediateAutomation? _automation;
    private readonly ImmediateInspection? _inspection;
    private readonly int _uiThread = Environment.CurrentManagedThreadId;
    private readonly ISystemFonts _systemFonts = NativeSystemFonts.Create();
    private readonly FontRegistry _fontRegistry;
    private readonly List<Popup> _popups = new();
    private readonly List<DialogWindow> _windows = new();
    private readonly List<UiRect> _dragAreas = new();
    private readonly ConcurrentQueue<Action> _ticked = new();
    private readonly List<RectI> _dragAreasOnWindow = new();
    private TitleBarContent? _titleBar;
    private readonly PopupPool _pool;
    private UiInput _pending = new();
    private UiInput _frame = new();
    private readonly StopwatchTimestamp _startTime = StopwatchTimestamp.Now();
    private StopwatchTimestamp _lastFrameTime = StopwatchTimestamp.Now();
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

    /// <summary>Opens the window of <paramref name="app"/>, which <see cref="Run"/> then runs in it; with
    /// <paramref name="automation"/>, in automation mode, driven through <see cref="Automation"/>. Call it on the
    /// thread that runs it.</summary>
    public ImmediateGuiAppHost(ImmediateGuiApp<TUiState> app, StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto,
        AutomationOptions? automation = null)
    {
        _app = app;
        _render = CreateRenderBackend(automation == null ? config : config with { Headless = true }, backend, automation != null);
        try
        {
            _fontRegistry = new FontRegistry(_render.Fonts, _render.MainCanvas, LoadFont);
            RegisterDefaultBold();
            _window = _render.App.MainWindow;
            _pool = new PopupPool(_render, PopupPoolSize);
            if (automation != null)
            {
                _automation = new ImmediateAutomation(automation, this, Environment.CurrentManagedThreadId, _render.App.Wake);
                _clipboard = _automation.Clipboard;
            }
            else
            {
                _platformClipboard = PlatformClipboard.Create(_render.App);
                _clipboard = _platformClipboard;
            }
            _ui = new Ui<TUiState>(app.UiState, _render.MainCanvas, _clipboard, this);
            _ui.Wake = Invalidate;
            if (OperatingSystem.IsMacOS())
            {
                ISystemMenuBar menuBar = _automation == null ? new MacMenuBar() : _automation.MenuBar;
                _ui.SystemMenus = new SystemMenus(menuBar, Invalidate);
            }
            _filePicker = _automation == null ? NativeFilePicker.Create(_ui.Post, _window) : _automation.FilePicker;
            Dispatcher = new TickDispatcher(this);
            if (_automation == null)
                _inspection = new ImmediateInspection(this);
            _clearColor = _ui.WindowBackground;
            WireMainFrame();
            _normalBounds = (config.WindowX ?? 0, config.WindowY ?? 0, config.WindowWidth, config.WindowHeight);
            _maximizeOnShow = config.StartHidden && config.StartMaximized && !config.IsFixedSize;

            Subscribe(_window);
            _window.OnResize += HandleResize;
            _window.OnFramebufferResize += HandleResize;
            _window.OnContentScaleChanged += HandleContentScaleChanged;
            _window.OnMove += HandleMove;
            _window.OnMinimizedChanged += HandleMinimizedChanged;
            _pending.SetMinimized(_window.IsMinimized);
            if (_automation == null)
            {
                _window.OnFocusChanged += HandleFocusChanged;
                _render.App.OnForegroundChanged += HandleForegroundChanged;
                _window.OnNonClientPress += HandleNonClientPress;
            }
            _render.App.OnTick += Tick;
            _window.OnClose += HandleCloseAsked;
            HotReload.Applied += Invalidate;

            SyncScale();
            var fallbackFonts = LoadSystemFallbackFonts();
            _automation?.BackgroundWork.Track("system fallback fonts", fallbackFonts);
        }
        catch
        {
            _disposed = true;
            HotReload.Applied -= Invalidate;
            _platformClipboard?.Dispose();
            _render.Dispose();
            throw;
        }
    }

    private static IGuiRenderBackend CreateRenderBackend(StartupConfig config, RenderBackendKind kind, bool offscreen) => kind switch
    {
        RenderBackendKind.OpenGl => new GlRenderBackend(config, offscreen),
        RenderBackendKind.Metal => new MetalRenderBackend(config, offscreen),
        _ => IGuiRenderBackend.CreateForPlatform(config, offscreen),
    };

    /// <summary>What drives the app in automation mode; null when it runs as usual.</summary>
    public ImmediateAutomation? Automation => _automation;

    /// <summary>Reads the app as it runs as usual, for an inspection-only debug server; null in automation mode, where
    /// <see cref="Automation"/> reads it.</summary>
    public ImmediateInspection? Inspection => _inspection;

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

    /// <summary>Runs the app in its window until the window closes.</summary>
    public void Run()
    {
        try
        {
            using var context = InstallSynchronizationContext();
            _app.Startup(this);
            RunUntilClosed();
            _automation?.Shutdown();
            _app.Shutdown(this);
        }
        finally
        {
            Close();
        }
    }

    private void RunUntilClosed()
    {
        TrackBounds();
        if (_automation == null)
            _render.RenderWindowNow(_window);
        _render.App.Run();
    }

    /// <summary>The window's bounds: where it sits and how big it is when not maximized, and once it
    /// has closed, where it was left — save them and reopen with <see cref="WindowBounds.ApplyTo"/>.</summary>
    /// <summary>Runs work on the UI thread between frames without drawing one, for an engine pumped from
    /// outside the UI, such as a web view's; it asks for a frame itself when it has something to show.
    /// Safe from any thread.</summary>
    public IUiDispatcher Dispatcher { get; }

    /// <summary>Writes the main window's next frame to <paramref name="path"/> as a PNG, where the render
    /// backend can read frames back; <paramref name="onComplete"/> runs once it has tried.</summary>
    public void RequestScreenshot(string path, Action? onComplete = null)
    {
        _render.RequestScreenshot(path, onComplete);
        Invalidate();
    }

    public WindowBounds WindowBounds => new(_normalBounds.X, _normalBounds.Y, _normalBounds.Width, _normalBounds.Height, _window.IsMaximized);

    /// <summary>Whether the window, opened hidden, is to be maximized as it is first shown.</summary>
    private bool _maximizeOnShow;

    private void HandleCloseAsked()
    {
        if (!_app.CloseRequested(this))
            _window.CancelClose();
        else
            _automation?.Shutdown();
    }

    /// <summary>Takes the window off the screen. The app keeps running, but no frame is drawn until
    /// <see cref="ShowWindow"/>, so nothing that waits for a frame runs, an <c>await</c> on the UI thread included;
    /// <see cref="Dispatcher"/> still does. Call it on the UI thread.</summary>
    public void HideWindow()
    {
        if (_automation != null)
        {
            _automation.Record(new AutomationEffect.WindowHidden());
            return;
        }
        _window.Hide();
        _render.MainCanvas.AbortFrame();
    }

    /// <summary>Puts a hidden window back and draws it. Call it on the UI thread.</summary>
    public void ShowWindow()
    {
        if (_automation != null)
        {
            _automation.Record(new AutomationEffect.WindowShown());
            Invalidate();
            return;
        }
        _window.Show();
        if (_maximizeOnShow)
            _window.Maximize();
        _maximizeOnShow = false;
        Invalidate();
    }

    /// <summary>Brings the window to the front, restoring it if minimized, where the platform allows; on Windows that
    /// is for the app the user is working with, which can lend the right with <see cref="ForegroundRights.LendTo"/>.
    /// Call it on the UI thread.</summary>
    public void FocusWindow()
    {
        if (_automation != null)
        {
            _automation.Record(new AutomationEffect.BroughtToFront());
            return;
        }
        if (_window.IsMinimized)
            _window.Restore();
        _window.Focus();
    }

    /// <summary>Closes the window as its close button does, without asking
    /// <see cref="ImmediateGuiApp{TUiState}.OnCloseRequested"/>, so the app shuts down the usual way. Works while hidden.
    /// Call it on the UI thread.</summary>
    public void CloseImmediately()
    {
        _window.CloseImmediately();
        _automation?.Shutdown();
        _render.App.Wake();
    }

    /// <summary>Schedules an update after the app's state changed outside a frame. Safe from any thread,
    /// and does nothing once the window has closed.</summary>
    public void Invalidate()
    {
        if (_disposed) return;
        if (_automation != null)
        {
            _automation.NoteFrameRequested();
            return;
        }
        _window.RequestRedraw();
        _render.App.Wake();
    }

    /// <summary>Draws again: on the loop's next turn, or in automation mode, where hidden windows are never drawn
    /// by the loop, as a frame the driver sees pending.</summary>
    private void RequestRedraw()
    {
        if (_automation != null)
            _automation.NoteFrameRequested();
        else
            _window.RequestRedraw();
    }

    /// <summary>Makes <c>await</c> on this thread continue on the UI thread before the next frame,
    /// until the returned scope is disposed.</summary>
    private UiSynchronizationContext.Scope InstallSynchronizationContext()
    {
        var context = new UiSynchronizationContext(_ui.Post);
        return context.Install();
    }

    /// <summary>Shows <paramref name="icon"/> on the window where the platform draws one, such as the
    /// title bar and taskbar on Windows, and in the UI's <see cref="Ui.AppIcon"/>; macOS
    /// takes the app's icon from its bundle instead.</summary>
    public void SetIcon(WindowIcon icon)
    {
        if (_disposed) return;
        _window.SetIcon(icon.Images);
        _ui.WindowIcon = icon;
        Invalidate();
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

    private Task LoadSystemFallbackFonts() => Task.Run(() =>
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
        var (dt, seconds) = NextFrameTime();

        _ui.WindowBounds = WindowBounds;
        ShowCaptionButtonsToUi();
        _drawing = true;
        _ui.BeginFrame(_frame, dt, seconds, _render.MainCanvas.Width, _render.MainCanvas.Height);
        _app.Update(_ui);
        _ui.EndFrame();
        _inspection?.FrameEnded();
        _drawing = false;
        if (_ui.WindowBackground != _clearColor)
        {
            _clearColor = _ui.WindowBackground;
            WireMainFrame();
        }
        ShowTitleBar();
        var titleBarColor = _ui.TitleBarBackground ?? _clearColor;
        var dark = UiColor.IsDark(titleBarColor);
        if (dark != _titleBarDark)
        {
            _titleBarDark = dark;
            _window.SetTitleBarTheme(dark);
        }
        PresentPopups();
        PresentWindows();
        TakeScale();
        ApplyRequestedWindowState();
        BringToFrontIfRequested();
        if (_ui.QuitRequested)
        {
            _render.App.Quit();
            _automation?.Shutdown();
        }

        ApplyPointerLock();
        if (_ui.Cursor != _cursor)
        {
            _cursor = _ui.Cursor;
            if (_automation == null)
                ShowCursor();
        }
        if (_ui.CompositionReset)
        {
            if (_automation == null)
            {
                _window.ResetPreedit();
                foreach (var dialog in _windows)
                    dialog.Window.ResetPreedit();
            }
            _pending.Compose(PreeditText.Empty);
        }
        if (_ui.WantsTextInput != _textInputFocus)
        {
            _textInputFocus = _ui.WantsTextInput;
            if (_automation == null)
            {
                _window.SetTextInputFocus(_textInputFocus);
                foreach (var dialog in _windows)
                    dialog.Window.SetTextInputFocus(_textInputFocus);
            }
            _imeCaret = null;
        }
        if (_automation == null && _textInputFocus && _ui.ImeCaret is { } caret)
            PlaceImeCaret(caret);
        _animating = _ui.UpdateRequested;
        _wakeAt = _ui.WakeAt;
        if (_automation == null && (_frame.HasEvents || _ui.UpdateRequested))
            _window.RequestRedraw();
    }

    /// <summary>How long since the frame before and the seconds since the app started: by the wall clock, or in
    /// automation mode by its virtual clock.</summary>
    private (float DeltaTime, double Seconds) NextFrameTime()
    {
        if (_automation != null)
            return (_automation.FrameStep, _automation.Now);
        var now = StopwatchTimestamp.Now();
        var dt = _animating ? (float)now.SecondsSince(_lastFrameTime) : 0f;
        _lastFrameTime = now;
        return (MathF.Min(dt, MaxFrameStep), now.SecondsSince(_startTime));
    }

    private void ShowCursor()
    {
        _window.SetCursor(_cursor);
        foreach (var popup in _popups)
            popup.Surface.Window.SetCursor(_cursor);
        foreach (var dialog in _windows)
            dialog.Window.SetCursor(_cursor);
    }

    /// <summary>Tells the frame whether it can draw in the window's title bar, where the caption
    /// buttons are there, and how much of the window's top lies off the screen.</summary>
    private void ShowCaptionButtonsToUi()
    {
        _ui.CanDrawTitleBar = _window.CanExtendIntoTitleBar;
        _ui.CaptionButtons = CaptionButtonsInUiSpace();
        _ui.HiddenTop = ToUiSpace(0, _window.HiddenContentTop).Y;
    }

    private UiRect? CaptionButtonsInUiSpace()
    {
        var buttons = _window.CaptionButtons;
        if (buttons.Width <= 0)
            return null;
        var topLeft = ToUiSpace(buttons.X, buttons.Y);
        var bottomRight = ToUiSpace(buttons.Right, buttons.Bottom);
        return new UiRect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }

    /// <summary>Has the window's content reach under its title bar while the frame records a title bar,
    /// shown as recorded, and gives the title bar its strip back once it does not. Draws again when that
    /// moved the caption buttons.</summary>
    private void ShowTitleBar()
    {
        if (!_window.CanExtendIntoTitleBar)
            return;
        var background = _ui.TitleBarBackground;
        ListDragAreasOnWindow();
        if (IsTitleBarShown(background))
            return;
        _titleBar = CreateTitleBarContent(background);
        var buttons = _window.CaptionButtons;
        _window.ExtendIntoTitleBar(_titleBar);
        if (_window.CaptionButtons != buttons)
            RequestRedraw();
    }

    private void ListDragAreasOnWindow()
    {
        _ui.ListTitleBarDragAreas(_dragAreas);
        _dragAreasOnWindow.Clear();
        foreach (var area in _dragAreas)
            _dragAreasOnWindow.Add(ToWindowRect(area));
    }

    private RectI ToWindowRect(UiRect rect)
    {
        var s = ScreenPerPoint;
        var left = (int)MathF.Round(rect.X * s);
        var top = (int)MathF.Round(rect.Y * s);
        var right = (int)MathF.Round(rect.Right * s);
        var bottom = (int)MathF.Round(rect.Bottom * s);
        return new RectI(left, top, right - left, bottom - top);
    }

    /// <summary>Whether the window already shows the title bar this frame recorded. Allocates nothing.</summary>
    private bool IsTitleBarShown(uint? background)
    {
        if (_titleBar == null)
            return background == null;
        if (background != _titleBar.Background)
            return false;
        return AreDragAreasShown(_titleBar.DragAreas);
    }

    private bool AreDragAreasShown(IReadOnlyList<RectI> shown)
    {
        if (shown.Count != _dragAreasOnWindow.Count)
            return false;
        for (var i = 0; i < shown.Count; i++)
        {
            if (shown[i] != _dragAreasOnWindow[i])
                return false;
        }
        return true;
    }

    private TitleBarContent? CreateTitleBarContent(uint? background)
    {
        if (background == null)
            return null;
        return new TitleBarContent(background.Value, _dragAreasOnWindow.ToArray());
    }

    private void ApplyRequestedWindowState()
    {
        if (_automation != null)
        {
            var requested = _ui.RequestedWindowState;
            if (requested != null)
                _automation.Record(new AutomationEffect.WindowStateRequested(requested.Value));
            return;
        }
        switch (_ui.RequestedWindowState)
        {
            case WindowState.Normal:
                _window.Restore();
                break;
            case WindowState.Minimized:
                _window.Minimize();
                break;
            case WindowState.Maximized:
                _window.Maximize();
                break;
        }
    }

    private void BringToFrontIfRequested()
    {
        if (!_ui.IsFrontRequested)
            return;
        if (_automation != null)
        {
            _automation.Record(new AutomationEffect.BroughtToFront());
            return;
        }
        if (_window.IsMinimized)
            _window.Restore();
        _window.Focus();
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

    private void Tick(TickScope tick)
    {
        while (!_disposed && _ticked.TryDequeue(out var work))
            work();
        if (_automation != null)
        {
            _automation.RunCalls();
            return;
        }
        if (_backgroundPending)
        {
            _backgroundPending = false;
            if (!_render.App.IsForeground)
            {
                _pending.ReleaseAll();
                _pending.DismissPopups();
                RequestRedraw();
            }
        }
        if (_ui.HasDialogs && _ui.ShowDialogs(_filePicker))
        {
            _pending.DismissPopups();
            RequestRedraw();
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
        if (_wakeAt <= tick.Now.SecondsSince(_startTime))
        {
            _wakeAt = double.PositiveInfinity;
            RequestRedraw();
        }
        else if (double.IsFinite(_wakeAt))
        {
            tick.WakeAt(_startTime.After(_wakeAt));
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
        var listing = Task.Run(() =>
        {
            var families = ListFamiliesOrNone();
            foreach (var onListed in listings)
                _ui.Post(() => onListed(families));
        });
        _automation?.BackgroundWork.Track("system font listing", listing);
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

    /// <summary>Locks the pointer while the frame asked for it and the app is in front, and frees it otherwise.</summary>
    private void ApplyPointerLock()
    {
        var foreground = _automation == null ? _render.App.IsForeground : _pending.IsForeground;
        var locked = _ui.PointerLockRequested && foreground;
        if (locked == _pending.PointerLocked)
            return;
        if (_automation == null)
            _window.SetPointerLocked(locked);
        else
            _automation.Record(new AutomationEffect.PointerLockChanged(locked));
        _pending.LockPointer(locked);
        RequestRedraw();
    }

    private void PollPointer()
    {
        _window.GetCursorPosition(out var x, out var y);
        var point = _pending.PointerLocked || _pending.FollowsPointerOutside || IsPointerOverOwnWindow() ? ToUiSpace(x, y) : UiInput.Offscreen;
        if (point == _pending.Pointer) return;
        _pending.MovePointer(point);
        RequestRedraw();
    }

    /// <summary>Whether the topmost window under the cursor is one of this app's, so the pointer is
    /// not over another app's window that covers it.</summary>
    private bool IsPointerOverOwnWindow()
    {
        if (_window.IsPointerOver)
            return true;
        foreach (var popup in _popups)
        {
            if (!popup.PassThrough && popup.Surface.Window.IsPointerOver)
                return true;
        }
        foreach (var dialog in _windows)
        {
            if (dialog.Window.IsPointerOver)
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
        if (_automation == null)
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
        if (_automation == null)
        {
            window.SetCursor(_cursor);
            window.SetTextInputFocus(_textInputFocus);
            _window.SetInputEnabled(false);
        }
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
                if (_automation == null)
                {
                    dialog.Window.SetOwner(_window);
                    dialog.Window.Show();
                    dialog.Window.Focus();
                }
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
        var activateMain = _windows.Count == 0 && _automation == null;
        if (activateMain)
            _window.SetInputEnabled(true);
        _render.DisposeWindow(window, dialog.Canvas);
        if (activateMain)
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
                if (_automation == null)
                    popup.Surface.Window.Show();
                popup.Shown = true;
            }
            popup.Used = false;
        }
    }

    /// <summary>Hides <paramref name="popup"/>, passing the focus it had to the window it was shown over.
    /// That is the open dialog window if there is one: focusing the main window instead would raise it
    /// over the dialog on macOS, where a window has no owner to keep the dialog above it.</summary>
    private void Release(Popup popup)
    {
        var window = popup.Surface.Window;
        var hadFocus = _automation == null && window.IsFocused;
        Unsubscribe(window);
        _pool.Release(popup.Surface);
        if (!hadFocus) return;
        if (_windows.Count > 0)
            _windows[^1].Window.Focus();
        else
            _window.Focus();
    }

    /// <summary>Takes <paramref name="window"/>'s input from the OS, except in automation mode, where input comes
    /// only from <see cref="Automation"/>.</summary>
    private void Subscribe(IWindow window)
    {
        if (_automation != null)
            return;
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
        RequestRedraw();
    }

    private void HandleText(uint codePoint)
    {
        _pending.Character(codePoint);
        RequestRedraw();
    }

    private void HandlePreedit(PreeditText preedit)
    {
        _pending.Compose(preedit);
        RequestRedraw();
    }

    private void HandleMouseButton(int button, InputAction action, KeyModifiers modifiers)
    {
        PollPointer();
        _pending.Button(button, action, modifiers);
        RequestRedraw();
    }

    private void HandleScroll(double x, double y)
    {
        // Read while still inside the platform's callback: the event behind it is the current one only until it returns.
        var (gesture, momentum) = MacScrollPhase.Read();
        _pending.Scroll(x, y, gesture, momentum);
        RequestRedraw();
    }

    private void HandleFileDrop(IReadOnlyList<string> paths)
    {
        PollPointer();
        _pending.DropFiles(paths);
        RequestRedraw();
    }

    private void HandleForegroundChanged(bool foreground)
    {
        _pending.SetForeground(foreground);
        _backgroundPending = !foreground;
        RequestRedraw();
    }

    private void HandleMinimizedChanged(bool minimized)
    {
        _pending.SetMinimized(minimized);
        RequestRedraw();
    }

    private void HandleNonClientPress()
    {
        _pending.DismissPopups();
        RequestRedraw();
    }

    private void HandleWindowMove(int x, int y)
    {
        _pending.DismissPopups();
        RequestRedraw();
    }

    /// <summary>Draws the frame now, and with it the dialog window at its new size, so a live resize
    /// does not show stretched content. A resize asked for by the frame being drawn only asks for
    /// another.</summary>
    private void HandleWindowResize(int width, int height)
    {
        if (_drawing || _automation != null)
        {
            RequestRedraw();
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
        RequestRedraw();
    }

    private void TrackBounds()
    {
        if (_window.IsMaximized || _window.Width <= 0 || _window.Height <= 0) return;
        _window.GetPosition(out var x, out var y);
        _normalBounds = (x, y, _window.Width, _window.Height);
    }

    /// <summary>Draws the frame now at the window's new size, so a live resize does not show stretched content. A resize
    /// that comes while a frame is being drawn, as when the frame shows the window maximized and Windows sizes it at once,
    /// only asks for another: drawing one inside it would start the frame's UI over under the frame still being drawn.</summary>
    private void HandleResize(int width, int height)
    {
        TrackBounds();
        SyncScale();
        if (_drawing)
        {
            RequestRedraw();
            return;
        }
        if (_automation == null)
            _render.RenderWindowNow(_window);
    }

    private void HandleContentScaleChanged(float scale)
    {
        SyncScale();
        RequestRedraw();
    }

    /// <summary>Draws at the scale the frame asked for from the next frame on, when it changed.</summary>
    private void TakeScale()
    {
        if (!AppZoom.Changes(_ui.Scale, _scale))
            return;
        _scale = _ui.Scale;
        SyncScale();
        RequestRedraw();
    }

    /// <summary>Sizes the main canvas to the window at the monitor's scale times the app's own.</summary>
    private void SyncScale()
    {
        var zoomed = AppZoom.Canvas(_window.FramebufferWidth, _window.FramebufferHeight, _window.ContentScale, _scale);
        var canvas = _render.MainCanvas;
        canvas.UpdateDpiScale(zoomed.DpiScale);
        canvas.Resize(zoomed.Width, zoomed.Height);
    }

    private void Close()
    {
        _disposed = true;
        _automation?.Shutdown();
        _inspection?.Shutdown();
        _ui.Wake = null;
        HotReload.Applied -= Invalidate;
        if (_automation == null)
        {
            _window.OnNonClientPress -= HandleNonClientPress;
            _render.App.OnForegroundChanged -= HandleForegroundChanged;
            _window.OnFocusChanged -= HandleFocusChanged;
        }
        _window.OnClose -= HandleCloseAsked;
        _render.App.OnTick -= Tick;
        Unsubscribe(_window);
        _window.OnResize -= HandleResize;
        _window.OnFramebufferResize -= HandleResize;
        _window.OnContentScaleChanged -= HandleContentScaleChanged;
        _window.OnMove -= HandleMove;
        _window.OnMinimizedChanged -= HandleMinimizedChanged;
        ReleasePopups();
        CloseWindows();
        _pool.Dispose();
        _platformClipboard?.Dispose();
        _render.Dispose();
    }

    void IInspectedHost.Post(Action work) => Dispatcher.Post(work);
    bool IInspectedHost.OnUiThread => Environment.CurrentManagedThreadId == _uiThread;
    void IInspectedHost.Invalidate() => Invalidate();
    Ui IInspectedHost.Ui => _ui;
    void IInspectedHost.RequestScreenshot(string path, Action done) => RequestScreenshot(path, done);

    UiInput IAutomatedHost.Input => _pending;
    MouseCursor IAutomatedHost.Cursor => _cursor;
    Ui IAutomatedHost.Ui => _ui;

    void IAutomatedHost.RunFrame()
    {
        _render.RenderWindowNow(_window);
        AnswerAutomatedServices();
    }

    void IAutomatedHost.DiscardPendingInput() => _pending.BeginAfter(_frame);

    void IAutomatedHost.RunPostedWork()
    {
        while (_ticked.TryDequeue(out var work))
            work();
        _ui.RunPosted();
    }

    void IAutomatedHost.Close() => CloseImmediately();

    SurfaceImage IAutomatedHost.Capture(SemanticSnapshot snapshot, SemanticSurface surface)
    {
        var window = WindowDrawingOn(snapshot.CanvasOf(surface));
        return _render.ReadOffscreen(window) ?? throw new InvalidOperationException($"{surface} has drawn nothing yet.");
    }

    private IWindow WindowDrawingOn(ICanvas canvas)
    {
        if (ReferenceEquals(canvas, _render.MainCanvas))
            return _window;
        foreach (var popup in _popups)
        {
            if (ReferenceEquals(popup.Surface.Canvas, canvas))
                return popup.Surface.Window;
        }
        foreach (var dialog in _windows)
        {
            if (ReferenceEquals(dialog.Canvas, canvas))
                return dialog.Window;
        }
        throw new InvalidOperationException("The window that surface drew on has closed since.");
    }

    /// <summary>Answers what the frame just drawn asked of the OS, in automation mode: file pickers from the
    /// queued answers, opening and revealing recorded, fonts as usual.</summary>
    private void AnswerAutomatedServices()
    {
        if (_automation == null)
            return;
        if (_ui.HasDialogs && _ui.ShowDialogs(_automation.FilePicker))
            _pending.DismissPopups();
        foreach (var folder in _ui.TakeFoldersToOpen())
            _automation.Record(new AutomationEffect.FolderOpened(folder));
        foreach (var file in _ui.TakeFilesToReveal())
            _automation.Record(new AutomationEffect.FileRevealed(file));
        foreach (var file in _ui.TakeFilesToOpen())
            _automation.Record(new AutomationEffect.FileOpened(file));
        foreach (var url in _ui.TakeUrlsToOpen())
            _automation.Record(new AutomationEffect.UrlOpened(url));
        if (_ui.HasFontChanges)
            ApplyAskedFontChanges();
        if (_ui.HasFontListings)
            ListSystemFonts();
    }
}

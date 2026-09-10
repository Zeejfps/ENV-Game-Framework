using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Automation;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Desktop;

public sealed class GuiApp : IDisposable
{
    private readonly IWindowedApp _app;
    private readonly RenderedCanvasBase _mainCanvas;
    private readonly FreeTypeFontBackend _fontBackend;
    private readonly IGuiRenderBackend _renderBackend;
    private readonly DesktopInputSystem _mainInput;
    private readonly PointerOwnershipArbiter _pointerArbiter;
    private readonly ImeCoordinator _imeCoordinator;
    private readonly GuiWindowHost _mainHost;
    private readonly QueuedUiDispatcher _dispatcher;
    private readonly FrameTicker _frameTicker;
    private long _lastAnimationTimestamp;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly PopupWindowFactory _popupFactory;
    private readonly SecondaryWindowFactory _secondaryWindows;
    private readonly IWindowChrome _windowChrome;
    private readonly IUiScale _uiScale;
    private readonly Context _context;
    private readonly Func<Context, View> _contentFactory;
    private readonly Action<Type[]?>? _hotReloadHandler;
    private GuiMcpServer? _mcpServer;

    public Context Context => _context;
    
    private GuiApp(
        IWindowedApp app,
        RenderedCanvasBase mainCanvas,
        FreeTypeFontBackend fontBackend,
        IGuiRenderBackend renderBackend,
        FontHandle defaultFont,
        Context context,
        Func<Context, View> contentFactory,
        Action<Context> registerBackendServices,
        Action? renderHook,
        Action<Context>? startup,
        int? mcpServerPort)
    {
        _app = app;
        _mainCanvas = mainCanvas;
        _fontBackend = fontBackend;
        _renderBackend = renderBackend;
        _context = context;
        _contentFactory = contentFactory;

        // One arbiter shared by every window's input system decides pointer ownership.
        // The main window is the base (non-modal) layer; popups and secondary windows register
        // themselves, and each re-registers on focus so arbiter order tracks on-screen stacking.
        var pointerArbiter = new PointerOwnershipArbiter();
        _pointerArbiter = pointerArbiter;
        // One coordinator decides which window the OS IME composes against. A field can't: a menu's
        // search box lives in a popup that never takes OS keyboard focus, so the window that composes
        // for it is the host window, not its own.
        _imeCoordinator = new ImeCoordinator(pointerArbiter);
        _uiScale = context.Get<IUiScale>() ?? FixedUiScale.One;
        _mainInput = new DesktopInputSystem(app.MainWindow, _uiScale, pointerArbiter, app, _imeCoordinator);
        pointerArbiter.Register(_mainInput, isModal: false);
        _imeCoordinator.Register(_mainInput);
        _mainHost = new GuiWindowHost(app.MainWindow, mainCanvas, _mainInput, context, _uiScale, sizeRootToWindow: true);
        _dispatcher = new QueuedUiDispatcher { OnWorkPosted = app.Wake };
        _frameTicker = new FrameTicker(onActivated: app.MainWindow.RequestRedraw);
        _lastAnimationTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

        var decorator = context.Get<IPopupNativeDecorator>() ?? new DefaultNoopDecorator();
        _windowChrome = context.Get<IWindowChrome>() ?? new NoopWindowChrome();
        var coordinates = new WindowCoordinates(app.MainWindow, _uiScale);
        _popupFactory = new PopupWindowFactory(
            app, fontBackend, defaultFont, renderBackend, decorator, context, pointerArbiter, _imeCoordinator,
            _uiScale, mainCanvasForFontRegistry: mainCanvas);
        _secondaryWindows = new SecondaryWindowFactory(
            app, fontBackend, defaultFont, renderBackend, decorator, context, pointerArbiter, _imeCoordinator,
            _uiScale, mainCanvasForFontRegistry: mainCanvas);

        _contextMenuManager = new ContextMenuManager(_popupFactory, coordinates, pointerArbiter);

        // A press on the OS title bar / borders / caption buttons is non-client: GLFW never surfaces
        // it, and grabbing a title bar changes no focus (the window already holds it), so the arbiter's
        // client-press and focus-loss dismissals both miss it. Watch the main window's native frame so
        // a title-bar grab while a menu is open still dismisses the menu. No-ops off Windows.
        decorator.WatchWindowNonClientPress(app.MainWindow.NativeHandle, pointerArbiter.NotifyNonClientPress);

        context.Canvas = mainCanvas;
        context.AddService(_mainInput.InputSystem);
        context.AddService<IContextMenuHost>(_contextMenuManager);
        context.AddService<IWindowCoordinates>(coordinates);
        context.AddService<IPopupWindowFactory>(_popupFactory);
        context.AddService<ISecondaryWindowFactory>(_secondaryWindows);
        context.AddService<IUiDispatcher>(_dispatcher);
        context.AddService<IFrameTicker>(_frameTicker);
        context.AddService<IAppForeground>(new AppForeground(app));
        context.AddService(new SvgImageCache(new SvgImageCacheOptions()));

        // Clipboard: the native implementation where one exists, else the window's
        // display-server connection. Apps can still override by registering an IClipboard
        // on the builder before Build.
        if (context.Get<IClipboard>() == null)
            context.AddService(CreatePlatformClipboard(app));

        registerBackendServices(context);

        // All framework and backend services are registered above, so the startup hook and
        // content factory see the fully-wired main-window context. The main window's
        // graphics context is still current from backend resolution, so the startup hook
        // can create engine resources (frame buffers, shaders) the content builds against.
        startup?.Invoke(context);
        MountContent();

        // The app is fully wired and mounted — start the hosted services the host owns (stores that
        // seed the view tree's initial loads, watchers, sync) so callers never hand-start background
        // work after Build.
        context.StartHostedServices();

        // Wire the main-window render loop now that PopulateGui (the draw callback) exists. Doing it
        // here — rather than in the backend resolver, which runs before this instance exists — keeps
        // the callback a direct instance reference instead of a static hole, so a second in-process
        // GuiApp (e.g. the automation runner) can't clobber the first.
        _renderBackend.WireRenderLoop(app.MainWindow, mainCanvas, PopulateGui, (0f, 0f, 0f, 0f), renderHook);

        _uiScale.Changed += HandleUiScaleChanged;
        app.OnTick += HandleTick;
        app.MainWindow.OnResize += HandleResize;
        app.MainWindow.OnFramebufferResize += HandleFramebufferResize;
        app.MainWindow.OnContentScaleChanged += HandleContentScaleChanged;
        app.MainWindow.OnMove += HandleMove;
        app.MainWindow.OnFocusChanged += HandleMainFocusChanged;
        app.MainWindow.OnClose += HandleMainWindowClose;

        // .NET Hot Reload (dotnet watch / Rider) patches edited Build/CreateView IL in place but
        // re-runs nothing, so the live tree keeps drawing the old output. Rebuild it when a delta
        // lands. MetadataUpdater.IsSupported is true only under a hot-reload host, so this is a
        // no-op in a normal or AOT run. The event fires on the agent's background thread, so hop
        // onto the UI loop before touching the tree.
        if (System.Reflection.Metadata.MetadataUpdater.IsSupported)
        {
            _hotReloadHandler = _ => _dispatcher.Post(Reload);
            HotReloadService.UpdateApplied += _hotReloadHandler;
        }

        StartMcpServer(mcpServerPort);
    }

    private void HandleMainFocusChanged(bool focused)
    {
        if (focused)
        {
            // Raised to the front ⇒ move the main window to the top of the arbiter's order so it
            // wins pointer ownership over any secondary window it now overlaps. Modal menus stay
            // unaffected — they win by modality regardless of order.
            _pointerArbiter.Register(_mainInput, isModal: false);
            return;
        }
        // Focus left the main window: close any open menu if the whole app lost focus. The arbiter
        // only dismisses when no arbitrated window (including the menu popup, which is key on macOS)
        // still holds focus, so interacting with an owned popup doesn't self-close the menu.
        _pointerArbiter.NotifyFocusChanged();
    }

    private static IClipboard CreatePlatformClipboard(IWindowedApp app)
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            return new Platforms.Osx.OsxClipboard();
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            return new Platforms.Windows.Win32Clipboard();
        return new WindowClipboard(app);
    }

    /// <summary>Starts a fluent <see cref="GuiAppBuilder"/> for configuring and building a GuiApp.</summary>
    public static GuiAppBuilder CreateBuilder(StartupConfig config) => new(config);

    internal static GuiApp Create(
        StartupConfig config,
        Context context,
        Func<Context, View> contentFactory,
        GuiRenderBackendKind backendKind = GuiRenderBackendKind.Auto,
        Action? renderHook = null,
        Action<Context>? startup = null,
        int? mcpServerPort = null)
    {
        var backend = PlatformBackend.Resolve(config, backendKind);
        return new GuiApp(
            backend.App, backend.MainCanvas, backend.FontBackend,
            backend.RenderBackend,
            backend.DefaultFont, context, contentFactory,
            backend.RegisterServices, renderHook, startup, mcpServerPort);
    }

    public void RegisterFont(string family, string path, int pixelSize)
    {
        var handle = _fontBackend.LoadFontFromFile(PathUtils.ResolveLocalPath(path), ScalePixelSize(pixelSize));
        _mainCanvas.RegisterFont(family, handle);
    }

    public void RegisterFont(string family, byte[] fontData, int pixelSize)
    {
        var handle = _fontBackend.LoadFontFromMemory(fontData, ScalePixelSize(pixelSize));
        _mainCanvas.RegisterFont(family, handle);
    }

    /// Registers a glyph-fallback font (consulted when the primary font lacks a glyph, e.g.
    /// a CJK system font behind the Latin UI font). <paramref name="faceIndex"/> selects the
    /// face inside a .ttc collection. Fallbacks live on the shared font backend, so every
    /// canvas (incl. popups) sees them automatically.
    public void RegisterFallbackFont(string path, int pixelSize, int faceIndex = 0)
    {
        var resolved = Path.IsPathRooted(path) ? path : PathUtils.ResolveLocalPath(path);
        var handle = _fontBackend.LoadFontFromFile(resolved, ScalePixelSize(pixelSize), faceIndex);
        _fontBackend.RegisterFallbackFont(handle);
    }

    /// Registers a glyph-fallback font from bytes already in memory. The font backend isn't
    /// thread-safe, so this must run on the UI thread — but the caller can read the (often large)
    /// font file off-thread and post the bytes here, keeping the file read off the startup path.
    public void RegisterFallbackFontFromMemory(byte[] data, int pixelSize, int faceIndex = 0)
    {
        var handle = _fontBackend.LoadFontFromMemory(data, ScalePixelSize(pixelSize), faceIndex);
        _fontBackend.RegisterFallbackFont(handle);
    }

    // Loads an image into the main canvas and returns the id (the resolved path) to reference
    // it by — pass that id to ImageView.ImageId. Mirrors RegisterFont's local-path resolution.
    public string LoadImage(string path)
    {
        var resolved = PathUtils.ResolveLocalPath(path);
        _mainCanvas.LoadImageFromFile(resolved);
        return resolved;
    }

    private int ScalePixelSize(int pixelSize)
    {
        var scaled = (int)MathF.Round(pixelSize * _mainCanvas.DpiScale);
        return scaled <= 0 ? pixelSize : scaled;
    }

    public void SetIcon(string rgbaPath)
    {
        var bytes = File.ReadAllBytes(PathUtils.ResolveLocalPath(rgbaPath));
        var count = BitConverter.ToInt32(bytes, 0);
        var icons = new List<WindowIconImage>(count);
        var offset = 4;
        for (var i = 0; i < count; i++)
        {
            var w = BitConverter.ToInt32(bytes, offset); offset += 4;
            var h = BitConverter.ToInt32(bytes, offset); offset += 4;
            var len = w * h * 4;
            var pixels = new byte[len];
            Buffer.BlockCopy(bytes, offset, pixels, 0, len);
            offset += len;
            icons.Add(new WindowIconImage(w, h, pixels));
        }
        _app.MainWindow.SetIcon(icons);
    }

    public event Action<int, int>? OnWindowResized;

    /// <summary>Fires when the main window is moved, with its new top-left screen position —
    /// for persisting placement so it can be restored on next launch.</summary>
    public event Action<int, int>? OnWindowMoved;

    /// <summary>
    ///     Switches the main window's native title bar between dark and light
    ///     appearance. No-op on platforms without a registered <see cref="IWindowChrome"/>.
    /// </summary>
    public void SetTitleBarDark(bool dark) =>
        _windowChrome.SetTitleBarTheme(_app.MainWindow, dark);

    /// <summary>
    /// Sets the UI's base writing direction on the main canvas, flipping text alignment and the bidi
    /// base for direction-neutral lines. Popups opened afterward inherit it (they copy the main
    /// canvas), so call it before opening RTL menus/dialogs. Schedules a repaint.
    /// </summary>
    public void SetBaseDirection(BidiDirection direction)
    {
        _mainCanvas.DefaultBaseDirection = direction;
        _app.MainWindow.RequestRedraw();
    }

    /// <summary>Captures the next rendered main-window frame to a PNG at <paramref name="path"/> —
    /// pixel-perfect, via the GPU backend's framebuffer read-back (no-op on backends without it).
    /// Bind it to a debug shortcut or a menu action to dump exactly what's on screen, e.g. for an
    /// LLM to inspect alongside the headless <c>GuiTestHarness</c> snapshot.</summary>
    public void CaptureScreenshot(string path) => CaptureScreenshot(path, null);

    /// <inheritdoc cref="CaptureScreenshot(string)"/>
    /// <param name="onComplete">Runs on the render thread once the capture attempt finishes.</param>
    public void CaptureScreenshot(string path, Action? onComplete)
    {
        _renderBackend.RequestScreenshot(path, onComplete);
        _app.MainWindow.RequestRedraw();
    }

    /// <summary>Projects every live window — main, open secondary windows, and shown popups
    /// (context menus, tooltips) — to a flat <see cref="GuiSurface"/> list, oldest/topmost order:
    /// main, then secondaries, then popups in open order (last = topmost). Rebuilt per call so a
    /// pooled/released popup simply drops out next time. Drives the multi-window MCP tools.</summary>
    private IReadOnlyList<GuiSurface> CollectSurfaces()
    {
        var list = new List<GuiSurface>
        {
            new("main", _app.MainWindow, _mainHost.Root, _mainInput, _mainHost.Space.Scale),
        };
        foreach (var s in _secondaryWindows.Active)
            list.Add(new GuiSurface("secondary", s.Window, s.Root, s.Input, s.Scale));
        foreach (var p in _popupFactory.ActivePopups)
            list.Add(new GuiSurface(
                p.MousePassThrough ? "tooltip" : "context-menu", p.Window, p.Root, p.Input, p.Space.Scale));
        return list;
    }

    /// <summary>Screenshots a specific window (not just main). Renders <paramref name="window"/>
    /// synchronously so its frame consumes the backend's single pending-capture slot — no race with
    /// other windows' redraws — then restores the main GL context when the target wasn't main.</summary>
    private void CaptureWindowScreenshot(IWindow window, string path, Action? onComplete)
    {
        _renderBackend.RequestScreenshot(path, onComplete);
        _renderBackend.RenderWindowNow(window);
        if (!ReferenceEquals(window, _app.MainWindow))
            _renderBackend.MakeMainContextCurrent();
    }

    /// <summary>
    /// A scripted driver over this app's live windows — find, click, type, wait, screenshot. Call it
    /// from a background thread; every action marshals onto the UI thread, so driving it from the UI
    /// thread would deadlock and nothing would repaint. Same engine the MCP server runs on.
    /// </summary>
    public GuiDriver CreateDriver() => new(CollectSurfaces, _dispatcher, CaptureWindowScreenshot);

    private void StartMcpServer(int? configuredPort)
    {
        var port = configuredPort ?? ResolveEnvMcpPort();
        if (port is not { } p) return;
        var server = new GuiMcpServer(CreateDriver());
        try
        {
            server.Start(p);
            _mcpServer = server;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GuiMcpServer] failed to start on port {p}: {ex.Message}");
        }
    }

    private static int? ResolveEnvMcpPort()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZGF_GUI_MCP"))) return null;
        return int.TryParse(Environment.GetEnvironmentVariable("ZGF_GUI_MCP_PORT"), out var p) ? p : 5577;
    }

    public void Run() => _app.Run();

    /// <summary>Asks the run loop to exit; <see cref="Run"/> returns after the current iteration.</summary>
    /// <remarks>Unconditional: <see cref="OnCloseRequested"/> handlers do not see it. Anything a user
    /// can trigger goes through <see cref="RequestQuit"/> instead, so that a handler holding the app
    /// open is not bypassed; this is what a handler calls once the user has agreed to close.</remarks>
    public void Quit() => _app.Quit();

    /// <summary>
    /// Raised before the application closes, for handlers that need to hold it open — unsaved work,
    /// a process that would be killed. Cancelling makes the request the handler's to resolve.
    /// </summary>
    /// <remarks>
    /// Every OS close arrives here: the title-bar button, Alt+F4, and macOS's Quit, which asks each
    /// window to close rather than terminating outright. Handlers run on the UI thread inside the
    /// event poll, so a handler that wants to show something should post it rather than build it
    /// here.
    /// </remarks>
    public event Action<CloseRequest>? OnCloseRequested;

    /// <summary>
    /// Asks to close, letting <see cref="OnCloseRequested"/> handlers hold the application open.
    /// Returns whether it is closing.
    /// </summary>
    public bool RequestQuit()
    {
        var request = new CloseRequest();
        OnCloseRequested?.Invoke(request);

        if (request.IsCancelled)
        {
            // The window may already be marked as closing — the OS marks it before raising the
            // request — so withdrawing the mark is what actually keeps the run loop going.
            _app.MainWindow.CancelClose();
            return false;
        }

        _app.Quit();
        return true;
    }

    private void HandleMainWindowClose() => RequestQuit();

    /// <summary>Schedules a main-window repaint — for embedded rendering that animates
    /// state the view tree doesn't know about (e.g. a scene's model matrix).</summary>
    public void RequestRedraw() => _app.MainWindow.RequestRedraw();

    /// <summary>Makes the main window's graphics context current — for engine resource
    /// work outside the render hook (loads, rebuilds).</summary>
    public void MakeMainContextCurrent() => _renderBackend.MakeMainContextCurrent();

    private void HandleTick()
    {
        _dispatcher.Drain();
        TickAnimations();
        _mainInput.Update();
        _popupFactory.UpdateActivePopupInput();
        _secondaryWindows.Update();
        _contextMenuManager.Update();
        // Last: menus opened and closed by this tick's input are already settled, so the IME state is
        // derived from what is actually on screen rather than from what a field remembered to set.
        _imeCoordinator.Update();
    }

    private void TickAnimations()
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var dt = (float)((now - _lastAnimationTimestamp) / (double)System.Diagnostics.Stopwatch.Frequency);
        _lastAnimationTimestamp = now;
        // An idle wait or a stall isn't animation time — cap the step so the first frame after
        // a gap doesn't lurch.
        const float maxStep = 0.1f;
        _frameTicker.Tick(dt > maxStep ? maxStep : dt);
    }

    // The reported width and height are screen coordinates, which is what a caller persists; the
    // canvas takes its own size from the framebuffer instead, through the host.
    private void HandleResize(int width, int height)
    {
        _mainHost.SyncScale();
        _renderBackend.RenderWindowNow(_app.MainWindow);
        OnWindowResized?.Invoke(width, height);
    }

    // Both this and OnResize re-sync: GLFW does not order the two callbacks, so whichever arrives
    // first may still see the other's stale half.
    private void HandleFramebufferResize(int width, int height)
    {
        _mainHost.SyncScale();
        _renderBackend.OnFramebufferResize(width, height);
    }

    // Popups aren't re-synced here: they're pooled and every acquire re-derives the popup's scale
    // and size, so one that outlives the change comes back correct rather than stale.
    private void HandleUiScaleChanged(float scale)
    {
        _mainHost.SyncScale();
        _secondaryWindows.SyncScale();
        _app.MainWindow.RequestRedraw();
    }

    // Dragged onto a display with different scaling. Only the main window is re-synced here: each
    // secondary window watches its own, since they can sit on different monitors.
    private void HandleContentScaleChanged(float contentScale)
    {
        _mainHost.SyncScale();
        _app.MainWindow.RequestRedraw();
    }

    private void HandleMove(int x, int y) => OnWindowMoved?.Invoke(x, y);

    private void PopulateGui() => _mainHost.DrawContent();

    private void MountContent() => SetRootContent(_contentFactory(_context));

    private void SetRootContent(View content) =>
        _mainHost.SetRoot(new ContainerView
        {
            Width = _mainCanvas.Width,
            Height = _mainCanvas.Height,
            Children = { content },
        });

    /// <summary>
    /// Rebuilds the main window's view tree from the content factory, picking up edited
    /// Build/CreateView code. Application state lives in the DI <see cref="Context"/> (stores and
    /// view models resolved as singletons), not in the views, so the rebuild preserves it. The new
    /// content is built before the old tree is torn down, so a Build that throws after an edit
    /// leaves the previous tree mounted and live instead of blanking the window. Runs on the UI
    /// thread — see the hot-reload wiring in the constructor.
    /// </summary>
    public void Reload()
    {
        View content;
        try
        {
            content = _contentFactory(_context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotReload] rebuild failed, keeping current view tree: {ex}");
            return;
        }

        SetRootContent(content);
    }

    public void Dispose()
    {
        Context?.Dispose();
        _mcpServer?.Dispose();

        if (_hotReloadHandler != null)
            HotReloadService.UpdateApplied -= _hotReloadHandler;

        // Unmount the whole view tree so behaviors release their per-mount resources
        // (subscriptions, input registrations, view models).
        _mainHost.SetRoot(null);

        _uiScale.Changed -= HandleUiScaleChanged;
        _app.OnTick -= HandleTick;
        _app.MainWindow.OnResize -= HandleResize;
        _app.MainWindow.OnFramebufferResize -= HandleFramebufferResize;
        _app.MainWindow.OnContentScaleChanged -= HandleContentScaleChanged;
        _app.MainWindow.OnMove -= HandleMove;
        _app.MainWindow.OnFocusChanged -= HandleMainFocusChanged;
        _app.MainWindow.OnClose -= HandleMainWindowClose;
        _secondaryWindows.Dispose();
        _popupFactory.Dispose();
        // Secondary/popup teardown above left their own (now-destroyed) contexts current. The
        // render backend's shared GL objects live in the main window's context share group, so
        // make it current before deleting them. The main window is still alive here — it's
        // destroyed in _app.Dispose() below.
        _renderBackend.MakeMainContextCurrent();
        _renderBackend.Dispose();
        _app.Dispose();
    }
}
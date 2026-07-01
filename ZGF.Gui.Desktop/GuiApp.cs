using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Desktop.Backends.OpenGl;
using ZGF.Fonts;
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
    private readonly GuiWindowHost _mainHost;
    private readonly QueuedUiDispatcher _dispatcher;
    private readonly FrameTicker _frameTicker;
    private long _lastAnimationTimestamp;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly PopupWindowFactory _popupFactory;
    private readonly SecondaryWindowFactory _secondaryWindows;
    private readonly IWindowChrome _windowChrome;
    private readonly Context _context;
    private readonly Func<Context, View> _contentFactory;
    private readonly Action<Type[]?>? _hotReloadHandler;
    private GuiMcpServer? _mcpServer;

    private GuiApp(
        IWindowedApp app,
        RenderedCanvasBase mainCanvas,
        FreeTypeFontBackend fontBackend,
        IGuiRenderBackend renderBackend,
        FontHandle defaultFont,
        Context context,
        Func<Context, View> contentFactory,
        Action<Context> registerBackendServices,
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
        _mainInput = new DesktopInputSystem(app.MainWindow, mainCanvas, pointerArbiter);
        pointerArbiter.Register(_mainInput, isModal: false);
        _mainHost = new GuiWindowHost(app.MainWindow, mainCanvas, _mainInput, context, sizeRootToWindow: true);
        _dispatcher = new QueuedUiDispatcher { OnWorkPosted = app.Wake };
        _frameTicker = new FrameTicker(onActivated: app.MainWindow.RequestRedraw);
        _lastAnimationTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

        var decorator = context.Get<IPopupNativeDecorator>() ?? new DefaultNoopDecorator();
        _windowChrome = context.Get<IWindowChrome>() ?? new NoopWindowChrome();
        var coordinates = new WindowCoordinates(app.MainWindow, mainCanvas);
        _popupFactory = new PopupWindowFactory(
            app, fontBackend, defaultFont, renderBackend, decorator, context, pointerArbiter,
            mainCanvasForFontRegistry: mainCanvas);
        _secondaryWindows = new SecondaryWindowFactory(
            app, fontBackend, defaultFont, renderBackend, decorator, context, pointerArbiter,
            mainCanvasForFontRegistry: mainCanvas);

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

        PlatformBackend.PopulateMain = PopulateGui;

        app.OnTick += HandleTick;
        app.MainWindow.OnResize += HandleResize;
        app.MainWindow.OnFramebufferResize += HandleFramebufferResize;
        app.MainWindow.OnFocusChanged += HandleMainFocusChanged;

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
        var backend = PlatformBackend.Resolve(config, backendKind, renderHook);
        return new GuiApp(
            backend.App, backend.MainCanvas, backend.FontBackend,
            backend.RenderBackend,
            backend.DefaultFont, context, contentFactory,
            backend.RegisterServices, startup, mcpServerPort);
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
        var list = new List<GuiSurface> { new("main", _app.MainWindow, _mainHost.Root, _mainInput) };
        foreach (var s in _secondaryWindows.Active)
            list.Add(new GuiSurface("secondary", s.Window, s.Root, s.Input));
        foreach (var p in _popupFactory.ActivePopups)
            list.Add(new GuiSurface(p.MousePassThrough ? "tooltip" : "context-menu", p.Window, p.Root, p.Input));
        return list;
    }

    /// <summary>Screenshots a specific window (not just main). Renders <paramref name="window"/>
    /// synchronously so its frame consumes the backend's single pending-capture slot — no race with
    /// other windows' redraws — then restores the main GL context when the target wasn't main.</summary>
    private void CaptureWindowScreenshot(IWindow window, string path, Action? onComplete)
    {
        _renderBackend.RequestScreenshot(path, onComplete);
        window.MakeContextCurrent();
        window.RenderNow();
        if (!ReferenceEquals(window, _app.MainWindow))
            _app.MakeMainContextCurrent();
    }

    private void StartMcpServer(int? configuredPort)
    {
        var port = configuredPort ?? ResolveEnvMcpPort();
        if (port is not { } p) return;
        var server = new GuiMcpServer(CollectSurfaces, _dispatcher, CaptureWindowScreenshot);
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
    public void Quit() => _app.Quit();

    /// <summary>Schedules a main-window repaint — for embedded rendering that animates
    /// state the view tree doesn't know about (e.g. a scene's model matrix).</summary>
    public void RequestRedraw() => _app.MainWindow.RequestRedraw();

    /// <summary>Makes the main window's graphics context current — for engine resource
    /// work outside the render hook (loads, rebuilds).</summary>
    public void MakeMainContextCurrent() => _app.MakeMainContextCurrent();

    private void HandleTick()
    {
        _dispatcher.Drain();
        TickAnimations();
        _mainInput.Update();
        _popupFactory.UpdateActivePopupInput();
        _secondaryWindows.Update();
        _contextMenuManager.Update();
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

    private void HandleResize(int width, int height)
    {
        _mainHost.HandleResize(width, height);
        _app.MainWindow.MakeContextCurrent();
        _app.MainWindow.RenderNow();
        OnWindowResized?.Invoke(width, height);
    }

    private void HandleFramebufferResize(int width, int height)
    {
        if (_app is OpenGlApp) GL46.glViewport(0, 0, width, height);
    }

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
        _mcpServer?.Dispose();

        if (_hotReloadHandler != null)
            HotReloadService.UpdateApplied -= _hotReloadHandler;

        // Unmount the whole view tree so behaviors release their per-mount resources
        // (subscriptions, input registrations, view models).
        _mainHost.SetRoot(null);

        _app.OnTick -= HandleTick;
        _app.MainWindow.OnResize -= HandleResize;
        _app.MainWindow.OnFramebufferResize -= HandleFramebufferResize;
        _app.MainWindow.OnFocusChanged -= HandleMainFocusChanged;
        _secondaryWindows.Dispose();
        _popupFactory.Dispose();
        _renderBackend.Dispose();
        _app.Dispose();
    }
}
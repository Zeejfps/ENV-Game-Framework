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
    private readonly GuiWindowHost _mainHost;
    private readonly QueuedUiDispatcher _dispatcher;
    private readonly FrameTicker _frameTicker;
    private long _lastAnimationTimestamp;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly PopupWindowFactory _popupFactory;
    private readonly SecondaryWindowFactory _secondaryWindows;
    private readonly IWindowChrome _windowChrome;

    private GuiApp(
        IWindowedApp app,
        RenderedCanvasBase mainCanvas,
        FreeTypeFontBackend fontBackend,
        IGuiRenderBackend renderBackend,
        FontHandle defaultFont,
        Context context,
        Func<Context, View> contentFactory,
        Action<Context> registerBackendServices,
        Action<Context>? startup)
    {
        _app = app;
        _mainCanvas = mainCanvas;
        _fontBackend = fontBackend;
        _renderBackend = renderBackend;

        // One arbiter shared by every window's input system decides pointer ownership.
        // The main window is the base (non-modal) layer; popups register themselves.
        var pointerArbiter = new PointerOwnershipArbiter();
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
            app, fontBackend, defaultFont, renderBackend, context,
            mainCanvasForFontRegistry: mainCanvas);

        _contextMenuManager = new ContextMenuManager(_popupFactory, coordinates, _mainInput);

        context.Canvas = mainCanvas;
        context.AddService(_mainInput.InputSystem);
        context.AddService<IContextMenuHost>(_contextMenuManager);
        context.AddService<IWindowCoordinates>(coordinates);
        context.AddService<IPopupWindowFactory>(_popupFactory);
        context.AddService<ISecondaryWindowFactory>(_secondaryWindows);
        context.AddService<IUiDispatcher>(_dispatcher);
        context.AddService<IFrameTicker>(_frameTicker);

        // Default clipboard routes through the window's display-server connection. Platforms
        // with a native implementation (Win32/macOS) register their own before this runs.
        if (context.Get<IClipboard>() == null)
            context.AddService<IClipboard>(new WindowClipboard(app));

        registerBackendServices(context);

        // All framework and backend services are registered above, so the startup hook and
        // content factory see the fully-wired main-window context. The main window's
        // graphics context is still current from backend resolution, so the startup hook
        // can create engine resources (frame buffers, shaders) the content builds against.
        startup?.Invoke(context);
        var content = contentFactory(context);

        _mainHost.SetRoot(new ContainerView
        {
            Width = mainCanvas.Width,
            Height = mainCanvas.Height,
            Children = { content },
        });

        PlatformBackend.PopulateMain = PopulateGui;

        app.OnTick += HandleTick;
        app.MainWindow.OnResize += HandleResize;
        app.MainWindow.OnFramebufferResize += HandleFramebufferResize;
        app.MainWindow.OnFocusChanged += HandleMainFocusChanged;
    }

    private void HandleMainFocusChanged(bool focused)
    {
        if (focused) return;
        // On macOS, clicking a popup NSWindow makes it key and main loses focus.
        // Treat focus loss as "close menus" only when the focus moved outside
        // our process — i.e., none of our windows are focused. Otherwise the
        // user is interacting with a menu popup we own.
        foreach (var w in _app.Windows)
        {
            if (w.IsFocused)
                return;
        }
        _contextMenuManager.CloseAllImmediately();
    }

    /// <summary>Starts a fluent <see cref="GuiAppBuilder"/> for configuring and building a GuiApp.</summary>
    public static GuiAppBuilder CreateBuilder(StartupConfig config) => new(config);

    internal static GuiApp Create(
        StartupConfig config,
        Context context,
        Func<Context, View> contentFactory,
        GuiRenderBackendKind backendKind = GuiRenderBackendKind.Auto,
        Action? renderHook = null,
        Action<Context>? startup = null)
    {
        var backend = PlatformBackend.Resolve(config, backendKind, renderHook);
        return new GuiApp(
            backend.App, backend.MainCanvas, backend.FontBackend,
            backend.RenderBackend,
            backend.DefaultFont, context, contentFactory,
            backend.RegisterServices, startup);
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

    public void Dispose()
    {
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
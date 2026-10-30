using ZGF.Desktop;
using ZGF.Desktop.Backends.Metal;
using ZGF.Fonts;
using ZGF.Gui.Metal;
using ZGF.Rendering.Metal;

namespace ZGF.Gui.Hosting;

// Metal has no per-thread current context to switch — each frame binds its own drawable — so every
// context step the OpenGL backend takes is absent here.
internal sealed class MetalRenderBackend : IGuiRenderBackend
{
    private readonly MetalApp _app;
    private readonly MetalImageManager _images;
    private readonly MetalSharedResources _shared;
    private readonly MetalRenderedCanvas _mainCanvas;
    private readonly PendingScreenshot _screenshot = new();
    private MetalSurfaceRenderer? _mainSurface;

    public MetalRenderBackend(StartupConfig config)
    {
        _app = new MetalApp(config);
        var dpiScale = _app.MainWindow.DpiScale;
        Fonts = new FreeTypeFontBackend();
        DefaultFont = GuiFonts.LoadDefault(Fonts, dpiScale);
        _images = new MetalImageManager(_app.Device, _app.CommandQueue);
        _shared = new MetalSharedResources(_app.Device, _app.CommandQueue, Fonts, _images);
        _mainCanvas = new MetalRenderedCanvas(config.WindowWidth, config.WindowHeight, Fonts, DefaultFont, _shared, dpiScale);
    }

    public IWindowedApp App => _app;
    public FreeTypeFontBackend Fonts { get; }
    public FontHandle DefaultFont { get; }
    public RenderedCanvasBase MainCanvas => _mainCanvas;

    public RenderedCanvasBase CreateCanvas(IWindow window, int width, int height)
    {
        var canvas = new MetalRenderedCanvas(width, height, Fonts, DefaultFont, _shared, window.DpiScale);
        canvas.CopyFontsFrom(_mainCanvas);
        return canvas;
    }

    public void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null)
    {
        var metalWindow = (MetalWindow)window;
        var metalCanvas = (MetalRenderedCanvas)canvas;
        var surface = new MetalSurfaceRenderer(metalWindow, new MTLClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
        var isMain = ReferenceEquals(window, _app.MainWindow);
        if (isMain)
            _mainSurface = surface;
        MetalSurfaceRenderer.FrameCallback draw = Draw;
        PendingScreenshot.Capture capture = surface.TryTakeCapture;

        void Draw(IntPtr encoder, IntPtr commandBuffer)
        {
            preDraw?.Invoke();
            canvas.BeginFrame();
            drawContent();
            metalCanvas.EndFrame(encoder, commandBuffer);
        }

        void Render()
        {
            surface.RenderFrame(draw);
            if (isMain)
                _screenshot.Fulfil(capture);
        }

        metalWindow.RenderFrame = Render;
    }

    public void WirePresentLoop(IWindow window, RenderedCanvasBase canvas)
    {
        var metalWindow = (MetalWindow)window;
        var surface = new MetalSurfaceRenderer(metalWindow, new MTLClearColor(0, 0, 0, 0));
        MetalSurfaceRenderer.FrameCallback present = ((MetalRenderedCanvas)canvas).EndFrame;
        metalWindow.RenderFrame = () => surface.RenderFrame(present);
    }

    public void OnFramebufferResize(int width, int height) { }

    public void RenderWindowNow(IWindow window) => ((MetalWindow)window).RenderNow();

    public void DisposeWindow(IWindow window, RenderedCanvasBase canvas)
    {
        ((MetalRenderedCanvas)canvas).Dispose();
        window.Dispose();
    }

    public void MakeMainContextCurrent() { }

    public void RequestScreenshot(string path, Action? onComplete = null)
    {
        _screenshot.Request(path, onComplete);
        _mainSurface?.RequestCapture();
    }

    public void RegisterServices(Context context) => context.AddService(_images);

    public void Dispose()
    {
        _mainCanvas.Dispose();
        _shared.Dispose();
        _app.Dispose();
    }
}

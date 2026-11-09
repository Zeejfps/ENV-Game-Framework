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
    private readonly bool _offscreen;
    private readonly Dictionary<IWindow, MetalOffscreenTexture> _targets = new();
    private MetalSurfaceRenderer? _mainSurface;

    /// <param name="offscreen">Whether every window draws into a texture of its own rather than into the window,
    /// read back with <see cref="ReadOffscreen"/>, and <see cref="RenderWindowNow"/> never presents.</param>
    public MetalRenderBackend(StartupConfig config, bool offscreen = false)
    {
        _offscreen = offscreen;
        _app = new MetalApp(config);
        MetalSharedResources? shared = null;
        try
        {
            var dpiScale = _app.MainWindow.DpiScale;
            Fonts = new FreeTypeFontBackend();
            DefaultFont = GuiFonts.LoadDefault(Fonts, dpiScale);
            _images = new MetalImageManager(_app.Device, _app.CommandQueue);
            _shared = shared = new MetalSharedResources(_app.Device, _app.CommandQueue, Fonts, _images);
            _mainCanvas = new MetalRenderedCanvas(config.WindowWidth, config.WindowHeight, Fonts, DefaultFont, _shared, dpiScale);
        }
        catch
        {
            shared?.Dispose();
            _app.Dispose();
            throw;
        }
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
        if (_offscreen)
        {
            WireOffscreenRenderLoop(window, canvas, drawContent, clearColor, preDraw);
            return;
        }
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

    private void WireOffscreenRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw)
    {
        var metalCanvas = (MetalRenderedCanvas)canvas;
        var clear = new MTLClearColor(clearColor.R * clearColor.A, clearColor.G * clearColor.A, clearColor.B * clearColor.A, clearColor.A);
        var isMain = ReferenceEquals(window, _app.MainWindow);
        MetalSurfaceRenderer.FrameCallback draw = metalCanvas.EndFrame;
        PendingScreenshot.Capture capture = ReadBack;

        bool ReadBack(out int width, out int height, out byte[] rgba)
        {
            var image = ReadOffscreen(window);
            width = image?.Width ?? 0;
            height = image?.Height ?? 0;
            rgba = image?.ToStraightAlpha() ?? [];
            return image != null;
        }

        void Render()
        {
            preDraw?.Invoke();
            canvas.BeginFrame();
            drawContent();
            RenderOffscreen(window, canvas, clear, draw);
            if (isMain)
                _screenshot.Fulfil(capture);
        }

        ((MetalWindow)window).RenderFrame = Render;
    }

    public void WirePresentLoop(IWindow window, RenderedCanvasBase canvas)
    {
        var metalWindow = (MetalWindow)window;
        MetalSurfaceRenderer.FrameCallback present = ((MetalRenderedCanvas)canvas).EndFrame;
        if (_offscreen)
        {
            metalWindow.RenderFrame = () => RenderOffscreen(window, canvas, new MTLClearColor(0, 0, 0, 0), present);
            return;
        }
        var surface = new MetalSurfaceRenderer(metalWindow, new MTLClearColor(0, 0, 0, 0));
        metalWindow.RenderFrame = () => surface.RenderFrame(present);
    }

    public void OnFramebufferResize(int width, int height) { }

    public void RenderWindowNow(IWindow window)
    {
        var metalWindow = (MetalWindow)window;
        if (_offscreen)
            metalWindow.RenderFrame?.Invoke();
        else
            metalWindow.RenderNow();
    }

    public SurfaceImage? ReadOffscreen(IWindow window) =>
        _targets.TryGetValue(window, out var target) ? target.Read() : null;

    public void DisposeWindow(IWindow window, RenderedCanvasBase canvas)
    {
        ((MetalRenderedCanvas)canvas).Dispose();
        if (_targets.Remove(window, out var target))
            target.Dispose();
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
        foreach (var target in _targets.Values)
            target.Dispose();
        _targets.Clear();
        _mainCanvas.Dispose();
        _shared.Dispose();
        _app.Dispose();
    }

    private void RenderOffscreen(IWindow window, RenderedCanvasBase canvas, MTLClearColor clear, MetalSurfaceRenderer.FrameCallback encode)
    {
        if (!_targets.TryGetValue(window, out var target))
            _targets[window] = target = new MetalOffscreenTexture(_app.Device, _app.CommandQueue);
        var width = Math.Max(1, (int)MathF.Round(canvas.Width * canvas.DpiScale));
        var height = Math.Max(1, (int)MathF.Round(canvas.Height * canvas.DpiScale));
        target.Render(width, height, clear, encode);
    }
}

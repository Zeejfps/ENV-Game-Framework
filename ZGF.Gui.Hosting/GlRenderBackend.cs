using ZGF.Desktop;
using ZGF.Desktop.Backends;
using ZGF.Desktop.Backends.OpenGl;
using ZGF.Fonts;
using ZGF.Gui.OpenGL;
using static GL46;

namespace ZGF.Gui.Hosting;

internal sealed class GlRenderBackend : IGuiRenderBackend
{
    private static readonly (float R, float G, float B, float A) Transparent = (0f, 0f, 0f, 0f);

    private readonly OpenGlApp _app;
    private readonly OpenGlWindow _mainWindow;
    private readonly GlImageManager _images = new();
    private readonly GlSharedResources _shared;
    private readonly OpenGlRenderedCanvas _mainCanvas;
    private readonly PendingScreenshot _screenshot = new();

    public GlRenderBackend(StartupConfig config)
    {
        _app = new OpenGlApp(config);
        _mainWindow = (OpenGlWindow)_app.MainWindow;
        Fonts = new FreeTypeFontBackend();
        DefaultFont = GuiFonts.LoadDefault(Fonts, _mainWindow.DpiScale);
        _shared = new GlSharedResources(Fonts, _images);
        _mainCanvas = new OpenGlRenderedCanvas(config.WindowWidth, config.WindowHeight, Fonts, DefaultFont, _shared, _mainWindow.DpiScale);
    }

    public IWindowedApp App => _app;
    public FreeTypeFontBackend Fonts { get; }
    public FontHandle DefaultFont { get; }
    public RenderedCanvasBase MainCanvas => _mainCanvas;

    public RenderedCanvasBase CreateCanvas(IWindow window, int width, int height)
    {
        MakeCurrent(window);
        var canvas = new OpenGlRenderedCanvas(width, height, Fonts, DefaultFont, _shared, window.DpiScale);
        canvas.CopyFontsFrom(_mainCanvas);
        MakeMainContextCurrent();
        return canvas;
    }

    public void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null)
    {
        var glCanvas = (OpenGlRenderedCanvas)canvas;
        PendingScreenshot.Capture capture = ReadBack;

        bool ReadBack(out int width, out int height, out byte[] rgba)
        {
            rgba = glCanvas.ReadFramebufferRgba(out width, out height);
            return true;
        }

        void Render()
        {
            preDraw?.Invoke();
            Clear(clearColor);
            canvas.BeginFrame();
            drawContent();
            canvas.EndFrame();
            ClearUnderCaptionButtons(window);
            // Read back after the draw and before the swap, while the back buffer holds this frame.
            _screenshot.Fulfil(capture);
        }

        ((OpenGlWindow)window).RenderFrame = Render;
    }

    public void WirePresentLoop(IWindow window, RenderedCanvasBase canvas)
    {
        void Present()
        {
            Clear(Transparent);
            canvas.EndFrame();
        }

        ((OpenGlWindow)window).RenderFrame = Present;
    }

    public void OnFramebufferResize(int width, int height) => glViewport(0, 0, width, height);

    public void RenderWindowNow(IWindow window)
    {
        MakeCurrent(window);
        ((GlfwWindowBase)window).RenderNow();
        MakeMainContextCurrent();
    }

    // VAOs are per context, not shared across the share group: deleting the canvas's under any other
    // context would destroy that context's same-numbered ones.
    public void DisposeWindow(IWindow window, RenderedCanvasBase canvas)
    {
        MakeCurrent(window);
        ((OpenGlRenderedCanvas)canvas).Dispose();
        window.Dispose();
        MakeMainContextCurrent();
    }

    public void MakeMainContextCurrent() => _mainWindow.MakeContextCurrent();

    public void RequestScreenshot(string path, Action? onComplete = null) => _screenshot.Request(path, onComplete);

    public void RegisterServices(Context context) => context.AddService(_images);

    public void Dispose()
    {
        MakeMainContextCurrent();
        _mainCanvas.Dispose();
        _shared.Dispose();
        _app.Dispose();
    }

    private static void MakeCurrent(IWindow window) => ((OpenGlWindow)window).MakeContextCurrent();

    /// <summary>Makes the frame fully transparent where the platform draws its caption buttons over the
    /// content, so that they show through.</summary>
    private static void ClearUnderCaptionButtons(IWindow window)
    {
        var buttons = window.CaptionButtons;
        if (buttons.Width <= 0 || buttons.Height <= 0)
            return;
        var scale = window.DpiScale;
        var left = (int)MathF.Floor(buttons.X * scale);
        var top = (int)MathF.Floor(buttons.Y * scale);
        var right = (int)MathF.Ceiling(buttons.Right * scale);
        var bottom = (int)MathF.Ceiling(buttons.Bottom * scale);
        glEnable(GL_SCISSOR_TEST);
        glScissor(left, window.FramebufferHeight - bottom, right - left, bottom - top);
        Clear(Transparent);
        glDisable(GL_SCISSOR_TEST);
    }

    private static void Clear((float R, float G, float B, float A) color)
    {
        glClearColor(color.R, color.G, color.B, color.A);
        glClear(GL_COLOR_BUFFER_BIT);
    }
}

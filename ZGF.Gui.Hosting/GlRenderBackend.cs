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
    private readonly GlImageManager _images = new(GLFW.Glfw.GetProcAddress);
    private readonly GlSharedResources _shared;
    private readonly OpenGlRenderedCanvas _mainCanvas;
    private readonly PendingScreenshot _screenshot = new();
    private readonly bool _offscreen;
    private readonly Dictionary<IWindow, GlOffscreenFramebuffer> _targets = new();

    /// <param name="offscreen">Whether every window draws into a framebuffer object of its own rather than
    /// into the window, read back with <see cref="ReadOffscreen"/>, and <see cref="RenderWindowNow"/> never presents.</param>
    public GlRenderBackend(StartupConfig config, bool offscreen = false)
    {
        _offscreen = offscreen;
        _app = OpenGlApp.Create(config);
        GlSharedResources? shared = null;
        try
        {
            _mainWindow = (OpenGlWindow)_app.MainWindow;
            Fonts = new FreeTypeFontBackend();
            DefaultFont = GuiFonts.LoadDefault(Fonts, _mainWindow.DpiScale);
            _shared = shared = new GlSharedResources(Fonts, _images);
            _mainCanvas = new OpenGlRenderedCanvas(config.WindowWidth, config.WindowHeight, Fonts, DefaultFont, _shared, _mainWindow.DpiScale);
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
        PendingScreenshot.Capture offscreenCapture = ReadBackOffscreen;

        bool ReadBack(out int width, out int height, out byte[] rgba)
        {
            rgba = glCanvas.ReadFramebufferRgba(out width, out height);
            return true;
        }

        bool ReadBackOffscreen(out int width, out int height, out byte[] rgba)
        {
            var image = _targets[window].Read();
            width = image.Width;
            height = image.Height;
            rgba = image.ToStraightAlpha();
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

        void RenderOffscreen()
        {
            canvas.BeginFrame();
            drawContent();
            BindTarget(window, canvas);
            preDraw?.Invoke();
            Clear(Premultiplied(clearColor));
            canvas.EndFrame();
            _screenshot.Fulfil(offscreenCapture);
            GlOffscreenFramebuffer.Unbind();
        }

        ((OpenGlWindow)window).RenderFrame = _offscreen ? RenderOffscreen : Render;
    }

    public void WirePresentLoop(IWindow window, RenderedCanvasBase canvas)
    {
        void Present()
        {
            Clear(Transparent);
            canvas.EndFrame();
        }

        void PresentOffscreen()
        {
            BindTarget(window, canvas);
            Clear(Transparent);
            canvas.EndFrame();
            GlOffscreenFramebuffer.Unbind();
        }

        ((OpenGlWindow)window).RenderFrame = _offscreen ? PresentOffscreen : Present;
    }

    public void OnFramebufferResize(int width, int height) => glViewport(0, 0, width, height);

    public void RenderWindowNow(IWindow window)
    {
        MakeCurrent(window);
        if (_offscreen)
            ((OpenGlWindow)window).RenderFrame?.Invoke();
        else
            ((GlfwWindowBase)window).RenderNow();
        MakeMainContextCurrent();
    }

    public SurfaceImage? ReadOffscreen(IWindow window)
    {
        if (!_targets.TryGetValue(window, out var target))
            return null;
        MakeCurrent(window);
        try
        {
            return target.Read();
        }
        finally
        {
            MakeMainContextCurrent();
        }
    }

    // VAOs are per context, not shared across the share group: deleting the canvas's under any other
    // context would destroy that context's same-numbered ones.
    public void DisposeWindow(IWindow window, RenderedCanvasBase canvas)
    {
        MakeCurrent(window);
        ((OpenGlRenderedCanvas)canvas).Dispose();
        if (_targets.Remove(window, out var target))
            target.Dispose();
        window.Dispose();
        MakeMainContextCurrent();
    }

    /// <summary>Draws on <paramref name="window"/>'s framebuffer object from here on, made or resized to
    /// <paramref name="canvas"/>'s device pixels. Call it with the window's context current.</summary>
    private void BindTarget(IWindow window, RenderedCanvasBase canvas)
    {
        var width = Math.Max(1, (int)MathF.Round(canvas.Width * canvas.DpiScale));
        var height = Math.Max(1, (int)MathF.Round(canvas.Height * canvas.DpiScale));
        if (!_targets.TryGetValue(window, out var target))
            _targets[window] = target = new GlOffscreenFramebuffer(width, height);
        target.Bind(width, height);
    }

    private static (float R, float G, float B, float A) Premultiplied((float R, float G, float B, float A) color) =>
        (color.R * color.A, color.G * color.A, color.B * color.A, color.A);

    public void MakeMainContextCurrent() => _mainWindow.MakeContextCurrent();

    public void RequestScreenshot(string path, Action? onComplete = null) => _screenshot.Request(path, onComplete);

    public void RegisterServices(Context context) => context.AddService(_images);

    public void Dispose()
    {
        MakeMainContextCurrent();
        if (_targets.Remove(_mainWindow, out var target))
            target.Dispose();
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

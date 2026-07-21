using PngSharp.Api;
using ZGF.Desktop;
using ZGF.Desktop.Backends.OpenGl;
using ZGF.Fonts;
using ZGF.Gui.OpenGL;
using static GL46;

namespace ZGF.Gui.Desktop.Backends.OpenGl;

internal sealed class GlRenderBackend : IGuiRenderBackend
{
    private readonly GlSharedResources _shared;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;
    // The first canvas created belongs to the main window (backend resolution builds it before any
    // secondary/popup exists); we keep it so MakeMainContextCurrent can restore its GL context.
    private OpenGlWindow? _mainWindow;
    private string? _pendingScreenshotPath;
    private Action? _pendingScreenshotDone;

    public GlRenderBackend(GlSharedResources shared, FreeTypeFontBackend fonts, FontHandle defaultFont)
    {
        _shared = shared;
        _fonts = fonts;
        _defaultFont = defaultFont;
    }

    public RenderedCanvasBase CreateCanvas(IWindow window, int width, int height, RenderedCanvasBase? fontSource)
    {
        var glWindow = (OpenGlWindow)window;
        _mainWindow ??= glWindow;
        glWindow.MakeContextCurrent();
        var canvas = new OpenGlRenderedCanvas(width, height, _fonts, _defaultFont, _shared, window.DpiScale);
        if (fontSource != null)
            canvas.CopyFontsFrom(fontSource);
        return canvas;
    }

    public void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null)
    {
        var glWindow = (OpenGlWindow)window;
        glWindow.RenderFrame = () =>
        {
            preDraw?.Invoke();
            glClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
            glClear(GL_COLOR_BUFFER_BIT);
            canvas.BeginFrame();
            drawContent();
            canvas.EndFrame();

            // Read back here — after the frame is drawn, before the window swaps buffers — so the
            // back buffer still holds this frame's content.
            if (_pendingScreenshotPath is { } path && canvas is OpenGlRenderedCanvas glCanvas)
            {
                _pendingScreenshotPath = null;
                var done = _pendingScreenshotDone;
                _pendingScreenshotDone = null;
                try
                {
                    var rgba = glCanvas.ReadFramebufferRgba(out var w, out var h);
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    Png.EncodeToFile(Png.CreateRgba(w, h, rgba), path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Screenshot] failed: {ex.Message}");
                }
                finally
                {
                    done?.Invoke();
                }
            }
        };
    }

    public void OnFramebufferResize(int width, int height) => glViewport(0, 0, width, height);

    public void RenderWindowNow(IWindow window)
    {
        var glWindow = (OpenGlWindow)window;
        glWindow.MakeContextCurrent();
        glWindow.RenderNow();
    }

    public void MakeWindowContextCurrent(IWindow window) => ((OpenGlWindow)window).MakeContextCurrent();

    public void MakeMainContextCurrent() => _mainWindow?.MakeContextCurrent();

    public void RequestScreenshot(string path, Action? onComplete = null)
    {
        _pendingScreenshotPath = path;
        _pendingScreenshotDone = onComplete;
    }

    public void Dispose() => _shared.Dispose();
}

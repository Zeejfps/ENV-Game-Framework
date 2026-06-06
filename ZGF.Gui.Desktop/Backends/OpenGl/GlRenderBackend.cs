using ZGF.Desktop;
using ZGF.Desktop.Backends.OpenGl;
using ZGF.Fonts;
using static GL46;

namespace ZGF.Gui.Desktop.Backends.OpenGl;

internal sealed class GlRenderBackend : IGuiRenderBackend
{
    private readonly GlSharedResources _shared;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;

    public GlRenderBackend(GlSharedResources shared, FreeTypeFontBackend fonts, FontHandle defaultFont)
    {
        _shared = shared;
        _fonts = fonts;
        _defaultFont = defaultFont;
    }

    public RenderedCanvasBase CreateCanvas(IWindow window, int width, int height, RenderedCanvasBase? fontSource)
    {
        window.MakeContextCurrent();
        var canvas = new OpenGlRenderedCanvas(width, height, _fonts, _defaultFont, _shared, window.DpiScale);
        if (fontSource != null)
            canvas.CopyFontsFrom(fontSource);
        return canvas;
    }

    public void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor)
    {
        var glWindow = (OpenGlWindow)window;
        glWindow.RenderFrame = () =>
        {
            glClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
            glClear(GL_COLOR_BUFFER_BIT);
            canvas.BeginFrame();
            drawContent();
            canvas.EndFrame();
        };
    }

    public void Dispose() => _shared.Dispose();
}

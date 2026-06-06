using ZGF.Desktop;
using ZGF.Desktop.Backends.Metal;
using ZGF.Fonts;
using ZGF.Rendering.Metal;

namespace ZGF.Gui;

internal sealed class MetalRenderBackend : IGuiRenderBackend
{
    private readonly MetalSharedResources _shared;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;

    public MetalRenderBackend(MetalSharedResources shared, FreeTypeFontBackend fonts, FontHandle defaultFont)
    {
        _shared = shared;
        _fonts = fonts;
        _defaultFont = defaultFont;
    }

    public RenderedCanvasBase CreateCanvas(IWindow window, int width, int height, RenderedCanvasBase? fontSource)
    {
        var canvas = new MetalRenderedCanvas(width, height, _fonts, _defaultFont, _shared, window.DpiScale);
        if (fontSource != null)
            canvas.CopyFontsFrom(fontSource);
        return canvas;
    }

    public void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor)
    {
        var metalWindow = (MetalWindow)window;
        var metalCanvas = (MetalRenderedCanvas)canvas;
        var surfaceRenderer = new MetalSurfaceRenderer(metalWindow);
        metalWindow.RenderFrame = () => surfaceRenderer.RenderFrame((encoder, commandBuffer) =>
        {
            canvas.BeginFrame();
            drawContent();
            metalCanvas.EndFrame(encoder, commandBuffer);
        });
    }

    public void Dispose() => _shared.Dispose();
}

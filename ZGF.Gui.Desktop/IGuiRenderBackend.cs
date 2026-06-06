using ZGF.Desktop;

namespace ZGF.Gui;

internal interface IGuiRenderBackend : IDisposable
{
    RenderedCanvasBase CreateCanvas(IWindow window, int width, int height, RenderedCanvasBase? fontSource);

    void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor);
}

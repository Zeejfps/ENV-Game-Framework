using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

internal interface IGuiRenderBackend : IDisposable
{
    RenderedCanvasBase CreateCanvas(IWindow window, int width, int height, RenderedCanvasBase? fontSource);

    /// <param name="preDraw">Optional pass that runs at the start of every frame, with the
    /// window's graphics context current, before the GUI clear/draw — the seam for embedding
    /// the GUI over engine-rendered content.</param>
    void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null);
}

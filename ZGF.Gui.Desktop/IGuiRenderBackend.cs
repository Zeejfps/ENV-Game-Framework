using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

internal interface IGuiRenderBackend : IDisposable
{
    RenderedCanvasBase CreateCanvas(IWindow window, int width, int height, RenderedCanvasBase? fontSource);

    /// <param name="preDraw">Optional pass that runs at the start of every frame, with the
    /// window's graphics context current, before the GUI clear/draw — the seam for embedding
    /// the GUI over engine-rendered content.</param>
    void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null);

    /// <summary>Requests that the next rendered frame be written to <paramref name="path"/> as a PNG.
    /// Captured inside the render loop (after draw, before swap) where the backend supports CPU
    /// read-back. <paramref name="onComplete"/> runs once the capture attempt finishes (success or
    /// failure), on the render thread — used by the debug server to know when the file is ready.</summary>
    void RequestScreenshot(string path, Action? onComplete = null);
}

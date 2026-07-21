using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

internal interface IGuiRenderBackend : IDisposable
{
    RenderedCanvasBase CreateCanvas(IWindow window, int width, int height, RenderedCanvasBase? fontSource);

    /// <param name="preDraw">Optional pass that runs at the start of every frame, with the
    /// window's graphics context current, before the GUI clear/draw — the seam for embedding
    /// the GUI over engine-rendered content.</param>
    void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null);

    /// <summary>Reacts to the main window's framebuffer changing size — the seam for backend-specific
    /// viewport state (an OpenGL <c>glViewport</c>; a no-op where the swapchain tracks the surface).
    /// Keeps GL knowledge out of the host, which only forwards the resize.</summary>
    void OnFramebufferResize(int width, int height);

    /// <summary>Renders one frame of <paramref name="window"/> synchronously, making whatever graphics
    /// context it needs current first — for off-loop repaints (a live resize, a popup/secondary first
    /// paint before Show, a screenshot). The host never touches per-window context state itself.</summary>
    void RenderWindowNow(IWindow window);

    /// <summary>Makes <paramref name="window"/>'s graphics context current — for GL object work outside
    /// a render (deleting a window's per-context VAOs before disposing it). No-op where the backend
    /// has no per-thread current context (Metal).</summary>
    void MakeWindowContextCurrent(IWindow window);

    /// <summary>Restores the main window's graphics context as current — after a secondary/popup
    /// window rendered or was disposed, and for app-side GL resource work (texture/icon uploads).
    /// No-op on backends without a per-thread current context.</summary>
    void MakeMainContextCurrent();

    /// <summary>Requests that the next rendered frame be written to <paramref name="path"/> as a PNG.
    /// Captured inside the render loop (after draw, before swap) where the backend supports CPU
    /// read-back. <paramref name="onComplete"/> runs once the capture attempt finishes (success or
    /// failure), on the render thread — used by the debug server to know when the file is ready.</summary>
    void RequestScreenshot(string path, Action? onComplete = null);
}

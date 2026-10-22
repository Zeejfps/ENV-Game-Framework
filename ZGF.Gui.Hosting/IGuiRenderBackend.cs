using ZGF.Desktop;
using ZGF.Fonts;

namespace ZGF.Gui.Hosting;

/// <summary>The windowed app and the graphics device the GUI draws with: the fonts every canvas
/// shares, the main window's canvas, and the render loop of each window.</summary>
internal interface IGuiRenderBackend : IDisposable
{
    IWindowedApp App { get; }
    FreeTypeFontBackend Fonts { get; }
    FontHandle DefaultFont { get; }
    RenderedCanvasBase MainCanvas { get; }

    /// <summary>A canvas for <paramref name="window"/> that draws with the main canvas's fonts.</summary>
    RenderedCanvasBase CreateCanvas(IWindow window, int width, int height);

    /// <param name="preDraw">Optional pass that runs at the start of every frame, with the
    /// window's graphics context current, before the GUI clear/draw — the seam for embedding
    /// the GUI over engine-rendered content.</param>
    void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null);

    /// <summary>Renders <paramref name="window"/> by clearing it to transparent and presenting what
    /// was last drawn on <paramref name="canvas"/>, for content drawn outside the window's own frame.</summary>
    void WirePresentLoop(IWindow window, RenderedCanvasBase canvas);

    /// <summary>Reacts to the main window's framebuffer changing size — the seam for backend-specific
    /// viewport state (an OpenGL <c>glViewport</c>; a no-op where the swapchain tracks the surface).</summary>
    void OnFramebufferResize(int width, int height);

    /// <summary>Renders one frame of <paramref name="window"/> synchronously, then restores the main
    /// window's context — for off-loop repaints (a live resize, a first paint before Show, a screenshot).</summary>
    void RenderWindowNow(IWindow window);

    /// <summary>Deletes <paramref name="canvas"/>'s GPU objects under its own window's context, then
    /// destroys the window and restores the main window's context.</summary>
    void DisposeWindow(IWindow window, RenderedCanvasBase canvas);

    /// <summary>Restores the main window's graphics context as current — for app-side GL resource work
    /// (texture/icon uploads). No-op on backends without a per-thread current context.</summary>
    void MakeMainContextCurrent();

    /// <summary>Requests that the next rendered frame be written to <paramref name="path"/> as a PNG.
    /// Captured inside the render loop (after draw, before swap) where the backend supports CPU
    /// read-back. <paramref name="onComplete"/> runs once the capture attempt finishes (success or
    /// failure), on the render thread — used by the debug server to know when the file is ready.</summary>
    void RequestScreenshot(string path, Action? onComplete = null);

    /// <summary>Registers the backend's image manager, for frame-buffer-backed images.</summary>
    void RegisterServices(Context context);

    static IGuiRenderBackend CreateForPlatform(StartupConfig config)
    {
        if (OperatingSystem.IsMacOS())
            return new MetalRenderBackend(config);
        return new GlRenderBackend(config);
    }
}

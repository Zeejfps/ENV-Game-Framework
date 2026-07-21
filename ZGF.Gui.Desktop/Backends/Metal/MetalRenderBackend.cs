using PngSharp.Api;
using ZGF.Desktop;
using ZGF.Desktop.Backends.Metal;
using ZGF.Fonts;
using ZGF.Gui.Metal;
using ZGF.Rendering.Metal;

namespace ZGF.Gui.Desktop.Backends.Metal;

internal sealed class MetalRenderBackend : IGuiRenderBackend
{
    private readonly MetalSharedResources _shared;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;
    private MetalSurfaceRenderer? _surfaceRenderer;
    private string? _pendingScreenshotPath;
    private Action? _pendingScreenshotDone;

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

    public void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null)
    {
        var metalWindow = (MetalWindow)window;
        var metalCanvas = (MetalRenderedCanvas)canvas;
        var surfaceRenderer = new MetalSurfaceRenderer(metalWindow);
        _surfaceRenderer = surfaceRenderer;
        metalWindow.RenderFrame = () =>
        {
            surfaceRenderer.RenderFrame((encoder, commandBuffer) =>
            {
                preDraw?.Invoke();
                canvas.BeginFrame();
                drawContent();
                metalCanvas.EndFrame(encoder, commandBuffer);
            });

            if (_pendingScreenshotPath is { } path)
            {
                _pendingScreenshotPath = null;
                var done = _pendingScreenshotDone;
                _pendingScreenshotDone = null;
                try
                {
                    if (surfaceRenderer.TryTakeCapture(out var w, out var h, out var rgba))
                    {
                        var dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        Png.EncodeToFile(Png.CreateRgba(w, h, rgba), path);
                    }
                    else
                    {
                        Console.WriteLine("[Screenshot] no captured frame was produced.");
                    }
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

    public void RequestScreenshot(string path, Action? onComplete = null)
    {
        _pendingScreenshotPath = path;
        _pendingScreenshotDone = onComplete;
        _surfaceRenderer?.RequestCapture();
    }

    // No-op: the Metal layer's drawable tracks the window surface, so there's no viewport to reset.
    public void OnFramebufferResize(int width, int height) { }

    public void RenderWindowNow(IWindow window) => ((MetalWindow)window).RenderNow();

    // Metal has no per-thread current context to switch — each frame binds its own drawable.
    public void MakeWindowContextCurrent(IWindow window) { }
    public void MakeMainContextCurrent() { }

    public void Dispose() => _shared.Dispose();
}

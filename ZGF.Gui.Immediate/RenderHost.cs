using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.Desktop.Backends.Metal;
using ZGF.Desktop.Backends.OpenGl;
using ZGF.Fonts;
using ZGF.Gui.Metal;
using ZGF.Gui.OpenGL;
using ZGF.Rendering.Metal;
using static GL46;

namespace ZGF.Gui.Immediate;

public enum RenderBackendKind
{
    Auto,
    OpenGl,
    Metal,
}

internal abstract class RenderHost : IDisposable
{
    public abstract IWindowedApp App { get; }
    public abstract RenderedCanvasBase Canvas { get; }

    public abstract void SetFrame(Action draw, uint clearColor);

    public abstract void RenderNow();

    public abstract void Dispose();

    public static RenderHost Create(StartupConfig config, RenderBackendKind kind) => kind switch
    {
        RenderBackendKind.OpenGl => new GlRenderHost(config),
        RenderBackendKind.Metal => new MetalRenderHost(config),
        _ => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MetalRenderHost(config) : new GlRenderHost(config),
    };

    protected static FontHandle LoadDefaultFont(FreeTypeFontBackend fonts, float dpiScale) =>
        fonts.LoadFontFromMemory(EmbeddedAssets.LoadFontBytes("Inter-Regular.ttf"), (int)MathF.Round(16 * dpiScale));

    protected static (float R, float G, float B, float A) Unpack(uint argb) => (
        ((argb >> 16) & 0xFF) / 255f,
        ((argb >> 8) & 0xFF) / 255f,
        (argb & 0xFF) / 255f,
        ((argb >> 24) & 0xFF) / 255f);
}

internal sealed class GlRenderHost : RenderHost
{
    private readonly OpenGlApp _app;
    private readonly OpenGlWindow _window;
    private readonly GlSharedResources _shared;
    private readonly OpenGlRenderedCanvas _canvas;

    public GlRenderHost(StartupConfig config)
    {
        _app = new OpenGlApp(config);
        _window = (OpenGlWindow)_app.MainWindow;
        var fonts = new FreeTypeFontBackend();
        var font = LoadDefaultFont(fonts, _window.DpiScale);
        _shared = new GlSharedResources(fonts, new GlImageManager());
        _canvas = new OpenGlRenderedCanvas(config.WindowWidth, config.WindowHeight, fonts, font, _shared, _window.DpiScale);
    }

    public override IWindowedApp App => _app;
    public override RenderedCanvasBase Canvas => _canvas;

    public override void SetFrame(Action draw, uint clearColor)
    {
        var (r, g, b, a) = Unpack(clearColor);
        _window.RenderFrame = () =>
        {
            glClearColor(r, g, b, a);
            glClear(GL_COLOR_BUFFER_BIT);
            _canvas.BeginFrame();
            draw();
            _canvas.EndFrame();
        };
    }

    public override void RenderNow()
    {
        _window.MakeContextCurrent();
        _window.RenderNow();
    }

    public override void Dispose()
    {
        _window.MakeContextCurrent();
        _canvas.Dispose();
        _shared.Dispose();
        _app.Dispose();
    }
}

internal sealed class MetalRenderHost : RenderHost
{
    private readonly MetalApp _app;
    private readonly MetalWindow _window;
    private readonly MetalSharedResources _shared;
    private readonly MetalRenderedCanvas _canvas;

    public MetalRenderHost(StartupConfig config)
    {
        _app = new MetalApp(config);
        _window = (MetalWindow)_app.MainWindow;
        var fonts = new FreeTypeFontBackend();
        var font = LoadDefaultFont(fonts, _window.DpiScale);
        _shared = new MetalSharedResources(_app.Device, _app.CommandQueue, fonts, new MetalImageManager(_app.Device));
        _canvas = new MetalRenderedCanvas(config.WindowWidth, config.WindowHeight, fonts, font, _shared, _window.DpiScale);
    }

    public override IWindowedApp App => _app;
    public override RenderedCanvasBase Canvas => _canvas;

    public override void SetFrame(Action draw, uint clearColor)
    {
        var (r, g, b, a) = Unpack(clearColor);
        var surface = new MetalSurfaceRenderer(_window, new MTLClearColor(r, g, b, a));
        _window.RenderFrame = () => surface.RenderFrame((encoder, commandBuffer) =>
        {
            _canvas.BeginFrame();
            draw();
            _canvas.EndFrame(encoder, commandBuffer);
        });
    }

    public override void RenderNow() => _window.RenderNow();

    public override void Dispose()
    {
        _canvas.Dispose();
        _shared.Dispose();
        _app.Dispose();
    }
}

using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.Desktop.Backends;
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

/// <summary>A borderless window for a popup and the canvas it is drawn on. Rendering it presents
/// whatever was last drawn on the canvas.</summary>
internal sealed record PopupSurface(IWindow Window, RenderedCanvasBase Canvas);

internal abstract class RenderHost : IDisposable
{
    private const int DefaultFontPixels = 16;
    private const int PopupInitialSize = 32;

    private readonly FreeTypeFontBackend _fonts;

    protected RenderHost(FreeTypeFontBackend fonts)
    {
        _fonts = fonts;
    }

    public abstract IWindowedApp App { get; }
    public abstract RenderedCanvasBase Canvas { get; }

    public abstract void SetFrame(Action draw, uint clearColor);

    public abstract void RenderNow();

    public abstract void Dispose();

    /// <summary>Makes a popup window with a canvas sharing the main canvas's fonts.</summary>
    public PopupSurface CreatePopup(bool passThrough)
    {
        var window = App.CreatePopupWindow(new PopupWindowOptions
        {
            WidthPoints = PopupInitialSize,
            HeightPoints = PopupInitialSize,
            OwnerWindow = App.MainWindow,
            MousePassThrough = passThrough,
        });
        var surface = new PopupSurface(window, CreatePopupCanvas(window, PopupInitialSize, PopupInitialSize));
        surface.Canvas.CopyFontsFrom(Canvas);
        MakeMainCurrent();
        return surface;
    }

    /// <summary>Sizes a popup's canvas, in UI points, and its scale.</summary>
    public void ResizePopup(PopupSurface popup, int width, int height, float dpiScale)
    {
        var canvas = popup.Canvas;
        if (canvas.Width == width && canvas.Height == height && canvas.DpiScale == dpiScale) return;
        MakeCurrent(popup.Window);
        canvas.UpdateDpiScale(dpiScale);
        canvas.Resize(width, height);
        MakeMainCurrent();
    }

    public void RenderPopup(PopupSurface popup)
    {
        MakeCurrent(popup.Window);
        if (popup.Window is GlfwWindowBase window)
            window.RenderNow();
        MakeMainCurrent();
    }

    public void DisposePopup(PopupSurface popup)
    {
        MakeCurrent(popup.Window);
        (popup.Canvas as IDisposable)?.Dispose();
        popup.Window.Dispose();
        MakeMainCurrent();
    }

    public void RegisterFont(string family, byte[] data) =>
        Canvas.RegisterFont(family, _fonts.LoadFontFromMemory(data, PixelSize(Canvas.DpiScale)));

    protected abstract RenderedCanvasBase CreatePopupCanvas(IWindow window, int width, int height);

    protected abstract void MakeCurrent(IWindow window);

    protected abstract void MakeMainCurrent();

    public static RenderHost Create(StartupConfig config, RenderBackendKind kind) => kind switch
    {
        RenderBackendKind.OpenGl => new GlRenderHost(config),
        RenderBackendKind.Metal => new MetalRenderHost(config),
        _ => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MetalRenderHost(config) : new GlRenderHost(config),
    };

    protected static FontHandle LoadDefaultFont(FreeTypeFontBackend fonts, float dpiScale) =>
        fonts.LoadFontFromMemory(EmbeddedAssets.LoadFontBytes("Inter-Regular.ttf"), PixelSize(dpiScale));

    private static int PixelSize(float dpiScale) => Math.Max(1, (int)MathF.Round(DefaultFontPixels * dpiScale));

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
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _font;
    private readonly GlSharedResources _shared;
    private readonly OpenGlRenderedCanvas _canvas;

    public GlRenderHost(StartupConfig config) : this(config, new FreeTypeFontBackend())
    {
    }

    private GlRenderHost(StartupConfig config, FreeTypeFontBackend fonts) : base(fonts)
    {
        _app = new OpenGlApp(config);
        _window = (OpenGlWindow)_app.MainWindow;
        _fonts = fonts;
        _font = LoadDefaultFont(fonts, _window.DpiScale);
        _shared = new GlSharedResources(fonts, new GlImageManager());
        _canvas = new OpenGlRenderedCanvas(config.WindowWidth, config.WindowHeight, fonts, _font, _shared, _window.DpiScale);
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

    protected override RenderedCanvasBase CreatePopupCanvas(IWindow window, int width, int height)
    {
        var glWindow = (OpenGlWindow)window;
        glWindow.MakeContextCurrent();
        var canvas = new OpenGlRenderedCanvas(width, height, _fonts, _font, _shared, _canvas.DpiScale);
        glWindow.RenderFrame = () =>
        {
            glViewport(0, 0, glWindow.FramebufferWidth, glWindow.FramebufferHeight);
            glClearColor(0f, 0f, 0f, 0f);
            glClear(GL_COLOR_BUFFER_BIT);
            canvas.EndFrame();
        };
        return canvas;
    }

    protected override void MakeCurrent(IWindow window) => ((OpenGlWindow)window).MakeContextCurrent();

    protected override void MakeMainCurrent() => _window.MakeContextCurrent();

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
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _font;
    private readonly MetalSharedResources _shared;
    private readonly MetalRenderedCanvas _canvas;

    public MetalRenderHost(StartupConfig config) : this(config, new FreeTypeFontBackend())
    {
    }

    private MetalRenderHost(StartupConfig config, FreeTypeFontBackend fonts) : base(fonts)
    {
        _app = new MetalApp(config);
        _window = (MetalWindow)_app.MainWindow;
        _fonts = fonts;
        _font = LoadDefaultFont(fonts, _window.DpiScale);
        _shared = new MetalSharedResources(_app.Device, _app.CommandQueue, fonts, new MetalImageManager(_app.Device));
        _canvas = new MetalRenderedCanvas(config.WindowWidth, config.WindowHeight, fonts, _font, _shared, _window.DpiScale);
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

    protected override RenderedCanvasBase CreatePopupCanvas(IWindow window, int width, int height)
    {
        var metalWindow = (MetalWindow)window;
        var canvas = new MetalRenderedCanvas(width, height, _fonts, _font, _shared, _canvas.DpiScale);
        var surface = new MetalSurfaceRenderer(metalWindow, new MTLClearColor(0, 0, 0, 0));
        metalWindow.RenderFrame = () => surface.RenderFrame((encoder, commandBuffer) => canvas.EndFrame(encoder, commandBuffer));
        return canvas;
    }

    protected override void MakeCurrent(IWindow window) { }

    protected override void MakeMainCurrent() { }

    public override void Dispose()
    {
        _canvas.Dispose();
        _shared.Dispose();
        _app.Dispose();
    }
}

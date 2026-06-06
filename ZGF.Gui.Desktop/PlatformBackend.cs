// FontHandle is in ZGF.Fonts namespace.
using System.Runtime.InteropServices;
using ZGF.Core;
using ZGF.Fonts;
using ZGF.Rendering.Metal;
using static GL46;

namespace ZGF.Gui;

internal static class PlatformBackend
{
    public readonly struct Backend
    {
        public required IApp App { get; init; }
        public required RenderedCanvasBase MainCanvas { get; init; }
        public required FreeTypeFontBackend FontBackend { get; init; }
        public required FontHandle DefaultFont { get; init; }
        public required GlSharedResources? GlShared { get; init; }
        public required MetalSharedResources? MetalShared { get; init; }
    }

    public static Backend Resolve(StartupConfig config)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return ResolveMetal(config);
        return ResolveOpenGl(config);
    }

    private static Backend ResolveOpenGl(StartupConfig config)
    {
        var app = new OpenGlApp(config);
        var mainWindow = (OpenGlWindow)app.MainWindow;
        var dpiScale = mainWindow.DpiScale;

        var fonts = new FreeTypeFontBackend();
        var defaultFont = fonts.LoadFontFromMemory(EmbeddedAssets.LoadFontBytes("Inter-Regular.ttf"), (int)MathF.Round(16 * dpiScale));
        var imageManager = new GlImageManager();
        var shared = new GlSharedResources(fonts, imageManager);
        var windowWidth = config.WindowWidth;
        if (windowWidth <= 0) windowWidth = 1280;
        var windowHeight = config.WindowHeight;
        if (windowHeight <= 0) windowHeight = 720;

        var canvas = new OpenGlRenderedCanvas(
            windowWidth, windowHeight, fonts, defaultFont, shared, dpiScale);

        glClearColor(0, 0, 0, 0);

        mainWindow.RenderFrame = () =>
        {
            glClear(GL_COLOR_BUFFER_BIT);
            canvas.BeginFrame();
            // Population happens via GuiApp wiring; PopulateMain is set by GuiApp.
            PopulateMain?.Invoke();
            canvas.EndFrame();
        };

        return new Backend
        {
            App = app,
            MainCanvas = canvas,
            FontBackend = fonts,
            DefaultFont = defaultFont,
            GlShared = shared,
            MetalShared = null,
        };
    }

    private static Backend ResolveMetal(StartupConfig config)
    {
        var app = new MetalApp(config);
        var mainWindow = (MetalWindow)app.MainWindow;
        var dpiScale = mainWindow.DpiScale;

        var fonts = new FreeTypeFontBackend();
        var defaultFont = fonts.LoadFontFromMemory(EmbeddedAssets.LoadFontBytes("Inter-Regular.ttf"), (int)MathF.Round(16 * dpiScale));
        var imageManager = new MetalImageManager(app.Device);
        var shared = new MetalSharedResources(app.Device, app.CommandQueue, fonts, imageManager);
        var canvas = new MetalRenderedCanvas(
            config.WindowWidth, config.WindowHeight, fonts, defaultFont, shared, dpiScale);

        // mainWindow is an IMetalSurface (CAMetalLayer on the GLFW NSWindow). The per-frame
        // drawable/encoder/present loop is host-agnostic and lives in MetalSurfaceRenderer,
        // so an iOS host can reuse it by supplying its own IMetalSurface.
        var surfaceRenderer = new MetalSurfaceRenderer(mainWindow);

        mainWindow.RenderFrame = () => surfaceRenderer.RenderFrame((encoder, commandBuffer) =>
        {
            canvas.BeginFrame();
            PopulateMain?.Invoke();
            canvas.EndFrame(encoder, commandBuffer);
        });

        return new Backend
        {
            App = app,
            MainCanvas = canvas,
            FontBackend = fonts,
            DefaultFont = defaultFont,
            GlShared = null,
            MetalShared = shared,
        };
    }

    // Wired by GuiApp: the main-window draw callback that fills the canvas.
    internal static Action? PopulateMain;
}

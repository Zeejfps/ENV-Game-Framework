using System.Runtime.InteropServices;
using ZGF.Core;
using ZGF.Fonts;

namespace ZGF.Gui;

internal static class PlatformBackend
{
    internal readonly struct Backend
    {
        public required IApp App { get; init; }
        public required RenderedCanvasBase MainCanvas { get; init; }
        public required FreeTypeFontBackend FontBackend { get; init; }
        public required FontHandle DefaultFont { get; init; }
        public required IGuiRenderBackend RenderBackend { get; init; }
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
        var backend = new GlRenderBackend(shared, fonts, defaultFont);

        var windowWidth = config.WindowWidth;
        if (windowWidth <= 0) windowWidth = 1280;
        var windowHeight = config.WindowHeight;
        if (windowHeight <= 0) windowHeight = 720;

        var canvas = backend.CreateCanvas(mainWindow, windowWidth, windowHeight, fontSource: null);
        backend.WireRenderLoop(mainWindow, canvas, () => PopulateMain?.Invoke(), (0f, 0f, 0f, 0f));

        return new Backend
        {
            App = app,
            MainCanvas = canvas,
            FontBackend = fonts,
            DefaultFont = defaultFont,
            RenderBackend = backend,
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
        var backend = new MetalRenderBackend(shared, fonts, defaultFont);

        var canvas = backend.CreateCanvas(mainWindow, config.WindowWidth, config.WindowHeight, fontSource: null);
        backend.WireRenderLoop(mainWindow, canvas, () => PopulateMain?.Invoke(), (0f, 0f, 0f, 0f));

        return new Backend
        {
            App = app,
            MainCanvas = canvas,
            FontBackend = fonts,
            DefaultFont = defaultFont,
            RenderBackend = backend,
        };
    }

    // Wired by GuiApp: the main-window draw callback that fills the canvas.
    internal static Action? PopulateMain;
}

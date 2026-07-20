using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.Desktop.Backends.Metal;
using ZGF.Desktop.Backends.OpenGl;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Backends.Metal;
using ZGF.Gui.Desktop.Backends.OpenGl;
using ZGF.Gui.Metal;
using ZGF.Gui.OpenGL;

namespace ZGF.Gui.Desktop;

internal static class PlatformBackend
{
    internal readonly struct Backend
    {
        public required IWindowedApp App { get; init; }
        public required RenderedCanvasBase MainCanvas { get; init; }
        public required FreeTypeFontBackend FontBackend { get; init; }
        public required FontHandle DefaultFont { get; init; }
        public required IGuiRenderBackend RenderBackend { get; init; }

        /// <summary>Registers backend-specific resources (e.g. the image manager, for
        /// frame-buffer-backed images) into the app context so embedded-rendering apps can
        /// resolve them at build time.</summary>
        public required Action<Context> RegisterServices { get; init; }
    }

    public static Backend Resolve(StartupConfig config, GuiRenderBackendKind kind, Action? mainPreDraw)
    {
        return kind switch
        {
            GuiRenderBackendKind.OpenGl => ResolveOpenGl(config, mainPreDraw),
            GuiRenderBackendKind.Metal => ResolveMetal(config, mainPreDraw),
            _ => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? ResolveMetal(config, mainPreDraw)
                : ResolveOpenGl(config, mainPreDraw),
        };
    }

    private static Backend ResolveOpenGl(StartupConfig config, Action? mainPreDraw)
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
        backend.WireRenderLoop(mainWindow, canvas, () => PopulateMain?.Invoke(), (0f, 0f, 0f, 0f), mainPreDraw);

        return new Backend
        {
            App = app,
            MainCanvas = canvas,
            FontBackend = fonts,
            DefaultFont = defaultFont,
            RenderBackend = backend,
            RegisterServices = ctx => ctx.AddService(imageManager),
        };
    }

    private static Backend ResolveMetal(StartupConfig config, Action? mainPreDraw)
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
        backend.WireRenderLoop(mainWindow, canvas, () => PopulateMain?.Invoke(), (0f, 0f, 0f, 0f), mainPreDraw);

        return new Backend
        {
            App = app,
            MainCanvas = canvas,
            FontBackend = fonts,
            DefaultFont = defaultFont,
            RenderBackend = backend,
            RegisterServices = ctx => ctx.AddService(imageManager),
        };
    }

    // Wired by GuiApp: the main-window draw callback that fills the canvas.
    internal static Action? PopulateMain;
}

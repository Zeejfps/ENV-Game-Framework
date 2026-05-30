// FontHandle is in ZGF.Fonts namespace.
using System.Runtime.InteropServices;
using ZGF.Core;
using ZGF.Fonts;
using static GL46;
using static ZGF.Core.MacOs.Objc;

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

        var layer = mainWindow.Layer;
        var queue = app.CommandQueue;
        var nextDrawableSel = Sel("nextDrawable");
        var commandBufferSel = Sel("commandBuffer");
        var renderCommandEncoderSel = Sel("renderCommandEncoderWithDescriptor:");
        var endEncodingSel = Sel("endEncoding");
        var presentDrawableSel = Sel("presentDrawable:");
        var commitSel = Sel("commit");
        var textureSel = Sel("texture");

        mainWindow.RenderFrame = () =>
        {
            var drawable = msg_IntPtr(layer, nextDrawableSel);
            if (drawable == IntPtr.Zero) return;

            var commandBuffer = msg_IntPtr(queue, commandBufferSel);
            var passDescriptor = BuildRenderPassDescriptor(msg_IntPtr(drawable, textureSel));
            var encoder = msg_IntPtr(commandBuffer, renderCommandEncoderSel, passDescriptor);

            canvas.BeginFrame();
            PopulateMain?.Invoke();
            canvas.EndFrame(encoder, commandBuffer);

            msg_Void(encoder, endEncodingSel);
            msg_Void_IntPtr(commandBuffer, presentDrawableSel, drawable);
            msg_Void(commandBuffer, commitSel);

            Release(passDescriptor);
        };

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

    private static IntPtr BuildRenderPassDescriptor(IntPtr drawableTexture)
    {
        var descClass = Class("MTLRenderPassDescriptor");
        var desc = msg_IntPtr(descClass, Sel("renderPassDescriptor"));
        Retain(desc);

        var colorAttachments = msg_IntPtr(desc, Sel("colorAttachments"));
        var color0 = msg_IntPtr_NUInt_NUInt(colorAttachments, Sel("objectAtIndexedSubscript:"), 0, 0);
        msg_Void_IntPtr(color0, Sel("setTexture:"), drawableTexture);
        msg_Void_UInt(color0, Sel("setLoadAction:"), 2);
        msg_Void_UInt(color0, Sel("setStoreAction:"), 1);
        SetClearColor(color0, Sel("setClearColor:"), new ZGF.Core.MacOs.MTLClearColor(0, 0, 0, 1));
        return desc;
    }

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void SetClearColor(IntPtr receiver, IntPtr selector, ZGF.Core.MacOs.MTLClearColor color);
}

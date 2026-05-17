using System.Runtime.InteropServices;
using ZGF.AppUtils;
using ZGF.Core;
using ZGF.Fonts;
using ZGF.Gui;
using ZGF.Gui.Tests;
using static GL46;
using static ZGF.Core.MacOs.Objc;

namespace LLMit;

internal static class PlatformBackend
{
    public readonly struct Backend
    {
        public required IWindowApp Window { get; init; }
        public required RenderedCanvasBase Canvas { get; init; }
        public required FreeTypeFontBackend FontBackend { get; init; }
        public required Action<Action> RenderFrame { get; init; }
    }

    public static Backend Resolve(StartupConfig config)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return ResolveMetal(config);
        return ResolveOpenGl(config);
    }

    private static Backend ResolveOpenGl(StartupConfig config)
    {
        var window = new OpenGlApp(config);

        var fontFilePath = PathUtils.ResolveLocalPath("Assets/Fonts/Inter/Inter-Regular.ttf");
        var fonts = new FreeTypeFontBackend();
        var defaultFont = fonts.LoadFontFromFile(fontFilePath, 16);
        var imageManager = new GlImageManager();
        var canvas = new OpenGlRenderedCanvas(
            config.WindowWidth,
            config.WindowHeight,
            fonts,
            defaultFont,
            imageManager);

        glClearColor(0, 0, 0, 0);

        Action<Action> renderFrame = populate =>
        {
            glClear(GL_COLOR_BUFFER_BIT);
            canvas.BeginFrame();
            populate();
            canvas.EndFrame();
        };

        return new Backend
        {
            Window = window,
            Canvas = canvas,
            FontBackend = fonts,
            RenderFrame = renderFrame,
        };
    }

    private static Backend ResolveMetal(StartupConfig config)
    {
        var window = new MetalApp(config);

        var fontFilePath = PathUtils.ResolveLocalPath("Assets/Fonts/Inter/Inter-Regular.ttf");
        var fonts = new FreeTypeFontBackend();
        var defaultFont = fonts.LoadFontFromFile(fontFilePath, 16);
        var imageManager = new MetalImageManager(window.Device);
        var canvas = new MetalRenderedCanvas(
            config.WindowWidth,
            config.WindowHeight,
            fonts,
            defaultFont,
            imageManager,
            window.Device);

        var layer = window.Layer;
        var queue = window.CommandQueue;
        var nextDrawableSel = Sel("nextDrawable");
        var commandBufferSel = Sel("commandBuffer");
        var renderCommandEncoderSel = Sel("renderCommandEncoderWithDescriptor:");
        var endEncodingSel = Sel("endEncoding");
        var presentDrawableSel = Sel("presentDrawable:");
        var commitSel = Sel("commit");
        var textureSel = Sel("texture");

        Action<Action> renderFrame = populate =>
        {
            var drawable = msg_IntPtr(layer, nextDrawableSel);
            if (drawable == IntPtr.Zero) return;

            var commandBuffer = msg_IntPtr(queue, commandBufferSel);
            var passDescriptor = BuildRenderPassDescriptor(msg_IntPtr(drawable, textureSel));

            var encoder = msg_IntPtr(commandBuffer, renderCommandEncoderSel, passDescriptor);

            canvas.BeginFrame();
            populate();
            canvas.EndFrame(encoder, commandBuffer);

            msg_Void(encoder, endEncodingSel);
            msg_Void_IntPtr(commandBuffer, presentDrawableSel, drawable);
            msg_Void(commandBuffer, commitSel);

            Release(passDescriptor);
        };

        return new Backend
        {
            Window = window,
            Canvas = canvas,
            FontBackend = fonts,
            RenderFrame = renderFrame,
        };
    }

    private static IntPtr BuildRenderPassDescriptor(IntPtr drawableTexture)
    {
        var descClass = Class("MTLRenderPassDescriptor");
        var desc = msg_IntPtr(descClass, Sel("renderPassDescriptor"));
        Retain(desc); // returned autoreleased; we'll release after commit.

        var colorAttachments = msg_IntPtr(desc, Sel("colorAttachments"));
        var color0 = msg_IntPtr_NUInt_NUInt(colorAttachments, Sel("objectAtIndexedSubscript:"), 0, 0);
        msg_Void_IntPtr(color0, Sel("setTexture:"), drawableTexture);
        // 2 = MTLLoadActionClear, 1 = MTLStoreActionStore.
        msg_Void_UInt(color0, Sel("setLoadAction:"), 2);
        msg_Void_UInt(color0, Sel("setStoreAction:"), 1);
        SetClearColor(color0, Sel("setClearColor:"), new ZGF.Core.MacOs.MTLClearColor(0, 0, 0, 1));
        return desc;
    }

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void SetClearColor(IntPtr receiver, IntPtr selector, ZGF.Core.MacOs.MTLClearColor color);
}

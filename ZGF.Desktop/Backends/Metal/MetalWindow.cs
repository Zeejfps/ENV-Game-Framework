using GLFW;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;
using Exception = System.Exception;

namespace ZGF.Desktop.Backends.Metal;

public sealed class MetalWindow : GlfwWindowBase, IMetalSurface
{
    public IntPtr Layer { get; }
    public IntPtr NsWindow { get; }

    // IMetalSurface: the device/queue this window's CAMetalLayer draws with.
    public IntPtr Device { get; }
    public IntPtr CommandQueue { get; }

    public MetalWindow(Window window, IntPtr device, IntPtr commandQueue, bool isMain)
        : base(window, isMain)
    {
        Device = device;
        CommandQueue = commandQueue;
        NsWindow = Native.GetCocoaWindow(window);

        Glfw.GetFramebufferSize(window, out var fbW, out var fbH);
        DpiScaleValue = ComputeDpiScale();
        Layer = AttachMetalLayer(window, device, fbW, fbH);
    }

    public override IntPtr NativeHandle => NsWindow;

    protected override void Present() { /* Metal presents its drawable inside RenderFrame */ }

    protected override void OnWindowResized(int width, int height)
    {
        Glfw.GetFramebufferSize(GlfwWindow, out var fbW, out var fbH);
        SetDrawableSize(Layer, fbW, fbH);
    }

    protected override void OnFramebufferResized(int width, int height) =>
        SetDrawableSize(Layer, width, height);

    protected override float ComputeDpiScale()
    {
        if (NsWindow != IntPtr.Zero)
        {
            var s = (float)msg_Double(NsWindow, Sel("backingScaleFactor"));
            if (s > 0f) return s;
        }
        Glfw.GetFramebufferSize(GlfwWindow, out var fbW, out var fbH);
        Glfw.GetWindowSize(GlfwWindow, out var winW, out var winH);
        if (winW > 0 && winH > 0)
            return MathF.Max((float)fbW / winW, (float)fbH / winH);
        return 1f;
    }

    private static IntPtr AttachMetalLayer(Window glfwWindow, IntPtr device, int fbWidth, int fbHeight)
    {
        var nsWindow = Native.GetCocoaWindow(glfwWindow);
        if (nsWindow == IntPtr.Zero) throw new Exception("glfwGetCocoaWindow returned null.");

        var contentView = msg_IntPtr(nsWindow, Sel("contentView"));
        if (contentView == IntPtr.Zero) throw new Exception("NSWindow contentView is null.");

        var caMetalLayerClass = Class("CAMetalLayer");
        if (caMetalLayerClass == IntPtr.Zero) throw new Exception("CAMetalLayer class not found.");

        var layer = msg_IntPtr(caMetalLayerClass, Sel("layer"));
        if (layer == IntPtr.Zero) throw new Exception("CAMetalLayer layer factory returned null.");

        msg_Void_IntPtr(layer, Sel("setDevice:"), device);
        msg_Void_UInt(layer, Sel("setPixelFormat:"), (uint)MTLPixelFormat.BGRA8Unorm);
        // false (not the usual true) so the presented drawable can be used as a blit source for
        // CPU framebuffer read-back (GuiApp.CaptureScreenshot / the MCP server's gui_screenshot tool).
        msg_Void_Bool(layer, Sel("setFramebufferOnly:"), false);

        var backingScale = msg_Double(nsWindow, Sel("backingScaleFactor"));
        msg_Void_Double(layer, Sel("setContentsScale:"), backingScale);

        SetDrawableSize(layer, fbWidth, fbHeight);

        msg_Void_IntPtr(contentView, Sel("setLayer:"), layer);
        msg_Void_Bool(contentView, Sel("setWantsLayer:"), true);

        return layer;
    }

    private static void SetDrawableSize(IntPtr layer, int width, int height)
    {
        msg_Void_CGSize(layer, Sel("setDrawableSize:"), new CGSize(width, height));
    }
}

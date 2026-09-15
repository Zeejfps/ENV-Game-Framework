// macOS-only app backed by GLFW (NoApi) + CAMetalLayer attached to the GLFW
// NSWindow's content view. Owns the MTLDevice, MTLCommandQueue, the run loop,
// and the per-window list.

using System.Runtime.InteropServices;
using GLFW;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;
using Monitor = GLFW.Monitor;

namespace ZGF.Desktop.Backends.Metal;

public sealed class MetalApp : GlfwApp<MetalWindow>
{
    private IntPtr _autoreleasePool;

    public IntPtr Device => Main.Device;
    public IntPtr CommandQueue => Main.CommandQueue;

    public MetalApp(StartupConfig startupConfig)
        : base(RequireMacOs(startupConfig), ClientApi.None, CreateMainWindow)
    {
    }

    private static StartupConfig RequireMacOs(StartupConfig startupConfig)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MetalApp requires macOS.");
        return startupConfig;
    }

    private static MetalWindow CreateMainWindow(Window window)
    {
        var device = MetalApi.MTLCreateSystemDefaultDevice();
        if (device == IntPtr.Zero) throw new System.Exception("MTLCreateSystemDefaultDevice returned null.");
        var commandQueue = msg_IntPtr(device, Sel("newCommandQueue"));
        if (commandQueue == IntPtr.Zero) throw new System.Exception("newCommandQueue returned null.");
        return new MetalWindow(window, device, commandQueue, isMain: true);
    }

    protected override MetalWindow OpenWindow(int widthPoints, int heightPoints, string title, bool transparent)
    {
        var glfw = Glfw.CreateWindow(widthPoints, heightPoints, title, Monitor.None, Window.None);
        Glfw.DefaultWindowHints();

        var window = new MetalWindow(glfw, Device, CommandQueue, isMain: false);
        if (transparent)
            window.MakeTransparent();
        return window;
    }

    // Drain every autoreleased Objective-C object this turn creates (NSEvents from
    // PollEvents, per-frame drawables / command buffers / encoders / pass descriptors).
    // Without this pool they leak as unbounded unmanaged growth — see Objc.objc_autoreleasePoolPush.
    protected override void BeginTurn() => _autoreleasePool = objc_autoreleasePoolPush();

    protected override void EndTurn() => objc_autoreleasePoolPop(_autoreleasePool);

    protected override void ReleaseDevice()
    {
        Release(CommandQueue);
        Release(Device);
    }
}

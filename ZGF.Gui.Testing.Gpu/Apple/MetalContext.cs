using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Testing.Gpu.Apple;

/// <summary>The system's default Metal device and a command queue on it, created and released on the
/// render thread.</summary>
[SupportedOSPlatform("macos")]
internal sealed class MetalContext : IDisposable
{
    // Every Apple Silicon Mac is Apple7 or later (M1 is Apple7); Intel and AMD GPUs are only Mac2.
    private const long GpuFamilyApple7 = 1007;

    private bool _disposed;

    private MetalContext(IntPtr device, IntPtr commandQueue, string description)
    {
        Device = device;
        CommandQueue = commandQueue;
        Description = description;
    }

    public IntPtr Device { get; }
    public IntPtr CommandQueue { get; }
    public string Description { get; }

    /// <exception cref="NotSupportedException">There is no Metal device, or it is not an Apple GPU.</exception>
    public static MetalContext Create()
    {
        var device = MetalApi.MTLCreateSystemDefaultDevice();
        if (device == IntPtr.Zero)
            throw new NotSupportedException("MTLCreateSystemDefaultDevice returned no device.");
        try
        {
            var description = $"Metal ({NSString(msg_IntPtr(device, Sel("name")))})";
            if (!msg_Bool_Long(device, Sel("supportsFamily:"), GpuFamilyApple7))
                throw new NotSupportedException(
                    $"{description} is not an Apple-family GPU. The Metal canvas keeps its font atlas and images " +
                    "in shared-storage textures, which only Apple GPUs support, so GpuDevice needs Apple Silicon.");
            var commandQueue = msg_IntPtr(device, Sel("newCommandQueue"));
            if (commandQueue == IntPtr.Zero)
                throw new InvalidOperationException($"{description} returned no command queue.");
            return new MetalContext(device, commandQueue, description);
        }
        catch
        {
            Release(device);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Release(CommandQueue);
        Release(Device);
    }

    /// <summary>Runs <paramref name="work"/> inside an autorelease pool, drained when it returns or throws.</summary>
    public static void InAutoreleasePool(Action work)
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            work();
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    internal static string NSString(IntPtr value) =>
        value == IntPtr.Zero ? "unknown" : Marshal.PtrToStringUTF8(msg_IntPtr(value, Sel("UTF8String"))) ?? "unknown";
}

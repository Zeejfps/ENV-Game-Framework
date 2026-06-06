// Strongly-typed wrappers around the raw IntPtr handles returned by the
// Objective-C Metal/CoreAnimation runtime. These are opaque pointer types and
// must not outlive the device that produced them.

using System.Runtime.InteropServices;
using static ZGF.Core.MacOs.Objc;

namespace ZGF.Core.MacOs;

// Named MetalApi (not "Metal") to avoid colliding with the ZGF.Rendering.Metal namespace,
// which would shadow the type for callers inside the ZGF.* root namespace.
public static class MetalApi
{
    [DllImport("/System/Library/Frameworks/Metal.framework/Metal")]
    public static extern IntPtr MTLCreateSystemDefaultDevice();
}

public enum MTLPixelFormat : uint
{
    Invalid = 0,
    R8Unorm = 10,
    RGBA8Unorm = 70,
    BGRA8Unorm = 80,
    Depth32Float = 252,
}

public enum MTLPrimitiveType : uint
{
    Point = 0,
    Line = 1,
    LineStrip = 2,
    Triangle = 3,
    TriangleStrip = 4,
}

public enum MTLLoadAction : uint
{
    DontCare = 0,
    Load = 1,
    Clear = 2,
}

public enum MTLStoreAction : uint
{
    DontCare = 0,
    Store = 1,
}

public enum MTLResourceOptions : ulong
{
    StorageModeShared = 0 << 4,
    StorageModeManaged = 1 << 4,
    StorageModePrivate = 2 << 4,
}

public enum MTLVertexFormat : uint
{
    Invalid = 0,
    Float = 28,
    Float2 = 29,
    Float4 = 31,
    UInt = 36,
}

public enum MTLVertexStepFunction : uint
{
    Constant = 0,
    PerVertex = 1,
    PerInstance = 2,
}

public enum MTLTextureType : uint
{
    Type2D = 2,
}

public enum MTLTextureUsage : uint
{
    Unknown = 0,
    ShaderRead = 1,
    RenderTarget = 4,
}

public enum MTLStorageMode : uint
{
    Shared = 0,
    Managed = 1,
    Private = 2,
}

public enum MTLSamplerMinMagFilter : uint
{
    Nearest = 0,
    Linear = 1,
}

public enum MTLSamplerAddressMode : uint
{
    ClampToEdge = 0,
    Repeat = 2,
}

[StructLayout(LayoutKind.Sequential)]
public struct MTLClearColor
{
    public double Red, Green, Blue, Alpha;
    public MTLClearColor(double r, double g, double b, double a)
    {
        Red = r; Green = g; Blue = b; Alpha = a;
    }
}

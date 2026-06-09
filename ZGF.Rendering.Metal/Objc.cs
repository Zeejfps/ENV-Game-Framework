// Minimal Objective-C runtime interop for macOS. Used to attach a CAMetalLayer
// to a GLFW NSWindow and to drive Metal/CoreAnimation objects from C# without
// taking a dependency on a managed Objective-C bridge.

using System.Runtime.InteropServices;

namespace ZGF.Rendering.Metal;

public static unsafe class Objc
{
    private const string Libobjc = "/usr/lib/libobjc.A.dylib";

    [DllImport(Libobjc)]
    public static extern IntPtr objc_getClass([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(Libobjc)]
    public static extern IntPtr sel_registerName([MarshalAs(UnmanagedType.LPStr)] string name);

    // --- runtime class creation ---
    //
    // Used to synthesize a tiny Objective-C target class at runtime (e.g. the receiver
    // for NSMenuItem actions) whose method IMPs point at [UnmanagedCallersOnly] statics.
    // This is the AOT-safe alternative to a managed Objective-C bridge.

    [DllImport(Libobjc)]
    public static extern IntPtr objc_allocateClassPair(IntPtr superclass, [MarshalAs(UnmanagedType.LPStr)] string name, nuint extraBytes);

    [DllImport(Libobjc)]
    public static extern void objc_registerClassPair(IntPtr cls);

    [DllImport(Libobjc)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool class_addMethod(IntPtr cls, IntPtr name, IntPtr imp, [MarshalAs(UnmanagedType.LPStr)] string types);

    // --- objc_msgSend variants ---
    //
    // Apple's libobjc exposes a single `objc_msgSend` symbol; the calling
    // convention requires us to redeclare it with the matching argument types
    // because the C ABI does not promote/convert (notably for floats and
    // structs returned by value). Each overload below is a distinct DllImport
    // bound to the same symbol but with a unique managed signature.

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern IntPtr msg_IntPtr(IntPtr receiver, IntPtr selector);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern IntPtr msg_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern IntPtr msg_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern IntPtr msg_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern IntPtr msg_IntPtr_Bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void(IntPtr receiver, IntPtr selector);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_Bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_CGSize(IntPtr receiver, IntPtr selector, CGSize size);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_UInt(IntPtr receiver, IntPtr selector, uint arg);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_ULong(IntPtr receiver, IntPtr selector, ulong arg);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_Long(IntPtr receiver, IntPtr selector, long arg);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_IntPtr_ULong_IntPtr(IntPtr receiver, IntPtr selector, IntPtr a, ulong b, IntPtr c);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_MTLRegion_NUInt_IntPtr_NUInt(IntPtr receiver, IntPtr selector, MTLRegion region, nuint level, IntPtr bytes, nuint bytesPerRow);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern IntPtr msg_IntPtr_MTLPrim_UInt_UInt_UInt(IntPtr receiver, IntPtr selector, uint prim, uint vertexStart, uint vertexCount, uint instanceCount);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_MTLPrim_NUInt_NUInt_NUInt(IntPtr receiver, IntPtr selector, nuint prim, nuint vertexStart, nuint vertexCount, nuint instanceCount);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_MTLPrim_NUInt_NUInt_NUInt_NUInt(IntPtr receiver, IntPtr selector, nuint prim, nuint vertexStart, nuint vertexCount, nuint instanceCount, nuint baseInstance);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_IntPtr_NUInt(IntPtr receiver, IntPtr selector, IntPtr a, nuint b);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_IntPtr_NUInt_NUInt(IntPtr receiver, IntPtr selector, IntPtr a, nuint b, nuint c);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern IntPtr msg_IntPtr_NUInt_NUInt(IntPtr receiver, IntPtr selector, nuint a, nuint b);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_NUInt_NUInt(IntPtr receiver, IntPtr selector, nuint a, nuint b);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern void msg_Void_Double(IntPtr receiver, IntPtr selector, double value);

    // On ARM64, plain objc_msgSend returns floats/doubles directly (no fpret variant).
    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern double msg_Double(IntPtr receiver, IntPtr selector);

    // objc_msgSend returning NSInteger (long on LP64). Used to read e.g. NSMenuItem.tag.
    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    public static extern long msg_Long(IntPtr receiver, IntPtr selector);

    // Objective-C autorelease pool. A manual (non-Cocoa) run loop creates autoreleased
    // objects every turn — NSEvents from event polling, plus per-frame CAMetalDrawables,
    // command buffers, encoders and render-pass descriptors. With no pool draining each
    // turn they accumulate as unbounded *unmanaged* growth (the managed heap stays flat).
    // Push at the top of each loop turn and Pop at the end. On iOS the UIKit run loop
    // supplies its own pool, so this is only needed by the macOS GLFW loop.
    [DllImport(Libobjc)]
    public static extern IntPtr objc_autoreleasePoolPush();

    [DllImport(Libobjc)]
    public static extern void objc_autoreleasePoolPop(IntPtr pool);

    public static IntPtr Sel(string name) => sel_registerName(name);
    public static IntPtr Class(string name) => objc_getClass(name);

    public static IntPtr New(IntPtr cls)
    {
        var alloc = msg_IntPtr(cls, Sel("alloc"));
        return msg_IntPtr(alloc, Sel("init"));
    }

    public static void Release(IntPtr obj)
    {
        if (obj == IntPtr.Zero) return;
        msg_Void(obj, Sel("release"));
    }

    public static void Retain(IntPtr obj)
    {
        if (obj == IntPtr.Zero) return;
        msg_IntPtr(obj, Sel("retain"));
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct CGSize
{
    public double Width;
    public double Height;
    public CGSize(double w, double h) { Width = w; Height = h; }
}

[StructLayout(LayoutKind.Sequential)]
public struct MTLOrigin
{
    public nuint X, Y, Z;
}

[StructLayout(LayoutKind.Sequential)]
public struct MTLSize
{
    public nuint Width, Height, Depth;
}

[StructLayout(LayoutKind.Sequential)]
public struct MTLRegion
{
    public MTLOrigin Origin;
    public MTLSize Size;
}

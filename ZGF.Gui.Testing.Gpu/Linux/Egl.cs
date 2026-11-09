using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Gui.Testing.Gpu.Linux;

[SupportedOSPlatform("linux")]
internal static unsafe partial class Egl
{
    public const string Library = "libEGL.so.1";

    public const int Success = 0x3000;
    public const int None = 0x3038;
    public const int True = 1;
    public const int Vendor = 0x3053;
    public const int Version = 0x3054;
    public const int Extensions = 0x3055;
    public const int SurfaceType = 0x3033;
    public const int RenderableType = 0x3040;
    public const int OpenGlBit = 0x0008;
    public const int OpenGlApi = 0x30A2;
    public const int PlatformSurfacelessMesa = 0x31DD;
    public const int ContextMajorVersion = 0x3098;
    public const int ContextMinorVersion = 0x30FB;
    public const int ContextFlags = 0x30FC;
    public const int ContextOpenGlProfileMask = 0x30FD;
    public const int ContextOpenGlCoreProfileBit = 0x0001;
    public const int ContextOpenGlForwardCompatibleBit = 0x0002;

    [LibraryImport(Library, EntryPoint = "eglGetError")]
    public static partial int GetError();

    [LibraryImport(Library, EntryPoint = "eglQueryString")]
    public static partial byte* QueryString(nint display, int name);

    [LibraryImport(Library, EntryPoint = "eglGetPlatformDisplay")]
    public static partial nint GetPlatformDisplay(int platform, nint nativeDisplay, nint* attributes);

    [LibraryImport(Library, EntryPoint = "eglInitialize")]
    public static partial int Initialize(nint display, int* major, int* minor);

    [LibraryImport(Library, EntryPoint = "eglTerminate")]
    public static partial int Terminate(nint display);

    [LibraryImport(Library, EntryPoint = "eglBindAPI")]
    public static partial int BindApi(int api);

    [LibraryImport(Library, EntryPoint = "eglChooseConfig")]
    public static partial int ChooseConfig(nint display, int* attributes, nint* configs, int configSize, int* configCount);

    [LibraryImport(Library, EntryPoint = "eglCreateContext")]
    public static partial nint CreateContext(nint display, nint config, nint shareContext, int* attributes);

    [LibraryImport(Library, EntryPoint = "eglDestroyContext")]
    public static partial int DestroyContext(nint display, nint context);

    [LibraryImport(Library, EntryPoint = "eglMakeCurrent")]
    public static partial int MakeCurrent(nint display, nint draw, nint read, nint context);

    [LibraryImport(Library, EntryPoint = "eglGetCurrentContext")]
    public static partial nint GetCurrentContext();

    [LibraryImport(Library, EntryPoint = "eglReleaseThread")]
    public static partial int ReleaseThread();

    [LibraryImport(Library, EntryPoint = "eglGetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint GetProcAddress(string name);

    public static string String(nint display, int name)
    {
        var value = QueryString(display, name);
        return value == null ? "" : Marshal.PtrToStringUTF8((nint)value) ?? "";
    }

    public static bool HasExtension(string extensions, string name) =>
        extensions.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(name, StringComparer.Ordinal);
}

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Gui.Testing.Gpu.Linux;

/// <summary>A desktop GL 4.5+ core context on Mesa's surfaceless EGL platform, current with no surface.
/// Every member runs on the thread that created it.</summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class EglContext : IGlContext
{
    private const string SurfacelessPlatform = "EGL_MESA_platform_surfaceless";
    private const string SurfacelessContext = "EGL_KHR_surfaceless_context";
    private const string CreateContextExtension = "EGL_KHR_create_context";

    private readonly nint _egl;
    private nint _openGl;
    private nint _display;
    private nint _context;
    private bool _disposed;

    private EglContext(nint egl)
    {
        _egl = egl;
    }

    /// <exception cref="NotSupportedException">EGL, an extension it needs, or a 4.5 core context is
    /// unavailable; the message names which.</exception>
    public static EglContext Create(int major, int minor)
    {
        if (!NativeLibrary.TryLoad(Egl.Library, out var egl))
            throw new NotSupportedException($"{Egl.Library} could not be loaded; Mesa's EGL (libegl1, libegl-mesa0) is required.");
        var context = new EglContext(egl);
        try
        {
            context.Initialize(major, minor);
            return context;
        }
        catch
        {
            context.Dispose();
            throw;
        }
    }

    /// <summary>Resolves a GL entry point through eglGetProcAddress, falling back to libOpenGL's exports
    /// for core functions an EGL without EGL_KHR_get_all_proc_addresses does not hand out.</summary>
    public nint GetProcAddress(string name)
    {
        var address = Egl.GetProcAddress(name);
        if (address != 0)
            return address;
        if (_openGl == 0 && !NativeLibrary.TryLoad("libOpenGL.so.0", out _openGl))
            return 0;
        return NativeLibrary.TryGetExport(_openGl, name, out var exported) ? exported : 0;
    }

    private void Initialize(int major, int minor)
    {
        if (!NativeLibrary.TryGetExport(_egl, "eglGetPlatformDisplay", out _))
            throw new NotSupportedException($"{Egl.Library} does not export eglGetPlatformDisplay; EGL 1.5 is required.");
        var clientExtensions = Egl.String(0, Egl.Extensions);
        if (!Egl.HasExtension(clientExtensions, SurfacelessPlatform))
            throw new NotSupportedException($"EGL does not offer {SurfacelessPlatform} (client extensions: {clientExtensions}).");

        _display = Egl.GetPlatformDisplay(Egl.PlatformSurfacelessMesa, 0, null);
        if (_display == 0)
            throw Failure("eglGetPlatformDisplay");
        int eglMajor, eglMinor;
        if (Egl.Initialize(_display, &eglMajor, &eglMinor) != Egl.True)
        {
            _display = 0;
            throw Failure("eglInitialize");
        }

        var extensions = Egl.String(_display, Egl.Extensions);
        if (!Egl.HasExtension(extensions, SurfacelessContext))
            throw new NotSupportedException($"{Describe()} does not offer {SurfacelessContext}.");
        if ((eglMajor, eglMinor) is (1, < 5) && !Egl.HasExtension(extensions, CreateContextExtension))
            throw new NotSupportedException($"{Describe()} offers neither EGL 1.5 nor {CreateContextExtension}.");
        if (Egl.BindApi(Egl.OpenGlApi) != Egl.True)
            throw new NotSupportedException($"{Describe()} cannot bind desktop OpenGL (error 0x{Egl.GetError():X}).");

        var configAttributes = stackalloc int[]
        {
            Egl.SurfaceType, 0,
            Egl.RenderableType, Egl.OpenGlBit,
            Egl.None,
        };
        nint config;
        int count;
        if (Egl.ChooseConfig(_display, configAttributes, &config, 1, &count) != Egl.True || count < 1)
            throw new NotSupportedException($"{Describe()} has no config that renders desktop OpenGL (error 0x{Egl.GetError():X}).");

        var contextAttributes = stackalloc int[]
        {
            Egl.ContextMajorVersion, major,
            Egl.ContextMinorVersion, minor,
            Egl.ContextFlags, Egl.ContextOpenGlForwardCompatibleBit,
            Egl.ContextOpenGlProfileMask, Egl.ContextOpenGlCoreProfileBit,
            Egl.None,
        };
        _context = Egl.CreateContext(_display, config, 0, contextAttributes);
        if (_context == 0)
            throw new NotSupportedException($"{Describe()} refused a {major}.{minor} core context (error 0x{Egl.GetError():X}).");
        if (Egl.MakeCurrent(_display, 0, 0, _context) != Egl.True)
            throw Failure("eglMakeCurrent");
    }

    private string Describe() => $"EGL {Egl.String(_display, Egl.Version)} ({Egl.String(_display, Egl.Vendor)})";

    private static InvalidOperationException Failure(string function) =>
        new($"{function} failed (error 0x{Egl.GetError():X}) creating the surfaceless EGL context.");

    /// <exception cref="InvalidOperationException">EGL reported a failure releasing the context, display or
    /// thread state; everything is still released.</exception>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var failures = new List<string>();
        if (_display != 0)
        {
            Check(Egl.MakeCurrent(_display, 0, 0, 0), "eglMakeCurrent", failures);
            if (_context != 0)
                Check(Egl.DestroyContext(_display, _context), "eglDestroyContext", failures);
            Check(Egl.Terminate(_display), "eglTerminate", failures);
        }
        _context = 0;
        _display = 0;
        Check(Egl.ReleaseThread(), "eglReleaseThread", failures);
        if (_openGl != 0)
            NativeLibrary.Free(_openGl);
        NativeLibrary.Free(_egl);
        if (failures.Count > 0)
            throw new InvalidOperationException($"Releasing the EGL context failed: {string.Join("; ", failures)}.");
    }

    private static void Check(int result, string function, List<string> failures)
    {
        if (result != Egl.True)
            failures.Add($"{function} (error 0x{Egl.GetError():X})");
    }
}

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ZGF.Gui.Testing.Gpu.Windows.Win32;

namespace ZGF.Gui.Testing.Gpu.Windows;

/// <summary>A desktop GL 4.5+ core context on a hidden window that is never shown. Every member runs on
/// the thread that created it.</summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WglContext : IGlContext
{
    private const int ContextMajorVersion = 0x2091;
    private const int ContextMinorVersion = 0x2092;
    private const int ContextFlags = 0x2094;
    private const int ContextProfileMask = 0x9126;
    private const int ForwardCompatibleBit = 0x0002;
    private const int CoreProfileBit = 0x0001;
    private const uint GlVersion = 0x1F02;
    private const uint GlRenderer = 0x1F01;

    private readonly nint _openGl32;
    private readonly nint _user32;
    private readonly nint _instance;
    private ushort _classAtom;
    private nint _window;
    private nint _dc;
    private nint _context;
    private bool _disposed;

    private WglContext()
    {
        _openGl32 = NativeLibrary.Load("opengl32.dll");
        _user32 = NativeLibrary.Load("user32.dll");
        _instance = GetModuleHandle(null);
    }

    public static WglContext Create(int major, int minor)
    {
        var context = new WglContext();
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

    /// <summary>Resolves a GL or WGL entry point: extensions and everything past GL 1.1 through
    /// wglGetProcAddress, GL 1.1 from opengl32.dll's exports, for which wglGetProcAddress has nothing.</summary>
    public nint GetProcAddress(string name)
    {
        var address = WglGetProcAddress(name);
        if (IsValid(address))
            return address;
        return NativeLibrary.TryGetExport(_openGl32, name, out var exported) ? exported : 0;
    }

    /// <summary>Delivers messages sent to the hidden window, so a broadcast never waits on this thread.</summary>
    public void PumpMessages()
    {
        if (_disposed) return;
        Msg message;
        while (PeekMessage(&message, 0, 0, 0, PmRemove) != 0)
        {
            TranslateMessage(&message);
            DispatchMessage(&message);
        }
    }

    private void Initialize(int major, int minor)
    {
        CreateHiddenWindow();

        var pixelFormat = new PixelFormatDescriptor
        {
            Size = (ushort)sizeof(PixelFormatDescriptor),
            Version = 1,
            Flags = PfdDrawToWindow | PfdSupportOpenGl | PfdDoubleBuffer,
            PixelType = PfdTypeRgba,
            ColorBits = 32,
            AlphaBits = 8,
        };
        var format = ChoosePixelFormat(_dc, &pixelFormat);
        if (format == 0)
            throw Win32Failure("ChoosePixelFormat");
        if (SetPixelFormat(_dc, format, &pixelFormat) == 0)
            throw Win32Failure("SetPixelFormat");

        var legacy = WglCreateContext(_dc);
        if (legacy == 0)
            throw Win32Failure("wglCreateContext");
        try
        {
            if (WglMakeCurrent(_dc, legacy) == 0)
                throw Win32Failure("wglMakeCurrent");
            _context = CreateCoreContext(major, minor);
            if (WglMakeCurrent(_dc, _context) == 0)
                throw Win32Failure("wglMakeCurrent");
        }
        finally
        {
            WglDeleteContext(legacy);
        }
    }

    private void CreateHiddenWindow()
    {
        var className = $"ZGF.GpuDevice.{Guid.NewGuid():N}";
        fixed (char* name = className)
        {
            var windowClass = new WndClassEx
            {
                Size = (uint)sizeof(WndClassEx),
                Style = CsOwnDc,
                WndProc = NativeLibrary.GetExport(_user32, "DefWindowProcW"),
                Instance = _instance,
                ClassName = name,
            };
            _classAtom = RegisterClassEx(&windowClass);
            if (_classAtom == 0)
                throw Win32Failure("RegisterClassExW");
        }

        _window = CreateWindowEx(WsExToolWindow | WsExNoActivate, _classAtom, null, WsPopup,
            0, 0, 1, 1, 0, 0, _instance, 0);
        if (_window == 0)
            throw Win32Failure("CreateWindowExW");

        _dc = GetDC(_window);
        if (_dc == 0)
            throw new InvalidOperationException("GetDC returned no device context for the hidden GL window.");
    }

    private nint CreateCoreContext(int major, int minor)
    {
        var createContextAttribs = WglGetProcAddress("wglCreateContextAttribsARB");
        if (!IsValid(createContextAttribs))
            throw new NotSupportedException(
                $"The OpenGL driver does not provide wglCreateContextAttribsARB, so no {major}.{minor} core context can be made ({Describe()}).");

        var attributes = stackalloc int[]
        {
            ContextMajorVersion, major,
            ContextMinorVersion, minor,
            ContextFlags, ForwardCompatibleBit,
            ContextProfileMask, CoreProfileBit,
            0,
        };
        var context = ((delegate* unmanaged[Stdcall]<nint, nint, int*, nint>)createContextAttribs)(_dc, 0, attributes);
        if (context == 0)
            throw new NotSupportedException(
                $"The OpenGL driver refused a {major}.{minor} core context; {Describe()}.");
        return context;
    }

    private string Describe() => $"legacy context: version {GlString(GlVersion)}, renderer {GlString(GlRenderer)}";

    private string GlString(uint name)
    {
        if (!NativeLibrary.TryGetExport(_openGl32, "glGetString", out var getString))
            return "unknown";
        var value = ((delegate* unmanaged[Stdcall]<uint, byte*>)getString)(name);
        return value == null ? "unknown" : Marshal.PtrToStringAnsi((nint)value) ?? "unknown";
    }

    // wglGetProcAddress signals failure with 0, and some drivers with 1, 2, 3 or -1.
    private static bool IsValid(nint address) => address is not (0 or 1 or 2 or 3 or -1);

    private static InvalidOperationException Win32Failure(string function) =>
        new($"{function} failed (error 0x{Marshal.GetLastPInvokeError():X}) creating the hidden GL window.");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_context != 0)
        {
            WglMakeCurrent(0, 0);
            WglDeleteContext(_context);
            _context = 0;
        }
        if (_dc != 0)
        {
            ReleaseDC(_window, _dc);
            _dc = 0;
        }
        if (_window != 0)
        {
            DestroyWindow(_window);
            _window = 0;
        }
        if (_classAtom != 0)
        {
            UnregisterClass(_classAtom, _instance);
            _classAtom = 0;
        }
        NativeLibrary.Free(_user32);
        NativeLibrary.Free(_openGl32);
    }
}

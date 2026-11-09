using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Gui.Testing.Gpu.Windows;

[SupportedOSPlatform("windows")]
internal static unsafe partial class Win32
{
    public const uint CsOwnDc = 0x0020;
    public const uint WsPopup = 0x80000000;
    public const uint WsExToolWindow = 0x00000080;
    public const uint WsExNoActivate = 0x08000000;
    public const uint PmRemove = 0x0001;

    public const uint PfdDrawToWindow = 0x00000004;
    public const uint PfdSupportOpenGl = 0x00000020;
    public const uint PfdDoubleBuffer = 0x00000001;
    public const byte PfdTypeRgba = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct WndClassEx
    {
        public uint Size;
        public uint Style;
        public nint WndProc;
        public int ClassExtra;
        public int WindowExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Background;
        public char* MenuName;
        public char* ClassName;
        public nint IconSmall;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PixelFormatDescriptor
    {
        public ushort Size;
        public ushort Version;
        public uint Flags;
        public byte PixelType;
        public byte ColorBits;
        public byte RedBits, RedShift, GreenBits, GreenShift, BlueBits, BlueShift, AlphaBits, AlphaShift;
        public byte AccumBits, AccumRedBits, AccumGreenBits, AccumBlueBits, AccumAlphaBits;
        public byte DepthBits;
        public byte StencilBits;
        public byte AuxBuffers;
        public byte LayerType;
        public byte Reserved;
        public uint LayerMask;
        public uint VisibleMask;
        public uint DamageMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Msg
    {
        public nint Window;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int PointX;
        public int PointY;
        public uint Private;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
    public static partial nint GetModuleHandle(char* moduleName);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    public static partial ushort RegisterClassEx(WndClassEx* windowClass);

    [LibraryImport("user32.dll", EntryPoint = "UnregisterClassW")]
    public static partial int UnregisterClass(nint atom, nint instance);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
    public static partial nint CreateWindowEx(
        uint exStyle, nint className, char* windowName, uint style,
        int x, int y, int width, int height,
        nint parent, nint menu, nint instance, nint param);

    [LibraryImport("user32.dll")]
    public static partial int DestroyWindow(nint window);

    [LibraryImport("user32.dll")]
    public static partial nint GetDC(nint window);

    [LibraryImport("user32.dll")]
    public static partial int ReleaseDC(nint window, nint dc);

    [LibraryImport("user32.dll", EntryPoint = "PeekMessageW")]
    public static partial int PeekMessage(Msg* message, nint window, uint filterMin, uint filterMax, uint remove);

    [LibraryImport("user32.dll")]
    public static partial int TranslateMessage(Msg* message);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    public static partial nint DispatchMessage(Msg* message);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int ChoosePixelFormat(nint dc, PixelFormatDescriptor* descriptor);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int SetPixelFormat(nint dc, int format, PixelFormatDescriptor* descriptor);

    [LibraryImport("opengl32.dll", EntryPoint = "wglCreateContext", SetLastError = true)]
    public static partial nint WglCreateContext(nint dc);

    [LibraryImport("opengl32.dll", EntryPoint = "wglDeleteContext")]
    public static partial int WglDeleteContext(nint context);

    [LibraryImport("opengl32.dll", EntryPoint = "wglMakeCurrent", SetLastError = true)]
    public static partial int WglMakeCurrent(nint dc, nint context);

    [LibraryImport("opengl32.dll", EntryPoint = "wglGetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint WglGetProcAddress(string name);
}

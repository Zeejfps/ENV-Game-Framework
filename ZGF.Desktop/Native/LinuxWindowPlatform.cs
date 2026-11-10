using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ZGF.Desktop;

// On Linux the title bar is drawn by the window manager, not the app — there is no per-window
// dark-mode attribute like Win32's DWM or macOS's NSAppearance. The closest mechanism is the
// _GTK_THEME_VARIANT window property: GTK-themed window managers (GNOME/Mutter, XFCE) read it
// to pick the dark or light variant of the decorations they draw. This is X11 only; under a
// pure Wayland backend there is no X11 window and the call no-ops (GitBench targets X11, with
// Wayland sessions served through XWayland).
[SupportedOSPlatform("linux")]
internal sealed class LinuxWindowPlatform : WindowPlatform
{
    private const int PropModeReplace = 0;
    private const int Format8Bit = 8;

    private const ulong CWOverrideRedirect = 1UL << 9;

    private IntPtr _display;

    /// <summary>Makes the popup override-redirect, as GTK and Qt make their menus and tooltips: the
    /// window manager then leaves it alone, so it gets no taskbar entry, no frame and never the
    /// focus. Without it a tooltip is a top-level window that XFCE lists in its taskbar and focuses
    /// when shown, which takes the hover from the window under it, hides the tooltip, and so on.
    /// It is set on GLFW's own connection, and before the window is first mapped, as X11 reads
    /// override-redirect only when a window is mapped.</summary>
    public override void DecoratePopup(IntPtr x11Window)
    {
        if (x11Window == IntPtr.Zero) return;

        var display = GLFW.Native.GetX11Display();
        if (display == IntPtr.Zero) return;

        var attributes = new XSetWindowAttributes { OverrideRedirect = 1 };
        XChangeWindowAttributes(display, x11Window, new UIntPtr(CWOverrideRedirect), ref attributes);
        XFlush(display);
    }

    public override void SetTitleBarTheme(IntPtr x11Window, bool dark)
    {
        if (x11Window == IntPtr.Zero) return;

        if (_display == IntPtr.Zero)
            _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero) return;

        var property = XInternAtom(_display, "_GTK_THEME_VARIANT", false);
        var utf8String = XInternAtom(_display, "UTF8_STRING", false);
        var value = Encoding.UTF8.GetBytes(dark ? "dark" : "light");

        XChangeProperty(_display, x11Window, property, utf8String, Format8Bit, PropModeReplace, value, value.Length);
        XFlush(_display);
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XInternAtom(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string name, bool onlyIfExists);

    [DllImport("libX11.so.6")]
    private static extern int XChangeProperty(
        IntPtr display, IntPtr window, IntPtr property, IntPtr type,
        int format, int mode, byte[] data, int elementCount);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XChangeWindowAttributes(IntPtr display, IntPtr window, UIntPtr valueMask, ref XSetWindowAttributes attributes);

    /// <summary>Xlib's XSetWindowAttributes, field for field: unsigned long and long are pointer-sized
    /// on 64-bit Linux, Bool and int are 32-bit. Only the fields a value mask names are read.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct XSetWindowAttributes
    {
        public UIntPtr BackgroundPixmap;
        public UIntPtr BackgroundPixel;
        public UIntPtr BorderPixmap;
        public UIntPtr BorderPixel;
        public int BitGravity;
        public int WinGravity;
        public int BackingStore;
        public UIntPtr BackingPlanes;
        public UIntPtr BackingPixel;
        public int SaveUnder;
        public IntPtr EventMask;
        public IntPtr DoNotPropagateMask;
        public int OverrideRedirect;
        public UIntPtr Colormap;
        public UIntPtr Cursor;
    }
}

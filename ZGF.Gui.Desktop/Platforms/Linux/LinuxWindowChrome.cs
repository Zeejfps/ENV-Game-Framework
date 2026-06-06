using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using ZGF.Desktop;

namespace ZGF.Gui.Desktop.Platforms.Linux;

// On Linux the title bar is drawn by the window manager, not the app — there is no per-window
// dark-mode attribute like Win32's DWM or macOS's NSAppearance. The closest mechanism is the
// _GTK_THEME_VARIANT window property: GTK-themed window managers (GNOME/Mutter, XFCE) read it
// to pick the dark or light variant of the decorations they draw. This is X11 only; under a
// pure Wayland backend there is no X11 window and the call no-ops (GitBench targets X11, with
// Wayland sessions served through XWayland).
[SupportedOSPlatform("linux")]
public sealed class LinuxWindowChrome : IWindowChrome
{
    private const int PropModeReplace = 0;
    private const int Format8Bit = 8;

    private IntPtr _display;

    public void SetTitleBarTheme(IWindow window, bool dark)
    {
        var x11Window = window.NativeHandle;
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
}

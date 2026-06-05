using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Gui;

[SupportedOSPlatform("windows")]
public sealed class WindowsWindowChrome : IWindowChrome
{
    // DWM window attribute that toggles the dark title bar. The constant changed
    // between Windows 10 builds: 19 on 1809, 20 on 2004+ and Windows 11. We try
    // the newer value first and fall back to the older one if DWM rejects it.
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    public void SetTitleBarTheme(IntPtr nativeWindowHandle, bool dark)
    {
        if (nativeWindowHandle == IntPtr.Zero) return;
        var hwnd = nativeWindowHandle;

        var value = dark ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref value, sizeof(int));

        // DWM often won't repaint the title bar of an already-visible window until
        // it next changes focus. Nudge the frame so the new color takes effect now.
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndAfter, int x, int y, int cx, int cy, uint flags);
}

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Desktop;

[SupportedOSPlatform("windows")]
internal static class WindowsDwm
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_DONOTROUND = 1;

    /// <summary>Asks DWM for the dark or light caption on <paramref name="hwnd"/> and waits for it to
    /// take effect. DWM drops the change if another window is hidden before it is composed.</summary>
    public static void SetDarkTitleBar(IntPtr hwnd, bool dark)
    {
        if (hwnd == IntPtr.Zero) return;
        var value = dark ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref value, sizeof(int));
        DwmFlush();
    }

    /// <summary>Keeps Windows 11 from rounding or outlining <paramref name="hwnd"/>, whose
    /// per-pixel-transparent content draws its own silhouette. A no-op before Windows 11.</summary>
    public static void KeepSquareCorners(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        var value = DWMWCP_DONOTROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();
}

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Geometry;

namespace ZGF.Desktop;

[SupportedOSPlatform("windows")]
internal static class WindowsDwm
{
    private const int DWMWA_CAPTION_BUTTON_BOUNDS = 5;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWCP_DONOTROUND = 1;
    private const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;

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

    /// <summary>Paints the caption, and the frame extended into the client area, in
    /// <paramref name="argb"/> (alpha ignored). Windows 11 only.</summary>
    public static void SetCaptionColor(IntPtr hwnd, uint argb)
    {
        var colorRef = (int)ToColorRef(argb);
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
    }

    public static void ResetCaptionColor(IntPtr hwnd)
    {
        var colorRef = unchecked((int)DWMWA_COLOR_DEFAULT);
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
    }

    private static uint ToColorRef(uint argb)
    {
        var r = (argb >> 16) & 0xFF;
        var g = (argb >> 8) & 0xFF;
        var b = argb & 0xFF;
        return (b << 16) | (g << 8) | r;
    }

    /// <summary>Extends the frame <paramref name="top"/> pixels down into the client area, where DWM
    /// draws it, caption buttons included, under client pixels that are fully transparent.</summary>
    public static void ExtendFrameIntoClientArea(IntPtr hwnd, int top)
    {
        var margins = new MARGINS { Top = top };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    /// <summary>Where DWM draws the caption buttons, relative to the window's top-left corner.</summary>
    public static RectI? ReadCaptionButtons(IntPtr hwnd)
    {
        if (DwmGetWindowAttribute(hwnd, DWMWA_CAPTION_BUTTON_BOUNDS, out var rect, Marshal.SizeOf<RECT>()) != 0)
            return null;
        return new RectI(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>The caption button under the point a hit-test message asks about, when there is one.</summary>
    public static bool HitTestCaptionButtons(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr hit) =>
        DwmDefWindowProc(hwnd, msg, wParam, lParam, out hit);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS { public int Left, Right, Top, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT value, int size);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    [DllImport("dwmapi.dll")]
    private static extern bool DwmDefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr result);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();
}

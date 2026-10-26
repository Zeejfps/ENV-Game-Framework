using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Geometry;

namespace ZGF.Desktop;

/// <summary>A window whose content reaches under its native title bar. The title bar's strip goes to the
/// client area, DWM keeps drawing the caption buttons over it in the title bar's color, and hit testing
/// hands the caption buttons, the top resize edge and the app's drag areas back to Windows, so snapping,
/// Snap Layouts and the system menu work as they do with the native title bar.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsTitleBar
{
    private const int Windows11Build = 22000;

    private const uint WM_SIZE = 0x0005;
    private const uint WM_NCCALCSIZE = 0x0083;
    private const uint WM_NCHITTEST = 0x0084;

    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int SM_CYSIZEFRAME = 33;
    private const int SM_CXPADDEDBORDER = 92;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint MONITOR_DEFAULTTONEAREST = 0x0002;

    private readonly IntPtr _hwnd;
    private TitleBarContent _content;
    private int _extendedFrameTop = -1;

    public WindowsTitleBar(IntPtr hwnd, TitleBarContent content)
    {
        _hwnd = hwnd;
        _content = content;
    }

    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, Windows11Build);

    /// <summary>Where DWM draws the caption buttons, in client coordinates.</summary>
    public RectI CaptionButtons { get; private set; }

    public void Extend()
    {
        WindowsDwm.SetCaptionColor(_hwnd, _content.Background);
        RecalculateFrame();
        ExtendFrameUnderCaptionButtons();
    }

    public void Show(TitleBarContent content)
    {
        var background = _content.Background;
        _content = content;
        if (content.Background != background)
            WindowsDwm.SetCaptionColor(_hwnd, content.Background);
    }

    public void Restore()
    {
        WindowsDwm.ExtendFrameIntoClientArea(_hwnd, 0);
        WindowsDwm.ResetCaptionColor(_hwnd);
        RecalculateFrame();
    }

    /// <summary>How far the content reaches above where GLFW, which places the window by its styles,
    /// expects it: the title bar's height.</summary>
    public int Overlap()
    {
        GetWindowRect(_hwnd, out var window);
        var client = ReadClientRect();
        var top = client.Y - window.Top;
        return ExpectedClientTop() - top;
    }

    private int ExpectedClientTop()
    {
        var frame = new RECT();
        var style = (uint)GetWindowLongPtr(_hwnd, GWL_STYLE).ToInt64();
        var exStyle = (uint)GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        var dpi = GetDpiForWindow(_hwnd);
        AdjustWindowRectExForDpi(ref frame, style, false, exStyle, dpi);
        return -frame.Top;
    }

    /// <summary>Handles <paramref name="msg"/> for the title bar, passing it on to <paramref name="next"/>
    /// where Windows' own handling is wanted too; false for a message it leaves alone.</summary>
    public bool Handle(IntPtr next, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr result)
    {
        switch (msg)
        {
            case WM_NCCALCSIZE when wParam != IntPtr.Zero:
                result = CalculateClientArea(next, wParam, lParam);
                return true;
            case WM_NCHITTEST:
                result = HitTest(next, wParam, lParam);
                return true;
            case WM_SIZE:
                ExtendFrameUnderCaptionButtons();
                result = CallWindowProc(next, _hwnd, msg, wParam, lParam);
                return true;
            default:
                result = IntPtr.Zero;
                return false;
        }
    }

    /// <summary>The client area Windows computes, with the title bar's strip given to it: its top stays
    /// at the window's top, maximized too, where the frame reaches past the monitor. DWM finds the
    /// caption buttons from the client area's top, so a maximized window's content starts
    /// <see cref="HiddenTop"/> pixels further down instead.</summary>
    private IntPtr CalculateClientArea(IntPtr next, IntPtr wParam, IntPtr lParam)
    {
        var proposed = Marshal.PtrToStructure<RECT>(lParam);
        var result = CallWindowProc(next, _hwnd, WM_NCCALCSIZE, wParam, lParam);
        var client = Marshal.PtrToStructure<RECT>(lParam);
        client.Top = proposed.Top;
        Marshal.StructureToPtr(client, lParam, false);
        return result;
    }

    /// <summary>How many pixels of the client area, from its top, lie above the monitor's work area,
    /// where a maximized window's frame reaches past it; 0 for a window that is not maximized.</summary>
    public int HiddenTop()
    {
        if (!IsZoomed(_hwnd))
            return 0;
        var monitor = MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info))
            return 0;
        var client = ReadClientRect();
        return Math.Max(0, info.Work.Top - client.Y);
    }

    private IntPtr HitTest(IntPtr next, IntPtr wParam, IntPtr lParam)
    {
        if (WindowsDwm.HitTestCaptionButtons(_hwnd, WM_NCHITTEST, wParam, lParam, out var button))
            return button;
        var hit = CallWindowProc(next, _hwnd, WM_NCHITTEST, wParam, lParam);
        if (hit != HTCLIENT)
            return hit;
        var point = ToClient(lParam);
        if (IsOnTopEdge(point))
            return TopEdgeHit(point);
        if (IsOnDragArea(point))
            return HTCAPTION;
        return HTCLIENT;
    }

    private bool IsOnTopEdge(PointI point)
    {
        if (IsZoomed(_hwnd))
            return false;
        return point.Y < ResizeBorder();
    }

    private IntPtr TopEdgeHit(PointI point)
    {
        var border = ResizeBorder();
        GetClientRect(_hwnd, out var client);
        if (point.X < border)
            return HTTOPLEFT;
        if (point.X >= client.Right - border)
            return HTTOPRIGHT;
        return HTTOP;
    }

    private int ResizeBorder()
    {
        var dpi = GetDpiForWindow(_hwnd);
        return GetSystemMetricsForDpi(SM_CYSIZEFRAME, dpi) + GetSystemMetricsForDpi(SM_CXPADDEDBORDER, dpi);
    }

    private bool IsOnDragArea(PointI point)
    {
        foreach (var area in _content.DragAreas)
        {
            if (area.Contains(point))
                return true;
        }
        return false;
    }

    private PointI ToClient(IntPtr lParam)
    {
        var point = new POINT
        {
            X = (short)(lParam.ToInt64() & 0xFFFF),
            Y = (short)((lParam.ToInt64() >> 16) & 0xFFFF),
        };
        ScreenToClient(_hwnd, ref point);
        return new PointI(point.X, point.Y);
    }

    /// <summary>Has Windows work out the client area again. A shown window keeps its place, so its content
    /// moves; a hidden one moves so that its content keeps its place and size.</summary>
    private void RecalculateFrame()
    {
        var before = ReadClientRect();
        SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        if (IsWindowVisible(_hwnd))
            return;
        KeepClientRect(before);
    }

    private void KeepClientRect(RectI before)
    {
        var after = ReadClientRect();
        GetWindowRect(_hwnd, out var window);
        var top = window.Top + before.Y - after.Y;
        var width = window.Right - window.Left;
        var height = window.Bottom - window.Top + before.Height - after.Height;
        SetWindowPos(_hwnd, IntPtr.Zero, window.Left, top, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>The client area in screen coordinates.</summary>
    private RectI ReadClientRect()
    {
        GetClientRect(_hwnd, out var client);
        var origin = new POINT();
        ClientToScreen(_hwnd, ref origin);
        return new RectI(origin.X, origin.Y, client.Right, client.Bottom);
    }

    /// <summary>Extends DWM's frame down to the caption buttons' bottom, where it draws them, as they
    /// move with the window's size and DPI. Runs before GLFW hears of a new size, so the frame drawn
    /// for it already leaves the buttons' room.</summary>
    private void ExtendFrameUnderCaptionButtons()
    {
        CaptionButtons = ReadCaptionButtons();
        var top = Math.Max(1, CaptionButtons.Bottom);
        if (top == _extendedFrameTop)
            return;
        _extendedFrameTop = top;
        WindowsDwm.ExtendFrameIntoClientArea(_hwnd, top);
    }

    private RectI ReadCaptionButtons()
    {
        var buttons = WindowsDwm.ReadCaptionButtons(_hwnd);
        if (buttons == null)
            return default;
        GetWindowRect(_hwnd, out var window);
        var client = ReadClientRect();
        var x = buttons.Value.X - (client.X - window.Left);
        var y = buttons.Value.Y - (client.Y - window.Top);
        return buttons.Value with { X = x, Y = y };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hwnd, ref POINT pt);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hwnd, ref POINT pt);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);

    [DllImport("user32.dll")]
    private static extern bool AdjustWindowRectExForDpi(ref RECT rect, uint style, bool menu, uint exStyle, uint dpi);
}

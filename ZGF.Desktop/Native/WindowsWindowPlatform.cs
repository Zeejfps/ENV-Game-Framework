using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Geometry;

namespace ZGF.Desktop;

[SupportedOSPlatform("windows")]
internal sealed class WindowsWindowPlatform : WindowPlatform
{
    private const int GWL_EXSTYLE = -20;
    private const int GWLP_WNDPROC = -4;
    private const int GWLP_HWNDPARENT = -8;

    private const long WS_EX_TOOLWINDOW = 0x00000080;
    private const long WS_EX_TOPMOST = 0x00000008;
    private const long WS_EX_NOACTIVATE = 0x08000000;

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOACTIVATE = 0x0010;

    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_MBUTTONDOWN = 0x0207;
    private const uint WM_NCLBUTTONDOWN = 0x00A1;
    private const uint WM_NCRBUTTONDOWN = 0x00A4;
    private const uint WM_CANCELMODE = 0x001F;

    private delegate IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    // One window procedure per window for everything GLFW does not surface. OutsideClick: a popup whose
    // outside clicks (and WM_CANCELMODE) dismiss the menu chain. NonClientPress: a host window whose
    // non-client presses (title bar, borders, caption buttons) dismiss an open menu. TitleBar: a window
    // whose content reaches under its native title bar.
    private sealed class Subclass
    {
        public IntPtr OriginalProc;
        public WndProc NewProc = null!;
        public Action<PointI>? OutsideClick;
        public Action? NonClientPress;
        public WindowsTitleBar? TitleBar;

        public bool IsUnused => OutsideClick == null && NonClientPress == null && TitleBar == null;
    }

    private readonly Dictionary<IntPtr, Subclass> _subclasses = new();
    private IntPtr _capturedHwnd = IntPtr.Zero;

    public override void DecoratePopup(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;

        var ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        ex |= WS_EX_TOOLWINDOW;
        ex |= WS_EX_NOACTIVATE;
        ex |= WS_EX_TOPMOST;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
        WindowsDwm.KeepSquareCorners(hwnd);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_NOACTIVATE);
    }

    public override void CapturePointer(IntPtr hwnd, Action<PointI> onOutsideClick)
    {
        if (hwnd == IntPtr.Zero) return;

        Hook(hwnd).OutsideClick = onOutsideClick;
        SetCapture(hwnd);
        _capturedHwnd = hwnd;
    }

    public override void ReleasePointerCapture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;

        if (_capturedHwnd == hwnd)
        {
            ReleaseCapture();
            _capturedHwnd = IntPtr.Zero;
        }
        if (!_subclasses.TryGetValue(hwnd, out var sub)) return;
        sub.OutsideClick = null;
        UnhookIfUnused(hwnd, sub);
    }

    public override void WatchNonClientPress(IntPtr hwnd, Action onNonClientPress)
    {
        if (hwnd == IntPtr.Zero) return;
        Hook(hwnd).NonClientPress = onNonClientPress;
    }

    public override void UnwatchNonClientPress(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        if (!_subclasses.TryGetValue(hwnd, out var sub)) return;
        sub.NonClientPress = null;
        UnhookIfUnused(hwnd, sub);
    }

    public override void SetTitleBarTheme(IntPtr hwnd, bool dark) => WindowsDwm.SetDarkTitleBar(hwnd, dark);

    public override bool CanExtendIntoTitleBar => WindowsTitleBar.IsSupported;

    public override void ExtendIntoTitleBar(IntPtr hwnd, TitleBarContent? content)
    {
        if (hwnd == IntPtr.Zero) return;
        if (!WindowsTitleBar.IsSupported) return;
        if (content == null)
            RestoreTitleBar(hwnd);
        else
            ShowInTitleBar(hwnd, content);
    }

    private void ShowInTitleBar(IntPtr hwnd, TitleBarContent content)
    {
        var sub = Hook(hwnd);
        if (sub.TitleBar != null)
        {
            sub.TitleBar.Show(content);
            return;
        }
        sub.TitleBar = new WindowsTitleBar(hwnd, content);
        sub.TitleBar.Extend();
    }

    private void RestoreTitleBar(IntPtr hwnd)
    {
        if (!_subclasses.TryGetValue(hwnd, out var sub)) return;
        var titleBar = sub.TitleBar;
        if (titleBar == null) return;
        sub.TitleBar = null;
        titleBar.Restore();
        UnhookIfUnused(hwnd, sub);
    }

    public override RectI CaptionButtons(IntPtr hwnd)
    {
        var titleBar = FindTitleBar(hwnd);
        if (titleBar == null) return default;
        return titleBar.CaptionButtons;
    }

    public override int HiddenContentTop(IntPtr hwnd)
    {
        var titleBar = FindTitleBar(hwnd);
        if (titleBar == null) return 0;
        return titleBar.HiddenTop();
    }

    public override int TitleBarOverlap(IntPtr hwnd)
    {
        var titleBar = FindTitleBar(hwnd);
        if (titleBar == null) return 0;
        return titleBar.Overlap();
    }

    private WindowsTitleBar? FindTitleBar(IntPtr hwnd)
    {
        if (!_subclasses.TryGetValue(hwnd, out var sub)) return null;
        return sub.TitleBar;
    }

    public override void SetInputEnabled(IntPtr hwnd, bool enabled)
    {
        if (hwnd == IntPtr.Zero) return;
        EnableWindow(hwnd, enabled);
    }

    public override void SetOwner(IntPtr hwnd, IntPtr owner)
    {
        if (hwnd == IntPtr.Zero) return;
        SetWindowLongPtr(hwnd, GWLP_HWNDPARENT, owner);
    }

    private Subclass Hook(IntPtr hwnd)
    {
        if (_subclasses.TryGetValue(hwnd, out var existing))
            return existing;
        var sub = new Subclass();
        sub.NewProc = (h, msg, w, l) => SubclassProc(sub, h, msg, w, l);
        sub.OriginalProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(sub.NewProc));
        _subclasses[hwnd] = sub;
        return sub;
    }

    private void UnhookIfUnused(IntPtr hwnd, Subclass sub)
    {
        if (!sub.IsUnused) return;
        SetWindowLongPtr(hwnd, GWLP_WNDPROC, sub.OriginalProc);
        _subclasses.Remove(hwnd);
    }

    private static IntPtr SubclassProc(Subclass sub, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // A non-client press dismisses any open menu, then falls through so the window still drags /
        // resizes / minimizes as usual. Fires before DefWindowProc's modal move loop, so the dismissal
        // is requested at the moment the frame is grabbed.
        if (sub.NonClientPress != null && msg is WM_NCLBUTTONDOWN or WM_NCRBUTTONDOWN)
            sub.NonClientPress();
        if (sub.OutsideClick != null && RedirectOutsideClick(sub.OutsideClick, hwnd, msg, wParam, lParam))
            return IntPtr.Zero;
        if (sub.TitleBar != null && sub.TitleBar.Handle(sub.OriginalProc, msg, wParam, lParam, out var result))
            return result;
        return CallWindowProc(sub.OriginalProc, hwnd, msg, wParam, lParam);
    }

    /// <summary>Hands a press outside the capturing popup to the window under it, after telling
    /// <paramref name="onOutsideClick"/>; true when the press was outside.</summary>
    private static bool RedirectOutsideClick(Action<PointI> onOutsideClick, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_LBUTTONDOWN:
            case WM_RBUTTONDOWN:
            case WM_MBUTTONDOWN:
            case WM_NCLBUTTONDOWN:
            case WM_NCRBUTTONDOWN:
            {
                var screen = ScreenPointFromLParam(lParam, hwnd, msg);
                var gotRect = GetWindowRectStruct(hwnd, out var r);
                var insideRect = gotRect && ContainsPoint(r, screen);
                if (insideRect)
                    return false;
                onOutsideClick(screen);
                // SetCapture routed this click to the popup hwnd. Re-route
                // to the window actually under the cursor so the underlying
                // control (e.g. another repo row) still receives the press.
                // Without this, a click outside the popup only dismisses
                // the menu and is otherwise lost.
                ReleaseCapture();
                var pt = new POINT { X = screen.X, Y = screen.Y };
                var target = WindowFromPoint(pt);
                if (target != IntPtr.Zero && target != hwnd)
                {
                    ScreenToClient(target, ref pt);
                    var lp = (IntPtr)(((pt.Y & 0xFFFF) << 16) | (pt.X & 0xFFFF));
                    PostMessage(target, msg, wParam, lp);
                }
                return true;
            }
            case WM_CANCELMODE:
                onOutsideClick(new PointI(int.MinValue, int.MinValue));
                return false;
            default:
                return false;
        }
    }

    private static PointI ScreenPointFromLParam(IntPtr lParam, IntPtr hwnd, uint msg)
    {
        var x = (short)(lParam.ToInt64() & 0xFFFF);
        var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        // NC messages already pass screen coords; client messages pass client coords.
        if (msg is WM_NCLBUTTONDOWN or WM_NCRBUTTONDOWN)
            return new PointI(x, y);

        var pt = new POINT { X = x, Y = y };
        ClientToScreen(hwnd, ref pt);
        return new PointI(pt.X, pt.Y);
    }

    private static bool ContainsPoint(RECT r, PointI p) =>
        p.X >= r.Left && p.X < r.Right && p.Y >= r.Top && p.Y < r.Bottom;

    private static bool GetWindowRectStruct(IntPtr hwnd, out RECT r)
    {
        var ok = GetWindowRect(hwnd, out r);
        return ok;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCapture(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hwnd, ref POINT pt);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT pt);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hwnd, ref POINT pt);

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(IntPtr hwnd, bool enabled);
}

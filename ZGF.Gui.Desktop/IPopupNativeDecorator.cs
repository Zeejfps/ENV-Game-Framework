using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

public interface IPopupNativeDecorator
{
    void DecoratePopup(IntPtr nativeWindowHandle);
    // Pooled popup windows are reused across kinds: a tooltip is mouse-pass-through, a context
    // menu is not. The native click-through attribute (macOS ignoresMouseEvents / Win32
    // WS_EX_TRANSPARENT) must therefore be re-applied on every acquire to match the current
    // popup. Without this a window first used as a tooltip stays click-through forever, so a
    // later menu reusing it gets hover (poll-based) but every click passes through it untouched.
    void SetMousePassThrough(IntPtr nativeWindowHandle, bool passThrough);
    void BeginCapture(IntPtr nativeWindowHandle, Action<PointI> onOutsideClick);
    void EndCapture(IntPtr nativeWindowHandle);
    // onOutsideClick is supplied explicitly so the target window always gets a
    // callback installed even when the source's capture state is missing (e.g.
    // it was released by a prior race).
    void TransferCapture(IntPtr fromHandle, IntPtr toHandle, Action<PointI> onOutsideClick);
    // Watch a host (non-popup) window's native frame for the lifetime of the window: while a context
    // menu is open, a press on this window's non-client area (title bar, borders, caption buttons) is
    // outside the menu and must dismiss it. GLFW surfaces only client-area mouse events, so without
    // this a title-bar grab on the menu's own host window — which changes neither focus nor client
    // input — leaves the menu open. onNonClientPress fires for each such press; the decorator no-ops
    // where the platform has no native frame hook.
    void WatchWindowNonClientPress(IntPtr nativeWindowHandle, Action onNonClientPress);
    void UnwatchWindow(IntPtr nativeWindowHandle);
}

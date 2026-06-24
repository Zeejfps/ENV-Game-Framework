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
}

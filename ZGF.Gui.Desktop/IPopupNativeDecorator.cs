using ZGF.Geometry;

namespace ZGF.Gui;

public interface IPopupNativeDecorator
{
    void DecoratePopup(IntPtr nativeWindowHandle, bool mousePassThrough);
    void BeginCapture(IntPtr nativeWindowHandle, Action<PointI> onOutsideClick);
    void EndCapture(IntPtr nativeWindowHandle);
    // onOutsideClick is supplied explicitly so the target window always gets a
    // callback installed even when the source's capture state is missing (e.g.
    // it was released by a prior race).
    void TransferCapture(IntPtr fromHandle, IntPtr toHandle, Action<PointI> onOutsideClick);
}

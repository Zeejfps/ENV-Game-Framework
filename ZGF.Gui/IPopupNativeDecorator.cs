using ZGF.Geometry;

namespace ZGF.Gui;

public interface IPopupNativeDecorator
{
    void DecoratePopup(IntPtr nativeWindowHandle, bool mousePassThrough);
    void BeginCapture(IntPtr nativeWindowHandle, Action<PointI> onOutsideClick);
    void EndCapture(IntPtr nativeWindowHandle);
    void TransferCapture(IntPtr fromHandle, IntPtr toHandle);
}

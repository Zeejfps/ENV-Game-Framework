namespace ZGF.Gui.Desktop;

internal sealed class DefaultNoopDecorator : IPopupNativeDecorator
{
    public void DecoratePopup(IntPtr handle, bool mousePassThrough) { }
    public void BeginCapture(IntPtr handle, Action<ZGF.Geometry.PointI> cb) { }
    public void EndCapture(IntPtr handle) { }
    public void TransferCapture(IntPtr from, IntPtr to, Action<ZGF.Geometry.PointI> cb) { }
}
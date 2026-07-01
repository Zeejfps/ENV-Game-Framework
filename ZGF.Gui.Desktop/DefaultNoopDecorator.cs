namespace ZGF.Gui.Desktop;

internal sealed class DefaultNoopDecorator : IPopupNativeDecorator
{
    public void DecoratePopup(IntPtr handle) { }
    public void SetMousePassThrough(IntPtr handle, bool passThrough) { }
    public void BeginCapture(IntPtr handle, Action<ZGF.Geometry.PointI> cb) { }
    public void EndCapture(IntPtr handle) { }
    public void TransferCapture(IntPtr from, IntPtr to, Action<ZGF.Geometry.PointI> cb) { }
    public void WatchWindowNonClientPress(IntPtr handle, Action onNonClientPress) { }
    public void UnwatchWindow(IntPtr handle) { }
}
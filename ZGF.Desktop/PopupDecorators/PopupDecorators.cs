using ZGF.Geometry;

namespace ZGF.Desktop;

/// <summary>The popup decorators that ship with the framework.</summary>
public static class PopupDecorator
{
    /// <summary>The current platform's decorator: popups stay out of the taskbar and window cycle
    /// and never take activation. A no-op where the platform has none.</summary>
    public static IPopupNativeDecorator ForCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPopupDecorator();
        if (OperatingSystem.IsMacOS())
            return new MacOsPopupDecorator();
        return new NoopPopupDecorator();
    }
}

internal sealed class NoopPopupDecorator : IPopupNativeDecorator
{
    public void DecoratePopup(IntPtr nativeWindowHandle) { }
    public void SetMousePassThrough(IntPtr nativeWindowHandle, bool passThrough) { }
    public void BeginCapture(IntPtr nativeWindowHandle, Action<PointI> onOutsideClick) { }
    public void EndCapture(IntPtr nativeWindowHandle) { }
    public void TransferCapture(IntPtr fromHandle, IntPtr toHandle, Action<PointI> onOutsideClick) { }
    public void WatchWindowNonClientPress(IntPtr nativeWindowHandle, Action onNonClientPress) { }
    public void UnwatchWindow(IntPtr nativeWindowHandle) { }
}

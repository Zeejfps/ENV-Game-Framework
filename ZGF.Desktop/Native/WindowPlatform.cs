using ZGF.Geometry;

namespace ZGF.Desktop;

/// <summary>The window calls GLFW does not expose, for the platform the app runs on. Each is a no-op
/// where the platform has no counterpart.</summary>
internal class WindowPlatform
{
    public static readonly WindowPlatform Current = ForThisPlatform();

    private static WindowPlatform ForThisPlatform()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsWindowPlatform();
        if (OperatingSystem.IsMacOS())
            return new MacOsWindowPlatform();
        if (OperatingSystem.IsLinux())
            return new LinuxWindowPlatform();
        return new WindowPlatform();
    }

    /// <summary>Keeps a popup out of the taskbar and the window cycle, and from taking activation.</summary>
    public virtual void DecoratePopup(IntPtr window) { }

    public virtual void CapturePointer(IntPtr window, Action<PointI> onOutsideClick) { }

    public virtual void ReleasePointerCapture(IntPtr window) { }

    public virtual void WatchNonClientPress(IntPtr window, Action onNonClientPress) { }

    public virtual void UnwatchNonClientPress(IntPtr window) { }

    public virtual void SetTitleBarTheme(IntPtr window, bool dark) { }

    public virtual bool CanExtendIntoTitleBar => false;

    public virtual void ExtendIntoTitleBar(IntPtr window, TitleBarContent? content) { }

    public virtual RectI CaptionButtons(IntPtr window) => default;

    /// <summary>How far the content reaches above where the window's frame, as GLFW computes it, puts
    /// it: the height of the title bar the content extends into, and 0 while it does not.</summary>
    public virtual int TitleBarOverlap(IntPtr window) => 0;

    public virtual void SetInputEnabled(IntPtr window, bool enabled) { }

    public virtual void SetOwner(IntPtr window, IntPtr owner) { }
}

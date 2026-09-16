using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Desktop;

namespace ZGF.Gui.Desktop.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsWindowModality : IWindowModality
{
    private const int GWLP_HWNDPARENT = -8;

    public void SetInputEnabled(IWindow window, bool enabled)
    {
        if (window.NativeHandle != IntPtr.Zero) EnableWindow(window.NativeHandle, enabled);
    }

    public void SetOwner(IWindow window, IWindow owner)
    {
        if (window.NativeHandle != IntPtr.Zero)
            SetWindowLongPtr(window.NativeHandle, GWLP_HWNDPARENT, owner.NativeHandle);
    }

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(IntPtr hwnd, bool enabled);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}

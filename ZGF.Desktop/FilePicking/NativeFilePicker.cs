using System.Runtime.InteropServices;
using GLFW;
using ZGF.Desktop.Backends;
using ZGF.Desktop.FilePicking;

namespace ZGF.Desktop;

public static class NativeFilePicker
{
    /// <summary>
    /// The file/folder picker for the current OS: the Windows COM dialog, the macOS
    /// <c>osascript</c> chooser, or on Linux the desktop's own dialog through the XDG desktop portal
    /// (falling back to <c>zenity</c>, <c>kdialog</c> or <c>yad</c>), else a
    /// <see cref="NoopFilePicker"/>. The macOS and Linux pickers wait on a worker thread and hand
    /// their result to <paramref name="postToUiThread"/>, which must run it on the UI thread.
    /// </summary>
    /// <param name="owner">The window the dialogs belong to: they stay above it, open centred on
    /// it, and block input to it while open. Null leaves them unowned. Honoured on Windows and on
    /// Linux through the portal under X11 (XWayland included); the macOS chooser and the Linux
    /// command-line fallbacks run in another process and cannot be owned. On Windows an owner needs
    /// the calling thread to be an STA, as a <c>[STAThread]</c> Main makes the UI thread; on an MTA
    /// this throws.</param>
    public static IFilePicker Create(Action<Action> postToUiThread, IWindow? owner)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsFilePicker(Win32Owner(owner));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsFilePicker(postToUiThread);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxFilePicker(postToUiThread, PortalParent(owner));
        return new NoopFilePicker();
    }

    private static IntPtr Win32Owner(IWindow? owner) =>
        owner is GlfwWindowBase window ? window.NativeHandle : IntPtr.Zero;

    /// <summary>
    /// <paramref name="owner"/> as the portal's parent-window string, <c>x11:&lt;hex id&gt;</c>, or
    /// empty when it has no X11 window. A Wayland window is named to the portal by an xdg-foreign
    /// export handle, which GLFW does not expose, so one is left unparented.
    /// </summary>
    private static string PortalParent(IWindow? owner)
    {
        if (owner is not GlfwWindowBase window) return "";
        if (Glfw.GetPlatform() != Platform.X11) return "";

        var xid = window.NativeHandle;
        return xid == IntPtr.Zero ? "" : $"x11:{xid:x}";
    }
}

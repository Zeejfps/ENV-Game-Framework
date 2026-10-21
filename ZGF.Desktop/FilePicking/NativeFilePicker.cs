using System.Runtime.InteropServices;
using ZGF.Desktop.FilePicking;

namespace ZGF.Desktop;

public static class NativeFilePicker
{
    /// <summary>
    /// The file/folder picker for the current OS: the Windows COM dialog, the macOS
    /// <c>osascript</c> chooser, or the Linux <c>zenity</c>/<c>kdialog</c> chooser, else a
    /// <see cref="NoopFilePicker"/>. The macOS and Linux pickers wait on a worker thread and hand
    /// their result to <paramref name="postToUiThread"/>, which must run it on the UI thread.
    /// </summary>
    public static IFilePicker Create(Action<Action> postToUiThread)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsFilePicker();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsFilePicker(postToUiThread);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxFilePicker(postToUiThread);
        return new NoopFilePicker();
    }
}

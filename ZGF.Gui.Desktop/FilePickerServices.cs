using System.Runtime.InteropServices;
using ZGF.Gui.Desktop.Platforms.Linux;
using ZGF.Gui.Desktop.Platforms.Osx;
using ZGF.Gui.Desktop.Platforms.Windows;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Registers the OS-native <see cref="IFilePicker"/> on a <see cref="Context"/>. The macOS and
/// Linux pickers post their result through the context's <c>IUiDispatcher</c>, so this is opt-in
/// rather than a default: call it once during app setup, before resolving <c>IFilePicker</c>.
/// </summary>
public static class FilePickerServices
{
    extension(Context context)
    {
        /// <summary>
        /// Registers the file/folder picker for the current OS: the Windows COM open dialog,
        /// the macOS <c>osascript</c> chooser, or the Linux <c>zenity</c>/<c>kdialog</c> chooser.
        /// Falls back to a <see cref="NoopFilePicker"/> on any other platform.
        /// </summary>
        public void AddNativeFilePicker()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                context.AddService<IFilePicker>(new WindowsFilePicker());
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                context.AddService<IFilePicker>(new MacOsFilePicker(context));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                context.AddService<IFilePicker>(new LinuxFilePicker(context));
            else
                context.AddService<IFilePicker>(new NoopFilePicker());
        }
    }
}

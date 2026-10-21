using ZGF.Desktop;
using ZGF.Observable;

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
        /// Registers <see cref="NativeFilePicker.Create"/>'s picker for the current OS, posting
        /// results through the context's <c>IUiDispatcher</c>.
        /// </summary>
        public void AddNativeFilePicker() =>
            context.AddService(NativeFilePicker.Create(action => context.Require<IUiDispatcher>().Post(action)));
    }
}

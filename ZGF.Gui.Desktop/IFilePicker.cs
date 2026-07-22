namespace ZGF.Gui.Desktop;

/// <summary>
/// Shows the OS-native file and folder open dialogs. Register a platform implementation with
/// <see cref="FilePickerServices.AddNativeFilePicker"/>, then resolve <c>IFilePicker</c> from the
/// <see cref="Context"/> where a Browse button or "Open…" action needs one.
/// </summary>
public interface IFilePicker
{
    /// <summary>
    /// Shows the OS folder picker without blocking the UI thread, then invokes
    /// <paramref name="onPicked"/> on the UI thread with the chosen path. Not invoked when the
    /// user cancels or no picker exists.
    /// </summary>
    void PickFolder(string title, Action<string> onPicked);

    /// <summary>
    /// Shows the OS file picker, following the same threading and cancel contract as
    /// <see cref="PickFolder"/>. Opens at <paramref name="initialDirectory"/> when it is a real
    /// folder; ignored otherwise. <paramref name="filters"/> restricts the visible files; null
    /// or empty shows everything. No OS adds an "All files" escape hatch on its own — append a
    /// <see cref="FileFilter"/> with pattern <c>*.*</c> if the user should be able to opt out.
    /// </summary>
    void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked);
}

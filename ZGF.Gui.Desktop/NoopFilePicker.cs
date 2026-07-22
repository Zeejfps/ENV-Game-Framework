namespace ZGF.Gui.Desktop;

/// <summary>
/// Fallback <see cref="IFilePicker"/> for platforms without a native picker. Logs the request
/// and never invokes the callback, so a Browse button is a harmless no-op rather than a crash.
/// </summary>
public sealed class NoopFilePicker : IFilePicker
{
    public void PickFolder(string title, Action<string> onPicked) =>
        Console.WriteLine($"[FilePicker] No native folder picker for this OS. Title: {title}");

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Console.WriteLine($"[FilePicker] No native file picker for this OS. Title: {title}");
}

namespace GitGui;

public sealed class NoopFolderPicker : IFolderPicker
{
    public string? PickFolder(string title)
    {
        Console.WriteLine($"[FolderPicker] No native picker for this OS. Title: {title}");
        return null;
    }
}

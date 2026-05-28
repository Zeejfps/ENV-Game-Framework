namespace GitGui;

public interface IStashView
{
    string Message { get; }
    bool KeepStaged { get; }
    IReadOnlyList<string> SelectedPaths { get; }
    bool StashEnabled { set; }
    string? ErrorMessage { set; }
    event Action MessageChanged;
    event Action SelectionChanged;
    event Action StashRequested;
    void SetFiles(IReadOnlyList<StashFileRow> files, IReadOnlyList<string> preChecked);
    void FocusMessage();
    void Close();
}

public readonly record struct StashRequest(Repo Repo);

public sealed record StashFileRow(string Path, FileChange Display, bool IsUntracked);

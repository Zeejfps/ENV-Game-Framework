namespace GitGui;

public interface ILocalChangesView
{
    void ShowPlaceholder(string text);
    void ShowSnapshot(IReadOnlyList<FileChange> unstaged, IReadOnlyList<FileChange> staged);
    void SetStagedFiles(IReadOnlyList<FileChange> files);

    string TitleText { get; set; }
    string DescriptionText { get; set; }
    bool AmendChecked { get; set; }
    bool CommitEnabled { set; }
    string? OpError { set; }

    void SelectUnstaged(IReadOnlyList<string> paths);
    void SelectStaged(IReadOnlyList<string> paths);

    event Action<IReadOnlyList<string>> StageRequested;
    event Action<IReadOnlyList<string>> UnstageRequested;
    event Action TitleChanged;
    event Action AmendToggled;
    event Action CommitClicked;
}

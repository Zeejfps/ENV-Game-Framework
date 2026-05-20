namespace GitGui;

public interface IRenameBranchView
{
    string Name { get; }
    bool Force { get; }
    bool RenameEnabled { set; }
    string? ErrorMessage { set; }
    event Action NameChanged;
    event Action RenameRequested;
    void FocusName();
    void Close();
}

public readonly record struct RenameBranchRequest(Repo Repo, string CurrentName);

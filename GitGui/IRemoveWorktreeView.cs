namespace GitGui;

public interface IRemoveWorktreeView
{
    bool Force { get; }
    bool RemoveEnabled { set; }
    string? ErrorMessage { set; }
    event Action RemoveRequested;
    void Close();
}

public readonly record struct RemoveWorktreeRequest(Repo Primary, Repo Worktree);

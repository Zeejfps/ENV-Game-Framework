namespace GitGui;

public interface IRebaseBranchView
{
    bool Autostash { get; }
    bool RebaseEnabled { set; }
    string? ErrorMessage { set; }
    RebasePreviewState PreviewState { set; }
    event Action RebaseRequested;
    void Close();
}

public readonly record struct RebaseBranchRequest(
    Repo Repo,
    string SourceBranch,
    string TargetRef,
    string TargetDisplay);

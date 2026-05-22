namespace GitGui;

public interface IDeleteLocalBranchView
{
    bool Force { get; }
    bool DeleteRemote { get; }
    bool DeleteEnabled { set; }
    bool CancelEnabled { set; }
    string? ErrorMessage { set; }
    bool IsBusy { set; }
    float BusyRotation { set; }

    event Action DeleteRequested;
    void Close();
}

public readonly record struct DeleteLocalBranchRequest(
    Repo Repo,
    string BranchName,
    string? UpstreamRemote = null,
    string? UpstreamBranch = null);

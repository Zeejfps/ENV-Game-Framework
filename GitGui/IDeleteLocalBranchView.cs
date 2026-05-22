namespace GitGui;

public interface IDeleteLocalBranchView
{
    bool Force { get; }
    bool DeleteRemote { get; }
    bool DeleteEnabled { set; }
    bool CancelEnabled { set; }
    string? ErrorMessage { set; }

    // While the delete runs, spin a loader icon inside the confirm button so the user
    // can tell something is happening — the remote half (`git push --delete`) is a
    // network op that can take a few seconds.
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

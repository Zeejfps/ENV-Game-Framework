namespace GitGui;

public interface IPublishBranchView
{
    string SelectedRemote { get; }
    bool SetUpstream { get; }
    bool PublishEnabled { set; }
    string? ErrorMessage { set; }
    void SetRemotes(IReadOnlyList<string> remotes);
    event Action PublishRequested;
    void Close();
}

public readonly record struct PublishBranchRequest(Repo Repo, string LocalBranch);

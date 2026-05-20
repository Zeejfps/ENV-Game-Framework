namespace GitGui;

public interface IDeleteRemoteBranchView
{
    bool DeleteEnabled { set; }
    string? ErrorMessage { set; }
    event Action DeleteRequested;
    void Close();
}

public readonly record struct DeleteRemoteBranchRequest(Repo Repo, string RemoteName, string BranchName);

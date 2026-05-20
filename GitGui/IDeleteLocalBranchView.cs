namespace GitGui;

public interface IDeleteLocalBranchView
{
    bool Force { get; }
    bool DeleteEnabled { set; }
    string? ErrorMessage { set; }
    event Action DeleteRequested;
    void Close();
}

public readonly record struct DeleteLocalBranchRequest(Repo Repo, string BranchName);

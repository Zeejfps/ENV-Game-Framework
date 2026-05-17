namespace GitGui;

public interface IGitService
{
    CommitSnapshot Load(Repo repo, int cap);
    CommitDetails LoadDetails(Repo repo, string sha);
    LocalChangesSnapshot GetLocalChanges(Repo repo);
    BranchListing GetBranches(Repo repo);
    void Stage(Repo repo, IReadOnlyList<string> paths);
    void Unstage(Repo repo, IReadOnlyList<string> paths);
    string? Commit(Repo repo, string message, bool amend);
    HeadCommitMessage? GetHeadCommitMessage(Repo repo);
    PushStatus GetPushStatus(Repo repo);
    PushOutcome Push(Repo repo);
    DiffResult GetDiff(Repo repo, string path, DiffSide side);
}

public sealed record HeadCommitMessage(string Title, string Description);

public sealed record PushStatus(
    string? CurrentBranchName,
    bool HasUpstream,
    int Ahead,
    int Behind,
    bool IsDetached);

public sealed record PushOutcome(bool Success, string? ErrorMessage);

namespace GitGui;

public interface IGitService
{
    CommitSnapshot Load(Repo repo, int cap);
    CommitDetails LoadDetails(Repo repo, string sha);
    LocalChangesSnapshot GetLocalChanges(Repo repo);
    BranchListing GetBranches(Repo repo);
    void Stage(Repo repo, IReadOnlyList<string> paths);
    void Unstage(Repo repo, IReadOnlyList<string> paths);
    void ResetToParent(Repo repo, IReadOnlyList<string> paths);
    string? DiscardChanges(Repo repo, IReadOnlyList<string> paths);
    string? Commit(Repo repo, string message, bool amend);
    HeadCommitMessage? GetHeadCommitMessage(Repo repo);
    IReadOnlyList<FileChange> GetHeadCommitFiles(Repo repo);
    PushStatus GetPushStatus(Repo repo);
    PushOutcome Push(Repo repo);
    PullOutcome Pull(Repo repo);
    FetchOutcome Fetch(Repo repo);
    CheckoutOutcome CheckoutLocalBranch(Repo repo, string branchName);
    CheckoutOutcome CheckoutRemoteBranch(Repo repo, string localName, string remoteName, string remoteBranchName, bool track);
    CreateBranchOutcome CreateBranch(Repo repo, string name, string startPoint, bool checkout);
    StashOutcome CreateStash(Repo repo, string message, bool includeUntracked, bool keepIndex);
    StashOutcome ApplyStash(Repo repo, int index);
    StashOutcome DropStash(Repo repo, int index);
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

public sealed record PullOutcome(bool Success, string? ErrorMessage);

public sealed record FetchOutcome(bool Success, string? ErrorMessage);

public sealed record CheckoutOutcome(bool Success, string? ErrorMessage);

public sealed record CreateBranchOutcome(bool Success, string? ErrorMessage);

public sealed record StashOutcome(bool Success, string? ErrorMessage);

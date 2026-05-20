using System.Text.Json.Serialization;

namespace GitGui;

public sealed record Repo(
    Guid Id,
    string Path,
    string DisplayName,
    Guid? ParentRepoId = null)
{
    [JsonIgnore]
    public bool IsMissing { get; init; }

    // Best-known branch this checkout points at. Refreshed by WorktreeSyncService from
    // `git worktree list`. Null on a detached HEAD or before the first discovery pass.
    // Persisted so a freshly-launched app shows the right "taken branches" set before
    // the background sync completes.
    public string? Branch { get; init; }

    [JsonIgnore]
    public bool IsWorktree => ParentRepoId is not null;
}

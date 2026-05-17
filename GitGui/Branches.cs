namespace GitGui;

public sealed record BranchEntry(string Name, string TipSha, bool IsHead);

public sealed record RemoteGroup(string Name, IReadOnlyList<BranchEntry> Branches);

public sealed record BranchListing(
    Guid RepoId,
    IReadOnlyList<BranchEntry> LocalBranches,
    IReadOnlyList<RemoteGroup> Remotes,
    string? ErrorMessage)
{
    public static BranchListing Empty(Guid repoId)
        => new(repoId, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), null);
}

/// Persisted per-repo state for the branches sidebar. Missing keys default to all-open.
public sealed class BranchesUiState
{
    public bool LocalOpen { get; set; } = true;
    public bool RemotesOpen { get; set; } = true;
    public Dictionary<string, bool> RemoteOpen { get; set; } = new();

    public BranchesUiState Clone() => new()
    {
        LocalOpen = LocalOpen,
        RemotesOpen = RemotesOpen,
        RemoteOpen = new Dictionary<string, bool>(RemoteOpen),
    };
}

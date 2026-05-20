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

    [JsonIgnore]
    public bool IsWorktree => ParentRepoId is not null;
}

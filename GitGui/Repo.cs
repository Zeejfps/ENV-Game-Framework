using System.Text.Json.Serialization;

namespace GitGui;

public sealed record Repo(
    Guid Id,
    string Path,
    string DisplayName)
{
    [JsonIgnore]
    public bool IsMissing { get; init; }
}

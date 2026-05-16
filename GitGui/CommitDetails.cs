namespace GitGui;

public enum FileChangeStatus
{
    Added,
    Modified,
    Deleted,
    Renamed,
    Copied,
    TypeChanged,
    Unmodified,
}

public sealed record FileChange(
    string Path,
    string? OldPath,
    FileChangeStatus Status);

public sealed record CommitDetails(
    Guid RepoId,
    string Sha,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset AuthorWhen,
    string CommitterName,
    string CommitterEmail,
    DateTimeOffset CommitterWhen,
    string Message,
    string MessageShort,
    IReadOnlyList<string> ParentShas,
    IReadOnlyList<FileChange> Files,
    string? ErrorMessage);

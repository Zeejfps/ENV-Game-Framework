namespace GitGui;

internal readonly record struct LocalChangesState(
    string Title,
    string Description,
    bool Amend,
    string? Placeholder,
    IReadOnlyList<FileChange> Unstaged,
    IReadOnlyList<FileChange> Staged,
    string? OpError)
{
    public const string OpenRepoPlaceholder = "Open a repository to see local changes.";
    public const string LoadingPlaceholder = "Loading…";

    public static LocalChangesState Initial { get; } = new(
        Title: string.Empty,
        Description: string.Empty,
        Amend: false,
        Placeholder: OpenRepoPlaceholder,
        Unstaged: [],
        Staged: [],
        OpError: null);
    
    public bool CommitEnabled =>
        !string.IsNullOrWhiteSpace(Title) && (Amend || Staged.Count > 0);
}
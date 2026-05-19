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

    // Amend can be a message-only edit of the previous commit, so it doesn't need
    // anything staged; a regular commit does.
    public bool CommitEnabled =>
        HasNonWhitespace(Title) && (Amend || Staged.Count > 0);

    private static bool HasNonWhitespace(string s)
    {
        foreach (var ch in s)
            if (!char.IsWhiteSpace(ch)) return true;
        return false;
    }
}
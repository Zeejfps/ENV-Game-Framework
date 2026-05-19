namespace GitGui;

internal readonly record struct LocalChangesState(
    string Title,
    string Description,
    bool Amend,
    bool HasRepo,
    bool IsLoading,
    string? LoadError,
    IReadOnlyList<FileChange> Unstaged,
    IReadOnlyList<FileChange> Staged,
    Selection Selection,
    string? OpError)
{
    public const string OpenRepoPlaceholder = "Open a repository to see local changes.";
    public const string LoadingPlaceholder = "Loading…";

    public static LocalChangesState Initial { get; } = new(
        Title: string.Empty,
        Description: string.Empty,
        Amend: false,
        HasRepo: false,
        IsLoading: false,
        LoadError: null,
        Unstaged: [],
        Staged: [],
        Selection: Selection.Empty,
        OpError: null);

    // Placeholder is derived, not settable. Loading never tears the panels down when
    // there is data on screen — that's reserved for "nothing to render at all"
    // (no repo, hard load error, or a cold start with empty lists while loading).
    // Splitting lifecycle (IsLoading / LoadError / HasRepo) from data (Staged / Unstaged)
    // makes the "Loading shown while data exists" state unrepresentable.
    public string? Placeholder =>
        !HasRepo ? OpenRepoPlaceholder :
        LoadError != null ? LoadError :
        (Staged.Count == 0 && Unstaged.Count == 0 && IsLoading) ? LoadingPlaceholder :
        null;

    public bool CommitEnabled =>
        !string.IsNullOrWhiteSpace(Title) && (Amend || Staged.Count > 0);
}

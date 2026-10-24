namespace ZGF.ILSpy;

/// <summary>What toggling the bookmark on a line did.</summary>
public abstract record BookmarkToggle
{
    private BookmarkToggle()
    {
    }

    /// <summary>Nothing: the line cannot be bookmarked.</summary>
    public sealed record Unchanged : BookmarkToggle;

    /// <summary>Bookmarked the line with <see cref="Bookmark"/>.</summary>
    public sealed record Added(Bookmark Bookmark) : BookmarkToggle;

    /// <summary>Removed <see cref="Bookmark"/>, which was in <see cref="Row"/> of the list.</summary>
    public sealed record Removed(Bookmark Bookmark, int Row) : BookmarkToggle;
}

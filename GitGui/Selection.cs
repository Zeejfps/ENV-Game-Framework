namespace GitGui;

/// <summary>
/// Local-changes selection state, owned by <see cref="LocalChangesViewModel"/> and
/// rendered by both the row panels (highlight) and the diff view (target). A list of
/// <see cref="DiffTarget"/> entries (path + side) plus an anchor for shift-click range
/// extension.
///
/// <see cref="Create"/> is the only constructor; it drops any item or anchor whose
/// path isn't in the corresponding side's file list, so an external reload (watcher
/// tick, refs change, terminal git op) can't strand the selection on a path that's
/// gone. The all-same-side property holds by construction at every call site (clicks
/// arrive one row at a time; ops always move paths to a single side), so the type
/// doesn't re-check it at runtime.
/// </summary>
internal sealed class Selection
{
    public IReadOnlyList<DiffTarget> Items { get; }
    public DiffTarget? Anchor { get; }
    public DiffTarget? Cursor { get; }

    private readonly HashSet<DiffTarget> _itemSet;

    public static readonly Selection Empty = new(Array.Empty<DiffTarget>(), null, null);

    public int Count => Items.Count;

    /// <summary>The single selected target, or null when the selection is empty or multi.</summary>
    public DiffTarget? Single => Items.Count == 1 ? Items[0] : null;

    private Selection(IReadOnlyList<DiffTarget> items, DiffTarget? anchor, DiffTarget? cursor)
    {
        Items = items;
        Anchor = anchor;
        Cursor = cursor;
        _itemSet = new HashSet<DiffTarget>(items);
    }

    public bool Contains(string path, DiffSide side)
        => _itemSet.Contains(new DiffTarget(path, side));

    /// <summary>
    /// Paths currently selected on <paramref name="side"/>, in selection order. Empty
    /// when the selection lives on the other side.
    /// </summary>
    public IReadOnlyList<string> PathsOn(DiffSide side)
        => Items.Count > 0 && Items[0].Side == side
            ? Items.Select(t => t.Path).ToList()
            : Array.Empty<string>();

    /// <summary>
    /// Builds a selection, pruning items and anchor whose paths aren't in the
    /// corresponding side's file list.
    /// </summary>
    public static Selection Create(
        IReadOnlyList<DiffTarget> items,
        DiffTarget? anchor,
        DiffTarget? cursor,
        IReadOnlyList<FileChange> unstaged,
        IReadOnlyList<FileChange> staged)
    {
        if (items.Count == 0 && anchor == null && cursor == null) return Empty;

        var unstagedPaths = BuildPathSet(unstaged);
        var stagedPaths = BuildPathSet(staged);

        var pruned = new List<DiffTarget>(items.Count);
        foreach (var t in items)
        {
            var available = t.Side == DiffSide.Unstaged ? unstagedPaths : stagedPaths;
            if (available.Contains(t.Path)) pruned.Add(t);
        }

        DiffTarget? normalizedAnchor = null;
        if (anchor != null)
        {
            var anchorAvailable = anchor.Side == DiffSide.Unstaged ? unstagedPaths : stagedPaths;
            if (anchorAvailable.Contains(anchor.Path)) normalizedAnchor = anchor;
        }

        DiffTarget? normalizedCursor = null;
        if (cursor != null)
        {
            var cursorAvailable = cursor.Side == DiffSide.Unstaged ? unstagedPaths : stagedPaths;
            if (cursorAvailable.Contains(cursor.Path)) normalizedCursor = cursor;
        }

        return pruned.Count == 0 && normalizedAnchor == null && normalizedCursor == null
            ? Empty
            : new Selection(pruned, normalizedAnchor, normalizedCursor);
    }

    /// <summary>
    /// Builds a selection from a flat list of paths landing on a single side. Anchor
    /// defaults to the first path. Used by stage/unstage to place the post-op selection
    /// on the destination side.
    /// </summary>
    public static Selection FromPaths(
        IReadOnlyList<string> paths,
        DiffSide side,
        IReadOnlyList<FileChange> unstaged,
        IReadOnlyList<FileChange> staged)
    {
        if (paths.Count == 0) return Empty;
        var items = new List<DiffTarget>(paths.Count);
        foreach (var p in paths) items.Add(new DiffTarget(p, side));
        return Create(items, items[0], items[0], unstaged, staged);
    }

    private static HashSet<string> BuildPathSet(IReadOnlyList<FileChange> files)
    {
        var set = new HashSet<string>(files.Count);
        foreach (var f in files) set.Add(f.Path);
        return set;
    }
}

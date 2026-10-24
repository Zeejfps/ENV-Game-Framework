using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>What the bookmarks panel shows of the bookmarks: the one selected, the cell current in it
/// and the text selected there, and the one being renamed, with the name typed so far.</summary>
public sealed class BookmarksPanel
{
    public BookmarksPanel() => Cells = new TableCellSelection(Selection.Rows);

    public ItemSelection<Bookmark> Selection { get; } = new();

    /// <summary>The cell of the selected bookmark current in the panel, and the text selected in it.</summary>
    public TableCellSelection Cells { get; }

    public Bookmark? Selected => Selection.Item;

    /// <summary>The bookmark whose name is being edited, and the name typed so far.</summary>
    public Bookmark? Renaming { get; private set; }

    public string RenameText = "";

    /// <summary>True once after a rename starts: the name field should take focus.</summary>
    public bool FocusRename { get; set; }

    /// <summary>Selects the bookmark a toggle added, or passes the selection of the one it removed to
    /// the bookmark now in its row.</summary>
    public void Take(BookmarkToggle toggle, IReadOnlyList<Bookmark> bookmarks)
    {
        switch (toggle)
        {
            case BookmarkToggle.Added added:
                Selection.Select(added.Bookmark, bookmarks);
                break;
            case BookmarkToggle.Removed removed:
                TakeRemoved(removed.Bookmark, removed.Row, bookmarks);
                break;
        }
    }

    /// <summary>Removes <paramref name="bookmark"/>, passing its selection to the bookmark now in its row.</summary>
    public void Remove(BookmarkList bookmarks, Bookmark bookmark)
    {
        var row = bookmarks.Remove(bookmark);
        TakeRemoved(bookmark, row, bookmarks.Items);
    }

    private void TakeRemoved(Bookmark bookmark, int row, IReadOnlyList<Bookmark> bookmarks)
    {
        if (Renaming == bookmark)
            Renaming = null;
        if (Selection.Item != bookmark)
            return;
        SelectInRow(row, bookmarks);
    }

    /// <summary>Selects the bookmark in <paramref name="row"/>, or the last when there are fewer.</summary>
    private void SelectInRow(int row, IReadOnlyList<Bookmark> bookmarks)
    {
        var kept = Math.Min(row, bookmarks.Count - 1);
        if (kept < 0)
        {
            Selection.Clear();
            return;
        }
        Selection.Select(bookmarks[kept], bookmarks, reveal: false);
    }

    /// <summary>Goes to the next enabled bookmark after the selected one, or the previous when
    /// <paramref name="direction"/> is negative, and selects it.</summary>
    public void Step(AppState w, int direction)
    {
        var bookmark = w.StepBookmark(Selected, direction);
        if (bookmark == null)
            return;
        Selection.Select(bookmark, w.Bookmarks.Items);
    }

    /// <summary>Forgets the rename of a bookmark that is gone, as importing in its place drops it.</summary>
    public void Follow(IReadOnlyList<Bookmark> bookmarks)
    {
        if (Renaming == null)
            return;
        if (bookmarks.Contains(Renaming))
            return;
        Renaming = null;
    }

    public void StartRename(Bookmark bookmark)
    {
        Renaming = bookmark;
        RenameText = bookmark.Name;
        FocusRename = true;
    }

    /// <summary>Gives the bookmark being renamed the name typed, unless it is blank, and ends the rename.</summary>
    public void CommitRename(BookmarkList bookmarks)
    {
        var bookmark = Renaming;
        Renaming = null;
        if (bookmark == null)
            return;
        bookmarks.Rename(bookmark, RenameText);
    }

    public void CancelRename() => Renaming = null;
}

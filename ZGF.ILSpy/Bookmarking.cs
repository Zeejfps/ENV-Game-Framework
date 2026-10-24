using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Marking lines with bookmarks, going to them, and exporting and importing them.</summary>
public static class Bookmarking
{
    /// <summary>Removes the bookmark marking <paramref name="line"/> of <paramref name="tab"/>'s
    /// document, or bookmarks the line, naming the bookmark by the statement or definition on it
    /// when it has one. Returns which it did.</summary>
    public static BookmarkToggle ToggleBookmark(this AppState w, DocumentTab tab, int line)
    {
        var node = tab.Node;
        if (node == null)
            return new BookmarkToggle.Unchanged();
        if (!CanBookmarkLine(tab, line))
            return new BookmarkToggle.Unchanged();
        var existing = w.Bookmarks.At(tab.Document, node.Path, line);
        if (existing != null)
            return w.RemoveBookmark(existing);
        return w.AddBookmark(tab, node, line);
    }

    private static BookmarkToggle RemoveBookmark(this AppState w, Bookmark bookmark)
    {
        var row = w.Bookmarks.Remove(bookmark);
        return new BookmarkToggle.Removed(bookmark, row);
    }

    private static bool CanBookmarkLine(DocumentTab tab, int line)
    {
        if (tab.IsBusy)
            return false;
        if (tab.Document.IsError)
            return false;
        if (line < 0)
            return false;
        return line < tab.Document.Lines.Length;
    }

    private static BookmarkToggle AddBookmark(this AppState w, DocumentTab tab, BrowseNode node, int line)
    {
        var anchor = tab.Document.AnchorAt(line);
        var name = w.Bookmarks.NextName(w.FormatBookmarkName);
        var info = ILSpyTree.Describe(node, anchor);
        var bookmark = new Bookmark(node.Path, tab.Language, line, anchor, name, info);
        w.Bookmarks.Add(bookmark);
        return new BookmarkToggle.Added(bookmark);
    }

    private static string FormatBookmarkName(this AppState w, int number) => Strings.For(w.Locale).BookmarksDefaultName(number);

    /// <summary>Shows the line <paramref name="bookmark"/> marks in the active tab, opening its
    /// assembly first if it is not open, and records it in the history.</summary>
    public static async void GoTo(this AppState w, Bookmark bookmark)
    {
        var ticket = w.Intent.Begin();
        var tab = w.Tabs.Active;
        var place = bookmark.Place;
        w.Intent.StartResolving(ticket);
        var found = await Background.Run(() => w.Assemblies.FindNode(place));
        if (w.Intent.IsStale(ticket))
            return;
        w.Intent.StopResolving();
        w.ShowBookmarkPlace(bookmark, found, tab);
    }

    private static void ShowBookmarkPlace(this AppState w, Bookmark bookmark, Outcome<BrowseNode?> found, DocumentTab? tab)
    {
        var node = found.Value;
        if (node == null)
        {
            var t = Strings.For(w.Locale);
            var details = AppState.DescribeFailure(found.Error, t.ErrorNoLongerThere(bookmark.Place));
            w.ReportError(t.ErrorGoToBookmark(bookmark.Name), details);
            return;
        }
        w.LeaveActive();
        w.ShowIn(w.Tabs.KeepIfOpen(tab), node, target: bookmark);
        w.History.Push(node, w.Language);
    }

    /// <summary>Goes to the next enabled bookmark after <paramref name="from"/>, or the previous when
    /// <paramref name="direction"/> is negative. Returns the bookmark gone to, or null when none is enabled.</summary>
    public static Bookmark? StepBookmark(this AppState w, Bookmark? from, int direction)
    {
        var bookmark = w.Bookmarks.Step(from, direction);
        if (bookmark == null)
            return null;
        w.GoTo(bookmark);
        return bookmark;
    }

    public static void ExportBookmarks(this AppState w, string path)
    {
        var records = w.Bookmarks.Save();
        var error = BookmarkFile.Write(path, records);
        if (error == null)
            return;
        w.ReportError(Strings.For(w.Locale).ErrorExportBookmarks, error);
    }

    /// <summary>Reads the bookmarks in the file at <paramref name="path"/> — this app's or the real
    /// ILSpy's — and finds the place of each in the background: takes them at once when there are
    /// none yet, and otherwise waits in <see cref="AppState.Imports"/> for the user to say how. Bookmarks
    /// whose place is not found are left out and reported.</summary>
    public static async void ImportBookmarks(this AppState w, string path)
    {
        var file = Path.GetFileName(path);
        var records = BookmarkFile.Read(path, out var error);
        if (records == null)
        {
            w.ReportError(Strings.For(w.Locale).ErrorImportBookmarks(file), error);
            return;
        }
        w.Imports.StartFinding();
        var found = await Background.Run(() => w.FindPlaces(records));
        w.Imports.StopFinding();
        w.TakeImported(found, file);
    }

    private static List<FoundBookmark> FindPlaces(this AppState w, List<BookmarkRecord> records)
    {
        return records.ConvertAll(r => new FoundBookmark(r, w.FindPlace(r)));
    }

    private static NodePath? FindPlace(this AppState w, BookmarkRecord record)
    {
        if (record.Place != null)
            return NodePath.From(record.FileName, record.Place);
        var treePath = record.ViewState?.SelectedTreeNodePath;
        return ILSpyTree.Locate(w.Assemblies, record.FileName, treePath, record.Token);
    }

    private static void TakeImported(this AppState w, Outcome<List<FoundBookmark>> found, string file)
    {
        var places = found.Value;
        if (places == null)
        {
            w.ReportError(Strings.For(w.Locale).ErrorImportBookmarks(file), found.Error!.ToString());
            return;
        }
        w.ReportLostBookmarks(places, file);
        var bookmarks = places
            .Where(f => f.Place != null)
            .Select(f => Bookmark.FromRecord(f.Record, f.Place!))
            .ToList();
        if (bookmarks.Count == 0)
            return;
        if (w.Bookmarks.Items.Count == 0)
        {
            w.TakeBookmarks(bookmarks, BookmarkImportMode.Replace);
            return;
        }
        w.Imports.Wait(new PendingImport(bookmarks));
    }

    private static void ReportLostBookmarks(this AppState w, List<FoundBookmark> found, string file)
    {
        var lost = found.FindAll(f => f.Place == null);
        if (lost.Count == 0)
            return;
        var lines = lost.Select(DescribeLostBookmark);
        var details = string.Join(Environment.NewLine, lines);
        w.ReportError(Strings.For(w.Locale).ErrorBookmarksNotFound(file), details);
    }

    private static string DescribeLostBookmark(FoundBookmark lost)
    {
        var record = lost.Record;
        var location = record.LocationNodeName ?? record.MemberName;
        return $"{record.Name}: {location} ({record.FileName})";
    }

    /// <summary>Takes the bookmarks <paramref name="import"/> waits with as <paramref name="mode"/> says,
    /// or drops them when null.</summary>
    public static void FinishImport(this AppState w, PendingImport import, BookmarkImportMode? mode)
    {
        if (!w.Imports.Finish(import))
            return;
        if (mode == null)
            return;
        w.TakeBookmarks(import.Bookmarks, mode.Value);
    }

    /// <summary>Takes <paramref name="bookmarks"/> in as <paramref name="mode"/> says. Those marking a
    /// line a bookmark marks already are left out: quietly when merging, and reported when replacing.</summary>
    private static void TakeBookmarks(this AppState w, IReadOnlyList<Bookmark> bookmarks, BookmarkImportMode mode)
    {
        var leftOut = w.Bookmarks.Import(bookmarks, mode);
        if (mode == BookmarkImportMode.Merge)
            return;
        w.ReportLeftOutBookmarks(leftOut);
    }

    /// <summary>Says how many bookmarks were left out because another marks the same line.</summary>
    internal static void ReportLeftOutBookmarks(this AppState w, int leftOut)
    {
        if (leftOut == 0)
            return;
        w.ReportError(Strings.For(w.Locale).ErrorDuplicateBookmarks(leftOut));
    }

    private sealed record FoundBookmark(BookmarkRecord Record, NodePath? Place);
}

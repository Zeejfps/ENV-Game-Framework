using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Marking lines with bookmarks, going to them, and exporting and importing them.</summary>
public static class Bookmarking
{
    /// <summary>Removes the bookmark marking <paramref name="line"/> of <paramref name="tab"/>'s
    /// document, or bookmarks the line, naming the bookmark by the statement or definition on it
    /// when it has one.</summary>
    public static void ToggleBookmark(this Workspace w, DocumentTab tab, int line)
    {
        var node = tab.Node;
        if (node == null)
            return;
        if (!CanBookmarkLine(tab, line))
            return;
        var existing = w.Bookmarks.At(tab.Document, node.Path, line);
        if (existing != null)
        {
            w.Bookmarks.Remove(existing);
            return;
        }
        w.AddBookmark(tab, node, line);
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

    private static void AddBookmark(this Workspace w, DocumentTab tab, BrowseNode node, int line)
    {
        var anchor = tab.Document.AnchorAt(line);
        var name = w.Bookmarks.NextName(w.FormatBookmarkName);
        var info = ILSpyTree.Describe(node, anchor);
        var bookmark = new Bookmark(node.Path, tab.Language, line, anchor, name, info);
        w.Bookmarks.Add(bookmark);
    }

    private static string FormatBookmarkName(this Workspace w, int number) => w.GetStrings().BookmarksDefaultName(number);

    /// <summary>Shows the line <paramref name="bookmark"/> marks in the active tab, opening its
    /// assembly first if it is not open, and records it in the history.</summary>
    public static void GoTo(this Workspace w, Bookmark bookmark)
    {
        var ticket = w.Intent.Begin();
        var tab = w.Tabs.Active;
        var place = bookmark.Place;
        w.Intent.StartResolving(ticket);
        w.RunInBackground(() => w.Assemblies.FindNode(place), (node, error) => w.OnBookmarkPlaceFound(bookmark, node, error, tab, ticket));
    }

    private static void OnBookmarkPlaceFound(this Workspace w, Bookmark bookmark, BrowseNode? node, Exception? error, DocumentTab? tab, int ticket)
    {
        if (w.Intent.IsStale(ticket))
            return;
        w.Intent.StopResolving();
        if (node == null)
        {
            var t = w.GetStrings();
            var details = Workspace.DescribeFailure(error, t.ErrorNoLongerThere(bookmark.Place));
            w.ReportError(t.ErrorGoToBookmark(bookmark.Name), details);
            return;
        }
        w.LeaveActive();
        w.ShowIn(w.Tabs.KeepIfOpen(tab), node, target: bookmark);
        w.History.Push(node, w.Language);
    }

    /// <summary>Goes to the next enabled bookmark after the selected one, or the previous when
    /// <paramref name="direction"/> is negative.</summary>
    public static void StepBookmark(this Workspace w, int direction)
    {
        var bookmark = w.Bookmarks.Step(direction);
        if (bookmark == null)
            return;
        w.GoTo(bookmark);
    }

    public static void ExportBookmarks(this Workspace w, string path)
    {
        var records = w.Bookmarks.Save();
        var error = BookmarkFile.Write(path, records);
        if (error == null)
            return;
        w.ReportError(w.GetStrings().ErrorExportBookmarks, error);
    }

    /// <summary>Reads the bookmarks in the file at <paramref name="path"/> — this app's or the real
    /// ILSpy's — and finds the place of each in the background: takes them at once when there are
    /// none yet, and otherwise asks how through <see cref="Workspace.PendingImport"/>. Bookmarks whose
    /// place is not found are left out and reported.</summary>
    public static void ImportBookmarks(this Workspace w, string path)
    {
        var file = Path.GetFileName(path);
        var records = BookmarkFile.Read(path, out var error);
        if (records == null)
        {
            w.ReportError(w.GetStrings().ErrorImportBookmarks(file), error);
            return;
        }
        w.IsImporting = true;
        w.RunInBackground(() => w.FindPlaces(records), (found, failure) => w.OnBookmarkPlacesFound(found, failure, file));
    }

    private static List<FoundBookmark> FindPlaces(this Workspace w, List<BookmarkRecord> records)
    {
        return records.ConvertAll(r => new FoundBookmark(r, w.FindPlace(r)));
    }

    private static NodePath? FindPlace(this Workspace w, BookmarkRecord record)
    {
        if (record.Place != null)
            return NodePath.From(record.FileName, record.Place);
        var treePath = record.ViewState?.SelectedTreeNodePath;
        return ILSpyTree.Locate(w.Assemblies, record.FileName, treePath, record.Token);
    }

    private static void OnBookmarkPlacesFound(this Workspace w, List<FoundBookmark>? found, Exception? failure, string file)
    {
        w.IsImporting = false;
        if (failure != null)
        {
            w.ReportError(w.GetStrings().ErrorImportBookmarks(file), failure.ToString());
            return;
        }
        w.ReportLostBookmarks(found!, file);
        var bookmarks = found!
            .Where(f => f.Place != null)
            .Select(f => Bookmark.FromRecord(f.Record, f.Place!))
            .ToList();
        if (w.Bookmarks.Items.Count == 0)
        {
            w.Bookmarks.Import(bookmarks, BookmarkImportMode.Replace);
            return;
        }
        if (bookmarks.Count == 0)
            return;
        w.PendingImport = bookmarks;
    }

    private static void ReportLostBookmarks(this Workspace w, List<FoundBookmark> found, string file)
    {
        var lost = found.FindAll(f => f.Place == null);
        if (lost.Count == 0)
            return;
        var lines = lost.Select(DescribeLostBookmark);
        var details = string.Join(Environment.NewLine, lines);
        w.ReportError(w.GetStrings().ErrorBookmarksNotFound(file), details);
    }

    private static string DescribeLostBookmark(FoundBookmark lost)
    {
        var record = lost.Record;
        var location = record.LocationNodeName ?? record.MemberName;
        return $"{record.Name}: {location} ({record.FileName})";
    }

    /// <summary>Imports the <see cref="Workspace.PendingImport"/> as <paramref name="mode"/> says, or
    /// drops it when null.</summary>
    public static void FinishImport(this Workspace w, BookmarkImportMode? mode)
    {
        if (mode != null && w.PendingImport != null)
            w.Bookmarks.Import(w.PendingImport, mode.Value);
        w.PendingImport = null;
    }

    private sealed record FoundBookmark(BookmarkRecord Record, NodePath? Place);
}

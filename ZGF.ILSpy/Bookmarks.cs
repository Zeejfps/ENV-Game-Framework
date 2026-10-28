using System.Diagnostics;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>Marking lines with bookmarks, going to them, and exporting and importing them.</summary>
public static class Bookmarks
{
    /// <summary>Removes the bookmark marking <paramref name="line"/> of <paramref name="tab"/>'s
    /// document, or bookmarks the line, naming the bookmark by the statement or definition on it
    /// when it has one. Returns which it did. <paramref name="nameOf"/> names a new bookmark by a number,
    /// counted from 1 until no bookmark has the name.</summary>
    public static BookmarkToggle ToggleBookmark(AppState appState, DocumentTab tab, int line, Func<int, string> nameOf)
    {
        var node = tab.Node;
        if (node == null)
            return new BookmarkToggle.Unchanged();
        if (!CanBookmarkLine(tab, line))
            return new BookmarkToggle.Unchanged();
        var existing = appState.Bookmarks.At(tab.Document, node.Path, line);
        if (existing != null)
            return RemoveBookmark(appState, existing);
        return AddBookmark(appState, tab, node, line, nameOf);
    }

    private static BookmarkToggle RemoveBookmark(AppState appState, Bookmark bookmark)
    {
        var row = appState.Bookmarks.Remove(bookmark);
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

    private static BookmarkToggle AddBookmark(AppState appState, DocumentTab tab, BrowseNode node, int line, Func<int, string> nameOf)
    {
        var anchor = tab.Document.AnchorAt(line);
        var name = appState.Bookmarks.NextName(nameOf);
        var info = ILSpyTree.Describe(node, anchor);
        var bookmark = new Bookmark(node.Path, tab.Language, line, anchor, name, info);
        appState.Bookmarks.Add(bookmark);
        return new BookmarkToggle.Added(bookmark);
    }

    /// <summary>Shows the line <paramref name="bookmark"/> marks in the active tab, opening its
    /// assembly first if it is not open, and records it in the history.</summary>
    public static async void GoToBookmark(AppState appState, Bookmark bookmark)
    {
        var ticket = appState.Intent.Begin();
        var destination = ChooseDestination(appState, inNewTab: false);
        var place = bookmark.Place;
        appState.Intent.StartResolving(ticket);
        var found = await Background.Run(() => appState.Assemblies.FindNode(place));
        if (appState.Intent.IsStale(ticket))
            return;
        appState.Intent.StopResolving();
        ShowBookmarkPlace(appState, bookmark, found, destination);
    }

    private static void ShowBookmarkPlace(AppState appState, Bookmark bookmark, Outcome<BrowseNode?> found, TabDestination destination)
    {
        switch (found)
        {
            case Outcome<BrowseNode?>.Ok { Value: { } node }:
                LeaveActiveTab(appState);
                ShowNodeIn(appState, destination, node, target: bookmark);
                appState.History.Push(node, appState.Language);
                break;
            case Outcome<BrowseNode?>.Ok:
                ReportBookmarkGone(appState, bookmark, null);
                break;
            case Outcome<BrowseNode?>.Failed failed:
                ReportBookmarkGone(appState, bookmark, failed.Error);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void ReportBookmarkGone(AppState appState, Bookmark bookmark, Exception? error)
    {
        appState.ReportError(new AppProblem.BookmarkGone(bookmark.Name, bookmark.Place, error));
    }

    /// <summary>Goes to the next enabled bookmark after <paramref name="from"/>, or the previous when
    /// <paramref name="direction"/> is negative. Returns the bookmark gone to, or null when none is enabled.</summary>
    public static Bookmark? StepBookmark(AppState appState, Bookmark? from, int direction)
    {
        var bookmark = appState.Bookmarks.Step(from, direction);
        if (bookmark == null)
            return null;
        GoToBookmark(appState, bookmark);
        return bookmark;
    }

    public static void ExportBookmarks(AppState appState, string path)
    {
        var records = appState.Bookmarks.Save();
        var error = BookmarkFile.Write(path, records);
        if (error == null)
            return;
        appState.ReportError(new AppProblem.BookmarksNotExported(error));
    }

    /// <summary>Reads the bookmarks in the file at <paramref name="path"/> — this app's or the real
    /// ILSpy's — and finds the place of each in the background: takes them at once when there are
    /// none yet, and otherwise waits in <see cref="AppState.Imports"/> for the user to say how. Bookmarks
    /// whose place is not found are left out and reported.</summary>
    public static async void ImportBookmarks(AppState appState, string path)
    {
        var file = Path.GetFileName(path);
        var records = BookmarkFile.Read(path, out var error);
        if (records == null)
        {
            appState.ReportError(new AppProblem.BookmarksNotImported(file, error ?? ""));
            return;
        }
        appState.Imports.StartFinding();
        var found = await Background.Run(() => FindPlaces(appState, records));
        appState.Imports.StopFinding();
        TakeImported(appState, found, file);
    }

    private static List<FoundBookmark> FindPlaces(AppState appState, List<BookmarkRecord> records)
    {
        return records.ConvertAll(r => new FoundBookmark(r, FindPlace(appState, r)));
    }

    private static NodePath? FindPlace(AppState appState, BookmarkRecord record)
    {
        if (record.Place != null)
            return NodePath.From(record.FileName, record.Place);
        var treePath = record.ViewState?.SelectedTreeNodePath;
        return ILSpyTree.Locate(appState.Assemblies, record.FileName, treePath, record.Token);
    }

    private static void TakeImported(AppState appState, Outcome<List<FoundBookmark>> found, string file)
    {
        switch (found)
        {
            case Outcome<List<FoundBookmark>>.Ok ok:
                TakeImported(appState, ok.Value, file);
                break;
            case Outcome<List<FoundBookmark>>.Failed failed:
                appState.ReportError(new AppProblem.BookmarksNotImported(file, failed.Error.ToString()));
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void TakeImported(AppState appState, List<FoundBookmark> places, string file)
    {
        ReportLostBookmarks(appState, places, file);
        var bookmarks = places
            .Where(f => f.Place != null)
            .Select(f => Bookmark.FromRecord(f.Record, f.Place!))
            .ToList();
        if (bookmarks.Count == 0)
            return;
        if (appState.Bookmarks.Items.Count == 0)
        {
            TakeBookmarks(appState, bookmarks, BookmarkImportMode.Replace);
            return;
        }
        appState.Imports.Wait(new PendingImport(bookmarks));
    }

    private static void ReportLostBookmarks(AppState appState, List<FoundBookmark> found, string file)
    {
        var lost = found.FindAll(f => f.Place == null);
        if (lost.Count == 0)
            return;
        var lines = lost.ConvertAll(DescribeLostBookmark);
        appState.ReportError(new AppProblem.ImportedBookmarksLost(file, lines));
    }

    private static string DescribeLostBookmark(FoundBookmark lost)
    {
        var record = lost.Record;
        var location = record.LocationNodeName ?? record.MemberName;
        return $"{record.Name}: {location} ({record.FileName})";
    }

    /// <summary>Takes the bookmarks <paramref name="import"/> waits with as <paramref name="mode"/> says,
    /// or drops them when null.</summary>
    public static void FinishBookmarkImport(AppState appState, PendingImport import, BookmarkImportMode? mode)
    {
        if (!appState.Imports.Finish(import))
            return;
        if (mode == null)
            return;
        TakeBookmarks(appState, import.Bookmarks, mode.Value);
    }

    /// <summary>Takes <paramref name="bookmarks"/> in as <paramref name="mode"/> says. Those marking a
    /// line a bookmark marks already are left out: quietly when merging, and reported when replacing.</summary>
    private static void TakeBookmarks(AppState appState, IReadOnlyList<Bookmark> bookmarks, BookmarkImportMode mode)
    {
        var leftOut = appState.Bookmarks.Import(bookmarks, mode);
        if (mode == BookmarkImportMode.Merge)
            return;
        ReportLeftOutBookmarks(appState, leftOut);
    }

    /// <summary>Says how many bookmarks were left out because another marks the same line.</summary>
    internal static void ReportLeftOutBookmarks(AppState appState, int leftOut)
    {
        if (leftOut == 0)
            return;
        appState.ReportError(new AppProblem.BookmarksLeftOut(leftOut));
    }

    private sealed record FoundBookmark(BookmarkRecord Record, NodePath? Place);
}

using System.Runtime.CompilerServices;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class BookmarksPanelView
{
    /// <summary>The bookmark list's focus and scroll id.</summary>
    public const int ListId = 0x424D524B;

    private const string MenuId = "bookmark-menu";
    private const int MarkColumn = 0;
    private const int NameColumn = 1;
    private const int LocationColumn = 2;
    private const int ModuleColumn = 3;

    private static readonly UiIcon LocationLink = new(LucideIcons.ExternalLink, LucideIcons.Family);

    private static Strings? _headersFor;
    private static string[] _headers = [];

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var bookmarks = w.Bookmarks;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, w);
        ui.Separator();

        if (bookmarks.Items.Count == 0)
        {
            DocumentsView.Hint(ui, t.BookmarksHint);
            return;
        }

        var table = ui.Table(ListId, w.BookmarkColumns, bookmarks.Cells);
        table.Header(Headers(t));
        var removed = List(ui, w, table);
        Menu(ui, w);
        if (removed != null)
            bookmarks.Remove(removed);
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? List(Ui<UiState> ui, Workspace w, TableFrame table)
    {
        var bookmarks = w.Bookmarks;
        var palette = ui.State.CodeViewPalette;
        var renaming = bookmarks.Renaming != null;
        Bookmark? removed = null;
        using var body = table.Body(bookmarks.Items.Count, keys: !renaming);
        if (body.Focused && !renaming)
            removed = HandleKeys(ui, bookmarks);
        for (var i = body.First; i < body.End; i++)
            Row(ui, w, body, bookmarks.Items[i], i, palette);
        Answer(ui, w, body);
        return removed;
    }

    /// <summary>Does what the user asked of the table this frame: go to, copy or open the menu.</summary>
    private static void Answer(Ui<UiState> ui, Workspace w, TableBody body)
    {
        var bookmarks = w.Bookmarks;
        var selected = bookmarks.SelectedBookmark;
        if (selected == null) return;
        if (body.Activated && bookmarks.Renaming == null)
            w.GoTo(selected);
        if (body.CopyRequested && bookmarks.Cells.HasCell)
            ui.ClipboardText = Copied(selected, bookmarks.Cells);
        if (body.ContextMenuRequested)
            ui.OpenContextMenu(MenuId, body.ContextMenuAt);
    }

    private static string[] Headers(Strings t)
    {
        if (ReferenceEquals(t, _headersFor)) return _headers;
        _headersFor = t;
        _headers = ["", t.BookmarksColumnName, t.BookmarksColumnLocation, t.BookmarksColumnModule];
        return _headers;
    }

    private static FileFilter[] Filters(Strings t) =>
    [
        new(t.BookmarksFileFilter, ["*.json"]),
        new(t.FileFilterAll, ["*.*"]),
    ];

    private static void Toolbar(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var bookmarks = w.Bookmarks;
        var any = bookmarks.Items.Any(static b => b.Enabled);
        var selected = bookmarks.SelectedBookmark;
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("previous", LucideIcons.ArrowUp, LucideIcons.Family, t.BookmarksPrevious, any))
                w.StepBookmark(-1);
            if (ui.IconButton("next", LucideIcons.ArrowDown, LucideIcons.Family, t.BookmarksNext, any))
                w.StepBookmark(1);
            ui.Separator(theme.IconSize);
            if (ui.IconButton("delete", LucideIcons.Trash2, LucideIcons.Family, t.BookmarksDelete, selected != null))
                bookmarks.Remove(selected!);
            ui.Separator(theme.IconSize);
            if (ui.IconButton("export", LucideIcons.Upload, LucideIcons.Family, t.BookmarksExport, bookmarks.Items.Count > 0))
                ui.PickSaveFile(t.BookmarksExportTitle, null, BookmarkFile.SuggestedName, Filters(t), w.ExportBookmarks);
            if (ui.IconButton("import", LucideIcons.Download, LucideIcons.Family, t.BookmarksImport))
                ui.PickFile(t.BookmarksImportTitle, null, Filters(t), w.ImportBookmarks);
        }
    }

    /// <summary>What <paramref name="column"/> shows of <paramref name="bookmark"/>; nothing for the
    /// mark's column.</summary>
    private static string Shown(Bookmark bookmark, int column) => column switch
    {
        NameColumn => bookmark.Name,
        LocationColumn => bookmark.Location,
        ModuleColumn => bookmark.Module,
        _ => "",
    };

    private static void Row(Ui<UiState> ui, Workspace w, TableBody body, Bookmark bookmark, int index, CodeViewPalette palette)
    {
        var bookmarks = w.Bookmarks;
        var row = body.Row(index, RuntimeHelpers.GetHashCode(bookmark));
        int locationId;
        using (row)
        {
            if (row.CellAt(NameColumn).Pressed && bookmarks.Renaming != bookmark)
                bookmarks.StartRename(bookmark);
            MarkCell(ui, bookmarks, row, bookmark, palette);
            NameCell(ui, bookmarks, row, bookmark);
            LocationCell(ui, w, row, bookmark);
            TextCell(ui, row, bookmark, ModuleColumn);
            locationId = row.CellAt(LocationColumn).Id;
        }
        LocationTooltip(ui, locationId, bookmark);
    }

    private static void MarkCell(Ui<UiState> ui, BookmarkList bookmarks, TableRow row, Bookmark bookmark, CodeViewPalette palette)
    {
        var t = ui.State.Strings;
        var mark = row.CellAt(MarkColumn);
        if (mark.Hovered)
            ui.SetCursor(MouseCursor.Hand);
        if (mark.Clicked)
            bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
        var color = bookmark.Enabled ? palette.Bookmark : palette.GutterText;
        using (row.Cell(MarkColumn))
            ui.Icon(new UiIcon(LucideIcons.Bookmark, LucideIcons.Family, color));
        ui.Tooltip(mark.Id, bookmark.Enabled ? t.BookmarksDisable : t.BookmarksEnable);
    }

    private static void NameCell(Ui<UiState> ui, BookmarkList bookmarks, TableRow row, Bookmark bookmark)
    {
        if (bookmarks.Renaming != bookmark)
        {
            TextCell(ui, row, bookmark, NameColumn);
            return;
        }
        using (row.Cell(NameColumn))
            RenameField(ui, bookmarks);
    }

    private static void LocationCell(Ui<UiState> ui, Workspace w, TableRow row, Bookmark bookmark)
    {
        var t = ui.State.Strings;
        uint? color = null;
        if (!bookmark.Enabled)
            color = ui.Theme.TextMuted;
        var followed = row.TextCell(LocationColumn, bookmark.Location, color, link: LocationLink, linkTooltip: t.BookmarksGoTo);
        if (followed)
            w.GoTo(bookmark);
    }

    private static void TextCell(Ui<UiState> ui, TableRow row, Bookmark bookmark, int column)
    {
        var theme = ui.Theme;
        var module = column == ModuleColumn;
        var color = theme.Text;
        if (!bookmark.Enabled || module)
            color = theme.TextMuted;
        var overflow = module ? TextOverflow.EllipsisMiddle : TextOverflow.Ellipsis;
        row.TextCell(column, Shown(bookmark, column), color, overflow: overflow);
    }

    private static void LocationTooltip(Ui<UiState> ui, int cellId, Bookmark bookmark)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using var tooltip = ui.TooltipContent(cellId);
        if (!tooltip.IsOpen) return;
        using (ui.Column(new Box { Gap = theme.GapTight }))
        {
            ui.Text(bookmark.Place.ToString(), color: theme.Text, size: theme.FontSizeSmall, wrap: true);
            ui.Text(t.BookmarksGoToHint, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
        }
    }

    private static void RenameField(Ui<UiState> ui, BookmarkList bookmarks)
    {
        var begin = bookmarks.FocusRename;
        bookmarks.FocusRename = false;
        var result = ui.InlineEdit("name", ref bookmarks.RenameText, begin, Sizing.Grow());
        if (result == InlineEditResult.Editing) return;
        var stillHere = ui.HasFocus(ui.GetId("name"));
        if (result == InlineEditResult.Committed)
            bookmarks.CommitRename();
        else
            bookmarks.CancelRename();
        if (stillHere || result == InlineEditResult.Cancelled)
            ui.Focus(ListId, takesText: false);
    }

    private static void Menu(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen || w.Bookmarks.SelectedBookmark is not { } bookmark) return;
        var bookmarks = w.Bookmarks;
        if (ui.MenuItem(t.BookmarksGoTo, "Enter", id: "go-to"))
            w.GoTo(bookmark);
        if (ui.MenuItem(t.CodeCopy, "Ctrl+C", enabled: bookmarks.Cells.HasCell, id: "copy"))
            ui.ClipboardText = Copied(bookmark, bookmarks.Cells);
        ui.MenuSeparator();
        if (ui.MenuItem(t.BookmarksRename, "F2", id: "rename"))
            bookmarks.StartRename(bookmark);
        if (ui.MenuItem(bookmark.Enabled ? t.BookmarksDisable : t.BookmarksEnable, "Space", id: "enable"))
            bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
        if (ui.MenuItem(t.BookmarksDelete, "Delete", id: "delete"))
            bookmarks.Remove(bookmark);
    }

    private static string Copied(Bookmark bookmark, TableCellSelection cells)
    {
        var shown = Shown(bookmark, cells.Column);
        return cells.Copied(shown, shown);
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? HandleKeys(Ui<UiState> ui, BookmarkList bookmarks)
    {
        Bookmark? removed = null;
        foreach (var stroke in ui.Input.Keys)
        {
            var bookmark = bookmarks.SelectedBookmark;
            if (bookmark == null) break;
            if (HasCommandModifier(stroke)) continue;
            switch (stroke.Key)
            {
                case KeyboardKey.Space:
                    bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
                    break;
                case KeyboardKey.F2:
                    bookmarks.Cells.SelectColumn(NameColumn);
                    bookmarks.StartRename(bookmark);
                    break;
                case KeyboardKey.Delete:
                    removed = bookmark;
                    break;
            }
        }
        return removed;
    }

    private static bool HasCommandModifier(KeyStroke stroke) =>
        (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0;
}

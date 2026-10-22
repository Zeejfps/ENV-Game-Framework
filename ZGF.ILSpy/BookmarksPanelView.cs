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

    private const int CellsId = 0x424D4345;
    private const int NameColumn = 1;
    private const int LocationColumn = 2;
    private const int ModuleColumn = 3;

    private static readonly UiIcon LocationLink = new(LucideIcons.ExternalLink, LucideIcons.Family);

    private static Strings? _headersFor;
    private static string[] _headers = [];

    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        var bookmarks = w.Bookmarks;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, w, t);
        ui.Separator();

        if (bookmarks.Items.Count == 0)
        {
            DocumentsView.Hint(ui, t.BookmarksHint);
            return;
        }

        var cell = ui.GetState<TableCellSelection>(CellsId);
        ui.TableHeader("header", w.BookmarkColumns, ListId, Headers(t), theme.ShellPaddingX);
        var removed = List(ui, w, t, cell);
        Menu(ui, w, t, cell);
        if (removed != null)
            bookmarks.Remove(removed);
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? List(Ui ui, Workspace w, Strings t, TableCellSelection cell)
    {
        var bookmarks = w.Bookmarks;
        var palette = ui.GetCodeViewPalette();
        var renaming = bookmarks.Renaming != null;
        Bookmark? removed = null;
        using var list = ui.SelectableList(ListId, bookmarks.Items.Count, bookmarks.Selection, keys: !renaming);
        FollowSelection(cell, bookmarks);
        if (list.Focused && !renaming)
            removed = HandleKeys(ui, w, cell);
        var activated = bookmarks.SelectedBookmark;
        if (list.Activated && activated != null)
            w.GoTo(activated);
        if (list.ContextMenuRequested)
            ui.OpenContextMenu("bookmark-menu", list.ContextMenuAt);
        for (var i = list.First; i < list.End; i++)
        {
            var bookmark = bookmarks.Items[i];
            using (ui.Id(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(bookmark)))
                Row(ui, w, t, list, cell, bookmark, i, palette);
        }
        return removed;
    }

    /// <summary>Keeps the current cell on the selected bookmark, in the column it was in.</summary>
    private static void FollowSelection(TableCellSelection cell, BookmarkList bookmarks)
    {
        var selected = bookmarks.SelectedBookmark;
        if (selected == null)
        {
            cell.Clear();
            return;
        }
        if (cell.Row == bookmarks.Selected) return;
        cell.Select(bookmarks.Selected, cell.Column, Shown(selected, cell.Column));
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

    private static void Toolbar(Ui ui, Workspace w, Strings t)
    {
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

    private static void Row(Ui ui, Workspace w, Strings t, ListScope list, TableCellSelection cell, Bookmark bookmark, int index, CodeViewPalette palette)
    {
        var bookmarks = w.Bookmarks;
        var hovered = false;
        var mark = ui.ListCell(ListId, ui.GetId("mark"), ref hovered);
        if (mark.Pressed)
            bookmarks.Selection.Select(index, bookmarks.Items.Count);
        Span<Interaction> cells = stackalloc Interaction[ModuleColumn + 1];
        for (var c = NameColumn; c <= ModuleColumn; c++)
        {
            cells[c] = ui.ListCell(ListId, ui.GetId(c), ref hovered);
            if (cells[c].Pressed || cells[c].ClickedWith(PointerButton.Right))
            {
                bookmarks.Selection.Select(index, bookmarks.Items.Count);
                cell.Select(index, c, Shown(bookmark, c));
                if (c == NameColumn && cells[c].Pressed && bookmarks.Renaming != bookmark)
                    bookmarks.StartRename(bookmark);
            }
            if (cells[c].ClickedWith(PointerButton.Right))
                ui.OpenContextMenu("bookmark-menu");
        }

        using (var row = list.Row(index, 0, new Box { Gap = 0f }, hovered))
        {
            var columns = w.BookmarkColumns;
            using (ui.TableCell(columns, 0))
            {
                if (mark.Hovered) ui.SetCursor(MouseCursor.Hand);
                if (mark.Clicked)
                    bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
                using (ui.Box(mark.Id, new Box { Height = Sizing.Grow() }))
                    ui.Icon(new UiIcon(LucideIcons.Bookmark, LucideIcons.Family, bookmark.Enabled ? palette.Bookmark : palette.GutterText));
                ui.Tooltip(mark.Id, bookmark.Enabled ? t.BookmarksDisable : t.BookmarksEnable);
            }
            for (var c = NameColumn; c <= ModuleColumn; c++)
            {
                if (c == NameColumn && bookmarks.Renaming == bookmark)
                {
                    using (ui.TableCell(columns, c, cells[c].Id))
                        RenameField(ui, bookmarks);
                    continue;
                }
                var current = row.Selected && cell.Column == c;
                if (c == LocationColumn)
                    LocationCell(ui, w, t, cells[c], cell, bookmark, current, row.Focused);
                else
                    TextCell(ui, w, cells[c], cell, bookmark, c, current, row.Focused);
            }
        }
        LocationTooltip(ui, t, cells[LocationColumn].Id, bookmark);
    }

    private static void LocationCell(Ui ui, Workspace w, Strings t, Interaction i, TableCellSelection cell, Bookmark bookmark, bool current, bool focused)
    {
        uint? color = null;
        if (!bookmark.Enabled)
            color = ui.Theme.TextMuted;
        var followed = ui.TableCellText(w.BookmarkColumns, LocationColumn, i, bookmark.Location, cell, current, focused, color, link: LocationLink,
            linkTooltip: t.BookmarksGoTo);
        if (followed)
            w.GoTo(bookmark);
    }

    private static void TextCell(Ui ui, Workspace w, Interaction i, TableCellSelection cell, Bookmark bookmark, int column, bool current, bool focused)
    {
        var theme = ui.Theme;
        var module = column == ModuleColumn;
        var color = theme.Text;
        if (!bookmark.Enabled || module)
            color = theme.TextMuted;
        var overflow = module ? TextOverflow.EllipsisMiddle : TextOverflow.Ellipsis;
        ui.TableCellText(w.BookmarkColumns, column, i, Shown(bookmark, column), cell, current, focused, color, overflow: overflow);
    }

    private static void LocationTooltip(Ui ui, Strings t, int cellId, Bookmark bookmark)
    {
        var theme = ui.Theme;
        using var tooltip = ui.TooltipContent(cellId);
        if (!tooltip.IsOpen) return;
        using (ui.Column(new Box { Gap = theme.GapTight }))
        {
            ui.Text(bookmark.Place.ToString(), color: theme.Text, size: theme.FontSizeSmall, wrap: true);
            ui.Text(t.BookmarksGoToHint, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
        }
    }

    private static void RenameField(Ui ui, BookmarkList bookmarks)
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

    private static void Menu(Ui ui, Workspace w, Strings t, TableCellSelection cell)
    {
        using var menu = ui.ContextMenu("bookmark-menu");
        if (!menu.IsOpen || w.Bookmarks.SelectedBookmark is not { } bookmark) return;
        var bookmarks = w.Bookmarks;
        if (ui.MenuItem(t.BookmarksGoTo, "Enter", id: "go-to"))
            w.GoTo(bookmark);
        if (ui.MenuItem(t.CodeCopy, "Ctrl+C", enabled: cell.HasCell, id: "copy"))
            ui.ClipboardText = Copied(bookmark, cell);
        ui.MenuSeparator();
        if (ui.MenuItem(t.BookmarksRename, "F2", id: "rename"))
            bookmarks.StartRename(bookmark);
        if (ui.MenuItem(bookmark.Enabled ? t.BookmarksDisable : t.BookmarksEnable, "Space", id: "enable"))
            bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
        if (ui.MenuItem(t.BookmarksDelete, "Delete", id: "delete"))
            bookmarks.Remove(bookmark);
    }

    private static string Copied(Bookmark bookmark, TableCellSelection cell)
    {
        var shown = Shown(bookmark, cell.Column);
        return cell.Copied(shown, shown);
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? HandleKeys(Ui ui, Workspace w, TableCellSelection cell)
    {
        var bookmarks = w.Bookmarks;
        Bookmark? removed = null;
        foreach (var stroke in ui.Input.Keys)
        {
            var bookmark = bookmarks.SelectedBookmark;
            if (bookmark == null) break;
            switch (cell.ApplyKey(stroke, NameColumn, ModuleColumn, out var column))
            {
                case TableCellKey.Column:
                    cell.Select(bookmarks.Selected, column, Shown(bookmark, column));
                    continue;
                case TableCellKey.Copy:
                    if (cell.HasCell)
                        ui.ClipboardText = Copied(bookmark, cell);
                    continue;
                case TableCellKey.Collapsed:
                    continue;
            }
            if ((stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            switch (stroke.Key)
            {
                case KeyboardKey.Space:
                    bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
                    break;
                case KeyboardKey.F2:
                    cell.Select(bookmarks.Selected, NameColumn, Shown(bookmark, NameColumn));
                    bookmarks.StartRename(bookmark);
                    break;
                case KeyboardKey.Delete:
                    removed = bookmark;
                    break;
            }
        }
        return removed;
    }
}

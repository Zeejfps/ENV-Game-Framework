using System.Runtime.CompilerServices;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Bookmarks;

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

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var bookmarks = appState.Bookmarks;
        var panel = ui.State.BookmarksPanel;
        panel.Follow(bookmarks.Items);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, appState);
        ui.Separator();

        if (bookmarks.Items.Count == 0)
        {
            DocumentsView.Hint(ui, t.BookmarksHint);
            return;
        }

        panel.Selection.ShowIn(bookmarks.Items);
        var table = ui.Table(ListId, ui.State.BookmarkColumns, panel.Cells);
        table.Header(Headers(t));
        var removed = List(ui, appState, table);
        Menu(ui, appState);
        if (removed != null)
            panel.Remove(bookmarks, removed);
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? List(Ui<UiState> ui, AppState appState, TableFrame table)
    {
        var bookmarks = appState.Bookmarks;
        var panel = ui.State.BookmarksPanel;
        var palette = ui.State.CodeViewPalette;
        var renaming = panel.Renaming != null;
        Bookmark? removed = null;
        using var body = table.Body(bookmarks.Items.Count, keys: !renaming);
        panel.Selection.TakeFrom(bookmarks.Items);
        if (body.Focused && !renaming)
            removed = HandleKeys(ui, panel, bookmarks);
        for (var i = body.First; i < body.End; i++)
            Row(ui, appState, body, bookmarks.Items[i], i, palette);
        panel.Selection.TakeFrom(bookmarks.Items);
        Answer(ui, appState, body);
        return removed;
    }

    /// <summary>Does what the user asked of the table this frame: go to, copy or open the menu.</summary>
    private static void Answer(Ui<UiState> ui, AppState appState, TableBody body)
    {
        var panel = ui.State.BookmarksPanel;
        var selected = panel.Selected;
        if (selected == null) return;
        if (body.Activated && panel.Renaming == null)
            GoToBookmark(appState, selected);
        if (body.CopyRequested && panel.Cells.HasCell)
            ui.ClipboardText = Copied(selected, panel.Cells);
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

    private static void Toolbar(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var bookmarks = appState.Bookmarks;
        var panel = ui.State.BookmarksPanel;
        var any = bookmarks.Items.Any(static b => b.Enabled);
        var selected = panel.Selected;
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("previous", LucideIcons.ArrowUp, LucideIcons.Family, t.BookmarksPrevious, any))
                panel.Step(appState, -1);
            if (ui.IconButton("next", LucideIcons.ArrowDown, LucideIcons.Family, t.BookmarksNext, any))
                panel.Step(appState, 1);
            ui.Separator(theme.IconSize);
            if (ui.IconButton("delete", LucideIcons.Trash2, LucideIcons.Family, t.BookmarksDelete, selected != null))
                panel.Remove(bookmarks, selected!);
            ui.Separator(theme.IconSize);
            if (ui.IconButton("export", LucideIcons.Upload, LucideIcons.Family, t.BookmarksExport, bookmarks.Items.Count > 0))
                PickExportFile(ui, appState);
            if (ui.IconButton("import", LucideIcons.Download, LucideIcons.Family, t.BookmarksImport))
                PickImportFile(ui, appState);
        }
    }

    private static void PickExportFile(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        ui.PickSaveFile(t.BookmarksExportTitle, null, BookmarkFile.SuggestedName, Filters(t), path => ExportBookmarks(appState, path));
    }

    private static void PickImportFile(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        ui.PickFile(t.BookmarksImportTitle, null, Filters(t), path => ImportBookmarks(appState, path));
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

    private static void Row(Ui<UiState> ui, AppState appState, TableBody body, Bookmark bookmark, int index, CodeViewPalette palette)
    {
        var bookmarks = appState.Bookmarks;
        var panel = ui.State.BookmarksPanel;
        var listed = appState.IsListed(bookmark.Place.FilePath);
        var row = body.Row(index, RuntimeHelpers.GetHashCode(bookmark));
        int locationId;
        int moduleId;
        using (row)
        {
            if (row.CellAt(NameColumn).Pressed && panel.Renaming != bookmark)
                panel.StartRename(bookmark);
            MarkCell(ui, bookmarks, row, bookmark, palette);
            NameCell(ui, panel, bookmarks, row, bookmark, listed);
            LocationCell(ui, appState, row, bookmark, listed);
            TextCell(ui, row, bookmark, listed, ModuleColumn);
            locationId = row.CellAt(LocationColumn).Id;
            moduleId = row.CellAt(ModuleColumn).Id;
        }
        LocationTooltip(ui, locationId, bookmark, listed);
        ModuleTooltip(ui, moduleId, bookmark, listed);
    }

    /// <summary>A disabled bookmark, or one whose assembly is not in the active list, is shown muted.</summary>
    private static bool IsDimmed(Bookmark bookmark, bool listed) => !bookmark.Enabled || !listed;

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

    private static void NameCell(Ui<UiState> ui, BookmarksPanel panel, BookmarkList bookmarks, TableRow row, Bookmark bookmark, bool listed)
    {
        if (panel.Renaming != bookmark)
        {
            TextCell(ui, row, bookmark, listed, NameColumn);
            return;
        }
        using (row.Cell(NameColumn))
            RenameField(ui, panel, bookmarks);
    }

    private static void LocationCell(Ui<UiState> ui, AppState appState, TableRow row, Bookmark bookmark, bool listed)
    {
        var t = ui.State.Strings;
        uint? color = null;
        if (IsDimmed(bookmark, listed))
            color = ui.Theme.TextMuted;
        var followed = row.TextCell(LocationColumn, bookmark.Location, color, link: LocationLink, linkTooltip: t.BookmarksGoTo);
        if (followed)
            GoToBookmark(appState, bookmark);
    }

    private static void TextCell(Ui<UiState> ui, TableRow row, Bookmark bookmark, bool listed, int column)
    {
        var theme = ui.Theme;
        var module = column == ModuleColumn;
        var color = theme.Text;
        if (module || IsDimmed(bookmark, listed))
            color = theme.TextMuted;
        var overflow = module ? TextOverflow.EllipsisMiddle : TextOverflow.Ellipsis;
        row.TextCell(column, Shown(bookmark, column), color, overflow: overflow);
    }

    private static void LocationTooltip(Ui<UiState> ui, int cellId, Bookmark bookmark, bool listed)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using var tooltip = ui.TooltipContent(cellId);
        if (!tooltip.IsOpen) return;
        using (ui.Column(new Box { Gap = theme.GapTight }))
        {
            ui.Text(bookmark.Place.ToString(), color: theme.Text, size: theme.FontSizeSmall, wrap: true);
            if (!listed)
                NotListedText(ui, bookmark);
            ui.Text(t.BookmarksGoToHint, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
        }
    }

    /// <summary>On the module of a bookmark whose assembly is not in the active list, says that going
    /// to it adds the assembly.</summary>
    private static void ModuleTooltip(Ui<UiState> ui, int cellId, Bookmark bookmark, bool listed)
    {
        if (listed) return;
        using var tooltip = ui.TooltipContent(cellId);
        if (!tooltip.IsOpen) return;
        NotListedText(ui, bookmark);
    }

    private static void NotListedText(Ui<UiState> ui, Bookmark bookmark)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Text(t.BookmarksNotListed(bookmark.Module), color: theme.Text, size: theme.FontSizeSmall, wrap: true);
    }

    private static void RenameField(Ui<UiState> ui, BookmarksPanel panel, BookmarkList bookmarks)
    {
        var begin = panel.FocusRename;
        panel.FocusRename = false;
        var result = ui.InlineEdit("name", ref panel.RenameText, begin, Sizing.Grow());
        if (result == InlineEditResult.Editing) return;
        var stillHere = ui.HasFocus(ui.GetId("name"));
        if (result == InlineEditResult.Committed)
            panel.CommitRename(bookmarks);
        else
            panel.CancelRename();
        if (stillHere || result == InlineEditResult.Cancelled)
            ui.Focus(ListId, takesText: false);
    }

    private static void Menu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var panel = ui.State.BookmarksPanel;
        var bookmark = panel.Selected;
        if (bookmark == null)
            return;
        var bookmarks = appState.Bookmarks;
        if (ui.MenuItem(t.BookmarksGoTo, "Enter", id: "go-to"))
            GoToBookmark(appState, bookmark);
        if (ui.MenuItem(t.CodeCopy, "Ctrl+C", enabled: panel.Cells.HasCell, id: "copy"))
            ui.ClipboardText = Copied(bookmark, panel.Cells);
        ui.MenuSeparator();
        if (ui.MenuItem(t.BookmarksRename, "F2", id: "rename"))
            panel.StartRename(bookmark);
        if (ui.MenuItem(bookmark.Enabled ? t.BookmarksDisable : t.BookmarksEnable, "Space", id: "enable"))
            bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
        if (ui.MenuItem(t.BookmarksDelete, "Delete", id: "delete"))
            panel.Remove(bookmarks, bookmark);
    }

    private static string Copied(Bookmark bookmark, TableCellSelection cells)
    {
        var shown = Shown(bookmark, cells.Column);
        return cells.Copied(shown, shown);
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? HandleKeys(Ui<UiState> ui, BookmarksPanel panel, BookmarkList bookmarks)
    {
        Bookmark? removed = null;
        foreach (var stroke in ui.Input.Keys)
        {
            var bookmark = panel.Selected;
            if (bookmark == null) break;
            if (HasCommandModifier(stroke)) continue;
            switch (stroke.Key)
            {
                case KeyboardKey.Space:
                    bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
                    break;
                case KeyboardKey.F2:
                    panel.Cells.SelectColumn(NameColumn);
                    panel.StartRename(bookmark);
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

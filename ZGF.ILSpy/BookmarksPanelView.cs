using System.Runtime.CompilerServices;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
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
    internal const int NameColumn = 1;
    private const int LocationColumn = 2;
    private const int ModuleColumn = 3;

    private static readonly ConditionalWeakTable<Strings, string[]> HeaderTexts = new();

    public static void Render(Ui<UiState> ui, BookmarkList bookmarks, AssemblyList assemblies)
    {
        var t = ui.State.Strings;
        var panel = ui.State.BookmarksPanel;
        panel.Follow(bookmarks.Items);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, bookmarks);
        ui.Separator();

        if (bookmarks.Items.Count == 0)
        {
            DocumentsView.Hint(ui, t.BookmarksHint);
            return;
        }

        panel.Selection.ShowIn(bookmarks.Items);
        var table = ui.Table(ListId, ui.State.BookmarkColumns, panel.Cells);
        table.Header(HeaderTexts.GetValue(t, static t => ListHeaders(t)));
        var removed = List(ui, bookmarks, assemblies, table);
        Menu(ui);
        if (removed != null)
            ui.State.Later((panel, removed), static (a, s) => Remove(s.panel, a.Bookmarks, s.removed));
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? List(Ui<UiState> ui, BookmarkList bookmarks, AssemblyList assemblies, TableFrame table)
    {
        var panel = ui.State.BookmarksPanel;
        var palette = ui.State.CodeViewPalette;
        var renaming = panel.Renaming != null;
        Bookmark? removed = null;
        var selected = panel.Selected;
        if (table.Focused && !renaming && selected != null && ui.Input.TakeKey(KeyboardKey.F2))
            RenameInPlace(panel, selected);
        using var body = table.Body(bookmarks.Items.Count, keys: !renaming);
        panel.Selection.TakeFrom(bookmarks.Items);
        if (body.Focused && !renaming)
            removed = HandleKeys(ui, panel);
        for (var i = body.First; i < body.End; i++)
            Row(ui, assemblies, body, bookmarks.Items[i], i, palette);
        panel.Selection.TakeFrom(bookmarks.Items);
        Answer(ui, body);
        return removed;
    }

    /// <summary>Does what the user asked of the table this frame: go to, rename, copy or open the menu.</summary>
    private static void Answer(Ui<UiState> ui, TableBody body)
    {
        var panel = ui.State.BookmarksPanel;
        var selected = panel.Selected;
        if (selected == null) return;
        if (body.Activated && panel.Renaming == null)
            ui.State.Later(selected, static (a, b) => GoToBookmark(a, b));
        if (body.EditRequested && panel.Cells.Column == NameColumn)
            panel.StartRename(selected);
        if (body.CopyRequested && panel.Cells.HasCell)
            ui.ClipboardText = Copied(selected, panel.Cells);
        if (body.ContextMenuRequested)
            ui.OpenContextMenu(MenuId, body.ContextMenuAt);
    }

    private static string[] ListHeaders(Strings t) => ["", t.BookmarksColumnName, t.BookmarksColumnLocation, t.BookmarksColumnModule];

    private static FileFilter[] Filters(Strings t) =>
    [
        new(t.BookmarksFileFilter, ["*.json"]),
        new(t.FileFilterAll, ["*.*"]),
    ];

    private static void Toolbar(Ui<UiState> ui, BookmarkList bookmarks)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var panel = ui.State.BookmarksPanel;
        var any = bookmarks.Items.Any(static b => b.Enabled);
        var selected = panel.Selected;
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("previous", ui.State.Icons.BookmarksPrevious, t.BookmarksPrevious, any))
                ui.State.Later(panel, static (a, p) => Step(p, a, -1));
            if (ui.IconButton("next", ui.State.Icons.BookmarksNext, t.BookmarksNext, any))
                ui.State.Later(panel, static (a, p) => Step(p, a, 1));
            ui.Separator(theme.IconSize);
            if (ui.IconButton("delete", ui.State.Icons.BookmarksDelete, t.BookmarksDelete, selected != null) && selected != null)
                ui.State.Later((panel, selected), static (a, s) => Remove(s.panel, a.Bookmarks, s.selected));
            ui.Separator(theme.IconSize);
            if (ui.IconButton("export", ui.State.Icons.BookmarksExport, t.BookmarksExport, bookmarks.Items.Count > 0))
                PickExportFile(ui);
            if (ui.IconButton("import", ui.State.Icons.BookmarksImport, t.BookmarksImport))
                PickImportFile(ui);
        }
    }

    private static void PickExportFile(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        ui.PickSaveFile(t.BookmarksExportTitle, null, BookmarkFile.SuggestedName, Filters(t), path => screen.Later(path, static (a, p) => ExportBookmarks(a, p)));
    }

    private static void PickImportFile(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        ui.PickFile(t.BookmarksImportTitle, null, Filters(t), path => screen.Later(path, static (a, p) => ImportBookmarks(a, p)));
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

    private static void Row(Ui<UiState> ui, AssemblyList assemblies, TableBody body, Bookmark bookmark, int index, CodeViewPalette palette)
    {
        var panel = ui.State.BookmarksPanel;
        var listed = assemblies.IsListed(bookmark.Place.FilePath);
        var row = body.Row(index, RuntimeHelpers.GetHashCode(bookmark));
        int locationId;
        int moduleId;
        using (row)
        {
            MarkCell(ui, row, bookmark, palette);
            NameCell(ui, panel, row, bookmark, listed);
            LocationCell(ui, row, bookmark, listed);
            TextCell(ui, row, bookmark, listed, ModuleColumn);
            locationId = row.CellAt(LocationColumn).Id;
            moduleId = row.CellAt(ModuleColumn).Id;
        }
        LocationTooltip(ui, locationId, bookmark, listed);
        ModuleTooltip(ui, moduleId, bookmark, listed);
    }

    /// <summary>A disabled bookmark, or one whose assembly is not in the active list, is shown muted.</summary>
    private static bool IsDimmed(Bookmark bookmark, bool listed) => !bookmark.Enabled || !listed;

    private static void MarkCell(Ui<UiState> ui, TableRow row, Bookmark bookmark, CodeViewPalette palette)
    {
        var t = ui.State.Strings;
        var mark = row.CellAt(MarkColumn);
        if (mark.Hovered)
            ui.SetCursor(MouseCursor.Hand);
        if (mark.Clicked)
            ui.State.Later(bookmark, static (a, b) => ToggleEnabled(a, b));
        var color = bookmark.Enabled ? palette.Bookmark : palette.GutterText;
        using (row.Cell(MarkColumn))
            ui.Icon(new UiIcon(ui.State.Icons.BookmarksMark, color));
        ui.Tooltip(mark.Id, bookmark.Enabled ? t.BookmarksDisable : t.BookmarksEnable);
    }

    private static void NameCell(Ui<UiState> ui, BookmarksPanel panel, TableRow row, Bookmark bookmark, bool listed)
    {
        if (panel.Renaming != bookmark)
        {
            TextCell(ui, row, bookmark, listed, NameColumn);
            return;
        }
        using (row.Cell(NameColumn))
            RenameField(ui, panel);
    }

    private static void LocationCell(Ui<UiState> ui, TableRow row, Bookmark bookmark, bool listed)
    {
        var t = ui.State.Strings;
        uint? color = null;
        if (IsDimmed(bookmark, listed))
            color = ui.Theme.TextMuted;
        var followed = row.TextCell(LocationColumn, bookmark.Location, color, link: new UiIcon(ui.State.Icons.LinkBookmark), linkTooltip: t.BookmarksGoTo);
        if (followed)
            ui.State.Later(bookmark, static (a, b) => GoToBookmark(a, b));
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

    private static void RenameField(Ui<UiState> ui, BookmarksPanel panel)
    {
        var begin = panel.FocusRename;
        panel.FocusRename = false;
        var result = ui.InlineEdit("name", ref panel.RenameText, begin, Sizing.Grow());
        if (result == InlineEditResult.Editing) return;
        var stillHere = ui.HasFocus(ui.GetId("name"));
        if (result == InlineEditResult.Committed)
            CommitRename(ui.State, panel);
        else
            panel.CancelRename();
        panel.Cells.StopEditing();
        if (stillHere || result == InlineEditResult.Cancelled)
            ui.Focus(ListId, takesText: false);
    }

    private static void Menu(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var panel = ui.State.BookmarksPanel;
        var bookmark = panel.Selected;
        if (bookmark == null)
            return;
        if (ui.MenuItem(t.BookmarksGoTo, Shortcuts.Enter, id: "go-to"))
            ui.State.Later(bookmark, static (a, b) => GoToBookmark(a, b));
        if (ui.MenuItem(t.CodeCopy, Shortcuts.Copy, enabled: panel.Cells.HasCell, id: "copy"))
            ui.ClipboardText = Copied(bookmark, panel.Cells);
        ui.MenuSeparator();
        if (ui.MenuItem(t.BookmarksRename, Shortcuts.Rename, id: "rename"))
            RenameInPlace(panel, bookmark);
        if (ui.MenuItem(bookmark.Enabled ? t.BookmarksDisable : t.BookmarksEnable, Shortcuts.Space, id: "enable"))
            ui.State.Later(bookmark, static (a, b) => ToggleEnabled(a, b));
        if (ui.MenuItem(t.BookmarksDelete, Shortcuts.Delete, id: "delete"))
            ui.State.Later((panel, bookmark), static (a, s) => Remove(s.panel, a.Bookmarks, s.bookmark));
    }

    private static string Copied(Bookmark bookmark, TableCellSelection cells)
    {
        var shown = Shown(bookmark, cells.Column);
        return cells.Copied(shown, shown);
    }

    /// <summary>Returns the bookmark Delete asked to remove.</summary>
    private static Bookmark? HandleKeys(Ui<UiState> ui, BookmarksPanel panel)
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
                    ui.State.Later(bookmark, static (a, b) => ToggleEnabled(a, b));
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

    /// <summary>Removes <paramref name="bookmark"/>, passing its selection to the bookmark now in its row.</summary>
    internal static void Remove(BookmarksPanel panel, BookmarkList bookmarks, Bookmark bookmark)
    {
        var row = bookmarks.Remove(bookmark);
        panel.TakeRemoved(bookmark, row, bookmarks.Items);
    }

    /// <summary>Goes to the next enabled bookmark after the selected one, or the previous when
    /// <paramref name="direction"/> is negative, and selects it.</summary>
    internal static void Step(BookmarksPanel panel, AppState appState, int direction)
    {
        var bookmark = StepBookmark(appState, panel.Selected, direction);
        if (bookmark == null)
            return;
        panel.Selection.Select(bookmark, appState.Bookmarks.Items);
    }

    private static void RenameInPlace(BookmarksPanel panel, Bookmark bookmark)
    {
        panel.Cells.SelectColumn(NameColumn);
        panel.Cells.Edit();
        panel.StartRename(bookmark);
    }

    /// <summary>Gives the bookmark being renamed the name typed, unless it is blank, and ends the rename.</summary>
    private static void CommitRename(UiState screen, BookmarksPanel panel)
    {
        var bookmark = panel.EndRename();
        if (bookmark == null)
            return;
        screen.Later((bookmark, name: panel.RenameText), static (a, s) => a.Bookmarks.Rename(s.bookmark, s.name));
    }

    private static void ToggleEnabled(AppState appState, Bookmark bookmark) =>
        appState.Bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
}

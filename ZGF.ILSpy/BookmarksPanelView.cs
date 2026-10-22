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
            using (ui.Column(new Box { Height = Sizing.Grow(), Padding = theme.ShellPaddingX, AlignX = Align.Center, AlignY = Align.Center }))
                ui.Text(t.BookmarksHint, color: theme.TextMuted, wrap: true, align: Align.Center);
            return;
        }

        var rowHeight = theme.RowHeight;
        var area = ui.Interact(ListId);
        if (area.Pressed)
            ui.Focus(ListId, takesText: false);
        var focused = ui.HasFocus(ListId);
        var cell = ui.GetState<TableCellSelection>(CellsId);
        if (focused && bookmarks.Renaming == null)
            Keys(ui, w, cell, rowHeight);
        if (bookmarks.SelectedBookmark is not { } selected)
            cell.Clear();
        else if (cell.Row != bookmarks.Selected)
            cell.Select(bookmarks.Selected, cell.Column, Shown(selected, cell.Column));

        var palette = ui.Get<CodeViewPalette>() ?? CodeViewPalette.Dark;
        ui.TableHeader("header", w.BookmarkColumns, ListId, Headers(t), theme.ShellPaddingX);
        using (var list = ui.VirtualScroll(ListId, bookmarks.Items.Count, rowHeight, new Box { Gap = 0f }))
        {
            if (bookmarks.ScrollToSelected)
            {
                list.ScrollIntoView(bookmarks.Selected);
                bookmarks.ScrollToSelected = false;
            }
            for (var i = list.First; i < list.End; i++)
            {
                var bookmark = bookmarks.Items[i];
                using (ui.Id(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(bookmark)))
                    Row(ui, w, t, cell, bookmark, i, focused, palette);
            }
        }
        Menu(ui, w, t, cell);
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
            ui.Leaf(new Box { Width = theme.SeparatorWidth, Height = theme.IconSize, Background = theme.Separator });
            if (ui.IconButton("delete", LucideIcons.Trash2, LucideIcons.Family, t.BookmarksDelete, selected != null))
                bookmarks.Remove(selected!);
            ui.Leaf(new Box { Width = theme.SeparatorWidth, Height = theme.IconSize, Background = theme.Separator });
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

    private static void Row(Ui ui, Workspace w, Strings t, TableCellSelection cell, Bookmark bookmark, int index, bool focused, CodeViewPalette palette)
    {
        var theme = ui.Theme;
        var bookmarks = w.Bookmarks;
        var mark = ui.Interact("mark");
        var hovered = mark.Hovered;
        if (mark.Pressed)
        {
            ui.Focus(ListId, takesText: false);
            bookmarks.Select(index);
        }
        Span<Interaction> cells = stackalloc Interaction[ModuleColumn + 1];
        for (var c = NameColumn; c <= ModuleColumn; c++)
        {
            cells[c] = ui.Interact(ui.GetId(c));
            hovered |= cells[c].Hovered || ui.IsLinkIconHovered(cells[c]);
            if (cells[c].Pressed || cells[c].ClickedWith(PointerButton.Right))
            {
                ui.Focus(ListId, takesText: false);
                bookmarks.Select(index);
                cell.Select(index, c, Shown(bookmark, c));
                if (c == NameColumn && cells[c].Pressed && bookmarks.Renaming != bookmark)
                    bookmarks.StartRename(bookmark);
            }
            if (cells[c].ClickedWith(PointerButton.Right))
                ui.OpenContextMenu("bookmark-menu");
        }

        var selected = index == bookmarks.Selected;
        using (ui.Row(new Box
        {
            Height = theme.RowHeight,
            Gap = 0f,
            Padding = new Spacing(theme.ShellPaddingX, 0f),
            AlignY = Align.Center,
            Background = selected ? theme.SelectionInactive : hovered ? theme.RowHover : 0u,
        }))
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
                var current = selected && cell.Column == c;
                if (cells[c].Hovered && c != LocationColumn)
                    ui.SetCursor(MouseCursor.Text);
                using (ui.TableCell(columns, c, cells[c].Id))
                {
                    if (c == NameColumn && bookmarks.Renaming == bookmark)
                    {
                        RenameField(ui, bookmarks);
                        continue;
                    }
                    if (current)
                        ui.TableCellOutline(focused);
                    var shown = Shown(bookmark, c);
                    if (c == LocationColumn)
                    {
                        if (ui.SelectableLink(cells[c], shown, cell.Text, ref cell.Shift, current, new UiIcon(LucideIcons.ExternalLink, LucideIcons.Family),
                                t.BookmarksGoTo, bookmark.Enabled ? null : theme.TextMuted))
                            w.GoTo(bookmark);
                    }
                    else
                    {
                        ui.SelectableText(cells[c], shown, cell.Text, ref cell.Shift, current,
                            !bookmark.Enabled || c == ModuleColumn ? theme.TextMuted : theme.Text,
                            overflow: c == ModuleColumn ? TextOverflow.EllipsisMiddle : TextOverflow.Ellipsis);
                    }
                    if (current)
                        cell.SettleClick(cells[c], shown);
                }
            }
        }
        LocationTooltip(ui, t, cells[LocationColumn].Id, bookmark);
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

    private static void Keys(Ui ui, Workspace w, TableCellSelection cell, float rowHeight)
    {
        var bookmarks = w.Bookmarks;
        var page = ui.TryGetRect(ListId, out var view) ? Math.Max(1, (int)(view.Height / rowHeight) - 1) : 10;
        var count = bookmarks.Items.Count;
        var keys = ui.Input.Keys;
        for (var k = 0; k < keys.Count; k++)
        {
            var stroke = keys[k];
            if (stroke.Key == KeyboardKey.C && (stroke.Modifiers & KeyModifiers.Control) != 0 && cell.HasCell && bookmarks.SelectedBookmark is { } copied)
                ui.ClipboardText = Copied(copied, cell);
            if ((stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            var selected = bookmarks.Selected;
            switch (stroke.Key)
            {
                case KeyboardKey.UpArrow:
                    bookmarks.Select(Math.Max(0, selected - 1));
                    break;
                case KeyboardKey.DownArrow:
                    bookmarks.Select(Math.Min(count - 1, selected + 1));
                    break;
                case KeyboardKey.PageUp:
                    bookmarks.Select(Math.Max(0, selected - page));
                    break;
                case KeyboardKey.PageDown:
                    bookmarks.Select(Math.Min(count - 1, Math.Max(0, selected) + page));
                    break;
                case KeyboardKey.Home:
                    bookmarks.Select(0);
                    break;
                case KeyboardKey.End:
                    bookmarks.Select(count - 1);
                    break;
                case KeyboardKey.LeftArrow when bookmarks.SelectedBookmark is { } bookmark:
                    MoveTo(Math.Max(NameColumn, cell.Column - 1), bookmark);
                    break;
                case KeyboardKey.RightArrow when bookmarks.SelectedBookmark is { } bookmark:
                    MoveTo(Math.Clamp(cell.Column + 1, NameColumn, ModuleColumn), bookmark);
                    break;
                case KeyboardKey.Escape when cell.Text.HasSelection:
                    cell.Text.Select(cell.Text.Caret, cell.Text.Caret);
                    break;
                case KeyboardKey.Enter when bookmarks.SelectedBookmark is { } bookmark:
                    w.GoTo(bookmark);
                    break;
                case KeyboardKey.Space when bookmarks.SelectedBookmark is { } bookmark:
                    bookmarks.SetEnabled(bookmark, !bookmark.Enabled);
                    break;
                case KeyboardKey.F2 when bookmarks.SelectedBookmark is { } bookmark:
                    MoveTo(NameColumn, bookmark);
                    bookmarks.StartRename(bookmark);
                    break;
                case KeyboardKey.Delete when bookmarks.SelectedBookmark is { } bookmark:
                    bookmarks.Remove(bookmark);
                    break;
            }
        }

        void MoveTo(int column, Bookmark bookmark) => cell.Select(bookmarks.Selected, column, Shown(bookmark, column));
    }
}

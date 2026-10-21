using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>Everything a <see cref="MetadataGridWidget.MetadataGridView"/> keeps between frames:
/// the columns' widths, the sort, the selected row and cell, and the scroll. One per open tab,
/// owned by the app.</summary>
public sealed class MetadataGridState
{
    internal const int None = -1;

    internal MetadataGrid? Grid;
    internal TableColumns? Columns;
    internal int SortColumn = None;
    internal bool Descending;
    internal int[]? Order;
    internal int Selected = None;
    internal int CurrentColumn = None;
    internal float ScrollX;
    internal float ScrollY;
    internal float RowHeight;
    internal int PendingReveal = None;
    internal CodeViewPosition? PendingRestore;
    internal bool ScrollToSelected;

    /// <summary>Where the view stands, as a code view's position: the top row as the top line, the
    /// selected row and cell as the caret.</summary>
    public CodeViewPosition Position =>
        new(RowHeight > 0f ? (int)(ScrollY / RowHeight) : 0, ScrollX, default, new TextPosition(Selected, CurrentColumn));

    /// <summary>Starts over on <paramref name="grid"/>: unsorted, nothing selected, at the top.</summary>
    public void Show(MetadataGrid grid)
    {
        if (ReferenceEquals(grid, Grid)) return;
        Grid = grid;
        Columns = null;
        SortColumn = Selected = CurrentColumn = PendingReveal = None;
        Descending = ScrollToSelected = false;
        Order = null;
        ScrollX = ScrollY = 0f;
        PendingRestore = null;
    }

    /// <summary>Returns the view to <paramref name="position"/> once it has been laid out.</summary>
    public void Restore(CodeViewPosition position)
    {
        PendingRestore = position;
        PendingReveal = None;
    }

    /// <summary>Selects <paramref name="row"/> and scrolls it into view once laid out.</summary>
    public void Reveal(int row)
    {
        PendingReveal = row;
        PendingRestore = null;
    }

    internal int RowAt(int index) => Order?[index] ?? index;

    internal int IndexOf(int row) => Order == null ? row : Array.IndexOf(Order, row);
}

/// <summary>What the user asked of a <see cref="MetadataGridWidget.MetadataGridView"/> this frame.</summary>
/// <param name="Target">The table row a token cell leads to.</param>
/// <param name="Definition">What a row stands for in the code, to decompile.</param>
/// <param name="InNewTab">Whether to show either in a new tab.</param>
public readonly record struct MetadataGridResult(MetadataTarget? Target, CodeReference? Definition, bool InNewTab);

public static class MetadataGridWidget
{
    private const int AutoFitRows = 200;
    private const float MaxAutoWidth = 480f;
    private const int CellSalt = 0x4D444743;

    public static MetadataGridResult MetadataGridView(this Ui ui, string id, MetadataGrid grid, MetadataGridState state, Strings t)
    {
        var theme = ui.Theme;
        var listId = ui.GetId(id);
        state.Show(grid);
        var columns = state.Columns ??= Measure(ui, grid);
        var rowHeight = state.RowHeight = theme.RowHeight;
        var result = default(MetadataGridResult);

        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = theme.EditorBackground });

        var headers = grid.Columns.Select(static c => c.Name).ToArray();
        var sortIcon = new UiIcon(state.Descending ? LucideIcons.ChevronDown : LucideIcons.ChevronUp, LucideIcons.Family);
        var clicked = ui.TableHeader($"{id}-header", columns, listId, headers, theme.ShellPaddingX, state.ScrollX, new TableSort(state.SortColumn, sortIcon));
        if (clicked >= 0)
            Sort(state, clicked);

        var area = ui.Interact(listId);
        if (area.Pressed)
            ui.Focus(listId, takesText: false);
        var focused = ui.HasFocus(listId);
        if (focused)
            result = Keys(ui, grid, state, listId, rowHeight);

        var width = columns.TotalWidth + theme.ShellPaddingX * 2;
        var monoSize = theme.FontSize;
        using (var list = ui.VirtualScroll(listId, grid.RowCount, rowHeight, ref state.ScrollX, ref state.ScrollY, new Box { Gap = 0f }))
        {
            Arrive(state, list, rowHeight);
            if (state.ScrollToSelected && list.ScrollIntoView(state.IndexOf(state.Selected)))
                state.ScrollToSelected = false;
            for (var index = list.First; index < list.End; index++)
            {
                var row = state.RowAt(index);
                var hoveredRow = false;
                Span<Interaction> cells = stackalloc Interaction[grid.Columns.Count];
                for (var c = 0; c < cells.Length; c++)
                {
                    cells[c] = ui.Interact(HashCode.Combine(listId, CellSalt, row, c));
                    hoveredRow |= cells[c].Hovered;
                    if (cells[c].Pressed || cells[c].ClickedWith(PointerButton.Right) || cells[c].ClickedWith(PointerButton.Middle))
                    {
                        ui.Focus(listId, takesText: false);
                        state.Selected = row;
                        state.CurrentColumn = c;
                    }
                    if (cells[c].Clicked && grid.Columns[c].IsToken && grid.Link(row, c) is { } target)
                        result = new MetadataGridResult(target, null, false);
                    else if (cells[c].DoubleClicked && grid.Definition(row) is { } definition)
                        result = new MetadataGridResult(null, definition, false);
                    else if (cells[c].ClickedWith(PointerButton.Middle) && grid.Definition(row) is { } inNewTab)
                        result = new MetadataGridResult(null, inNewTab, true);
                    if (cells[c].ClickedWith(PointerButton.Right))
                        ui.OpenContextMenu($"{id}-menu");
                }

                var selected = row == state.Selected;
                var background = selected ? focused ? theme.Selection : theme.SelectionInactive : hoveredRow ? theme.RowHover : 0u;
                using (ui.Row(new Box
                {
                    Width = width,
                    Height = rowHeight,
                    Gap = 0f,
                    Padding = new Spacing(theme.ShellPaddingX, 0f),
                    AlignY = Align.Center,
                    Background = background,
                }))
                {
                    for (var c = 0; c < cells.Length; c++)
                    {
                        var column = grid.Columns[c];
                        var link = column.IsToken && grid.Link(row, c) != null;
                        if (link && cells[c].Hovered)
                            ui.SetCursor(MouseCursor.Hand);
                        var color = link ? theme.AccentText : selected && focused ? theme.SelectionText : theme.Text;
                        using (ui.TableCell(columns, c, cells[c].Id))
                            ui.Text(grid.Text(row, c), new Box { Width = Sizing.Fit() }, color: color, size: column.IsNumeric || column.IsToken ? monoSize : null,
                                family: column.IsNumeric || column.IsToken ? MonoFonts.Regular : null, overflow: TextOverflow.Ellipsis);
                        Tooltip(ui, grid, row, c, cells[c].Id);
                    }
                }
            }
        }

        if (Menu(ui, id, grid, state, t) is { } picked)
            result = picked;
        return result;
    }

    private static TableColumns Measure(Ui ui, MetadataGrid grid)
    {
        var theme = ui.Theme;
        var padding = theme.GapTight * 2;
        var widths = new float[grid.Columns.Count];
        var rows = Math.Min(grid.RowCount, AutoFitRows);
        for (var c = 0; c < widths.Length; c++)
        {
            var column = grid.Columns[c];
            var mono = column.IsNumeric || column.IsToken;
            var width = ui.MeasureText(column.Name, theme.FontSizeSmall) + theme.IconSize + theme.GapTight;
            for (var row = 0; row < rows; row++)
                width = MathF.Max(width, ui.MeasureText(grid.Text(row, c), mono ? theme.FontSize : null, family: mono ? MonoFonts.Regular : null));
            widths[c] = MathF.Ceiling(MathF.Min(MaxAutoWidth, width + padding + theme.GapTight));
        }
        return TableColumns.Sized(widths);
    }

    private static void Sort(MetadataGridState state, int column)
    {
        var grid = state.Grid!;
        state.Descending = state.SortColumn == column && !state.Descending;
        state.SortColumn = column;
        state.Order = grid.Order(column, state.Descending);
        if (state.Selected != MetadataGridState.None)
            state.PendingReveal = state.Selected;
    }

    private static void Arrive(MetadataGridState state, Ui.VirtualList list, float rowHeight)
    {
        if (list.Viewport <= 0f) return;
        if (state.PendingRestore is { } restore)
        {
            state.PendingRestore = null;
            state.ScrollX = restore.ScrollX;
            list.ScrollTo(restore.TopLine * rowHeight);
            state.Selected = restore.Caret.Line;
            state.CurrentColumn = restore.Caret.Column;
        }
        else if (state.PendingReveal is >= 0 and var row)
        {
            state.PendingReveal = MetadataGridState.None;
            state.Selected = row;
            var index = state.IndexOf(row);
            if (index < 0) return;
            var top = index * rowHeight;
            if (top < list.Offset || top + rowHeight > list.Offset + list.Viewport)
                list.ScrollTo(MathF.Max(0f, top - list.Viewport / 3f));
        }
    }

    private static void Tooltip(Ui ui, MetadataGrid grid, int row, int column, int cellId)
    {
        if (!ui.IsHovered(cellId) || grid.Tooltip(row, column) is not { } content) return;
        var theme = ui.Theme;
        using var tooltip = ui.TooltipContent(cellId);
        if (!tooltip.IsOpen) return;
        if (content.Text is { } text)
        {
            ui.Text(text, color: theme.Text, size: theme.FontSizeSmall, wrap: true);
            return;
        }
        using (ui.Column(new Box { Gap = theme.GapTight }))
        {
            foreach (var group in content.Groups)
            {
                if (group.IsChoice)
                {
                    using (ui.Row(new Box { Gap = theme.GapTight }))
                    {
                        if (group.Header is { Length: > 0 } header)
                            ui.Text(header, color: theme.Text, size: theme.FontSizeSmall, bold: true);
                        if (group.Flags is [var flag, ..])
                            ui.Text(flag.Name, color: theme.Text, size: theme.FontSizeSmall);
                    }
                    continue;
                }
                using (ui.Column(new Box { Gap = 0f }))
                {
                    if (group.Header is { Length: > 0 } header)
                        ui.Text(header, color: theme.Text, size: theme.FontSizeSmall, bold: true);
                    foreach (var flag in group.Flags)
                    {
                        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
                        {
                            ui.Text(flag.IsSet ? LucideIcons.CheckSquare : LucideIcons.Square, color: flag.IsSet ? theme.AccentText : theme.TextMuted,
                                size: theme.FontSizeSmall, family: LucideIcons.Family);
                            ui.Text(flag.Name, color: flag.IsSet ? theme.Text : theme.TextMuted, size: theme.FontSizeSmall);
                        }
                    }
                }
            }
        }
    }

    private static MetadataGridResult? Menu(Ui ui, string id, MetadataGrid grid, MetadataGridState state, Strings t)
    {
        using var menu = ui.ContextMenu($"{id}-menu");
        if (!menu.IsOpen || state.Selected < 0 || state.Selected >= grid.RowCount) return null;
        var row = state.Selected;
        var column = state.CurrentColumn;
        MetadataGridResult? result = null;
        if (ui.MenuItem(t.CodeCopy, "Ctrl+C", enabled: column >= 0, id: "copy"))
            ui.ClipboardText = grid.Text(row, column);
        if (ui.MenuItem(t.MetadataCopyRow, id: "copy-row"))
            ui.ClipboardText = RowText(grid, row);
        var target = column >= 0 ? grid.Link(row, column) : null;
        var definition = grid.Definition(row);
        if (target == null && definition == null) return null;
        ui.MenuSeparator();
        if (target is { } token && ui.MenuItem(t.MetadataGoToToken, "Ctrl+G", id: "go-to-token"))
            result = new MetadataGridResult(token, null, false);
        if (definition != null)
        {
            if (ui.MenuItem(t.CodeGoToDefinition, "Enter", id: "go-to-definition"))
                result = new MetadataGridResult(null, definition, false);
            if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
                result = new MetadataGridResult(null, definition, true);
        }
        return result;
    }

    private static string RowText(MetadataGrid grid, int row) =>
        string.Join('\t', Enumerable.Range(0, grid.Columns.Count).Select(c => grid.Text(row, c)));

    private static MetadataGridResult Keys(Ui ui, MetadataGrid grid, MetadataGridState state, int listId, float rowHeight)
    {
        var count = grid.RowCount;
        if (count == 0) return default;
        var page = ui.TryGetRect(listId, out var view) ? Math.Max(1, (int)(view.Height / rowHeight) - 1) : 10;
        var result = default(MetadataGridResult);
        foreach (var stroke in ui.Input.Keys)
        {
            var modifiers = stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super);
            var index = state.Selected >= 0 ? state.IndexOf(state.Selected) : -1;
            var columns = grid.Columns.Count;
            switch (modifiers, stroke.Key)
            {
                case (0, KeyboardKey.UpArrow):
                    Select(Math.Max(0, index - 1));
                    break;
                case (0, KeyboardKey.DownArrow):
                    Select(Math.Min(count - 1, index + 1));
                    break;
                case (0, KeyboardKey.PageUp):
                    Select(Math.Max(0, index - page));
                    break;
                case (0, KeyboardKey.PageDown):
                    Select(Math.Min(count - 1, Math.Max(0, index) + page));
                    break;
                case (0, KeyboardKey.Home) or (KeyModifiers.Control, KeyboardKey.Home):
                    Select(0);
                    break;
                case (0, KeyboardKey.End) or (KeyModifiers.Control, KeyboardKey.End):
                    Select(count - 1);
                    break;
                case (0, KeyboardKey.LeftArrow):
                    state.CurrentColumn = Math.Max(0, state.CurrentColumn - 1);
                    break;
                case (0, KeyboardKey.RightArrow):
                    state.CurrentColumn = Math.Min(columns - 1, state.CurrentColumn + 1);
                    break;
                case (0, KeyboardKey.Enter) when state.Selected >= 0 && grid.Definition(state.Selected) is { } definition:
                    result = new MetadataGridResult(null, definition, false);
                    break;
                case (KeyModifiers.Control, KeyboardKey.C) when state.Selected >= 0:
                    ui.ClipboardText = state.CurrentColumn >= 0 ? grid.Text(state.Selected, state.CurrentColumn) : RowText(grid, state.Selected);
                    break;
                case (KeyModifiers.Control, KeyboardKey.G) when state is { Selected: >= 0, CurrentColumn: >= 0 }
                                                                && grid.Link(state.Selected, state.CurrentColumn) is { } target:
                    result = new MetadataGridResult(target, null, false);
                    break;
            }
        }
        return result;

        void Select(int index)
        {
            state.Selected = state.RowAt(index);
            if (state.CurrentColumn < 0)
                state.CurrentColumn = 0;
            state.ScrollToSelected = true;
        }
    }
}

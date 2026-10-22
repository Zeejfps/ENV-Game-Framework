using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>What the user asked of a <see cref="MetadataGridWidget.MetadataGridView"/> this frame.</summary>
/// <param name="Target">The table row a token cell leads to.</param>
/// <param name="Definition">What a row stands for in the code, to decompile.</param>
/// <param name="InNewTab">Whether to show either in a new tab.</param>
public readonly record struct MetadataGridResult(MetadataTarget? Target, CodeReference? Definition, bool InNewTab);

public static class MetadataGridWidget
{
    private const int AutoFitRows = 200;
    private const float MaxAutoWidth = 480f;
    private const float FlyoutMinWidth = 320f;
    private const int CellSalt = 0x4D444743;
    private const int FunnelSalt = 0x4D444746;
    private const int FilterTextSalt = 0x4D444754;
    private const int FlyoutSalt = 0x4D44464C;

    private static readonly UiIcon TokenLink = new(LucideIcons.ExternalLink, LucideIcons.Family);

    public static MetadataGridResult MetadataGridView(this Ui ui, string id, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t)
    {
        var listId = ui.GetId(id);
        state.Show(grid);
        if (state.Columns == null)
            state.Columns = Measure(ui, grid);
        state.RowHeight = ui.Theme.RowHeight;
        state.UpdateView(flags);

        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = ui.Theme.PaneBackground });
        Header(ui, id, grid, state, flags, t, listId);
        FlagsFlyout(ui, grid, state, flags, t, listId);
        state.UpdateView(flags);

        var result = default(MetadataGridResult);
        var focused = ui.ListArea(listId);
        if (focused)
            result = Keys(ui, grid, state, listId, state.RowHeight);
        Rows(ui, id, grid, state, t, listId, focused, ref result);
        var picked = Menu(ui, id, grid, state, t);
        if (picked != null)
            result = picked.Value;
        return result;
    }

    private static void Header(Ui ui, string id, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t, int listId)
    {
        var headers = grid.Columns
            .Select(static c => c.Name)
            .ToArray();
        var sortGlyph = state.Descending ? LucideIcons.ChevronDown : LucideIcons.ChevronUp;
        var sort = new TableSort(state.SortColumn, new UiIcon(sortGlyph, LucideIcons.Family));
        var clicked = ui.TableHeader($"{id}-header", state.Columns!, listId, headers, ui.Theme.ShellPaddingX, state.ScrollX, sort,
            (u, column, hovered) => Heading(u, grid, state, flags, t, listId, column, hovered));
        if (clicked < 0) return;
        if (clicked == state.EditingColumn) return;
        state.SortBy(clicked);
    }

    private static void Rows(Ui ui, string id, MetadataGrid grid, MetadataGridState state, Strings t, int listId, bool focused, ref MetadataGridResult result)
    {
        var columns = state.Columns!;
        var rowHeight = state.RowHeight;
        var width = columns.TotalWidth + ui.Theme.ShellPaddingX * 2;
        var heights = TallRows(ui, grid, state, rowHeight, width);
        var tall = heights.ConvertAll(static h => new TallRow(h.Index, h.Cells + h.Details));
        using var list = ui.VirtualScroll(listId, state.View.Length, rowHeight, ref state.ScrollX, ref state.ScrollY, default, tall);
        Arrive(state, list, rowHeight);
        for (var index = list.First; index < list.End; index++)
        {
            var row = state.RowAt(index);
            var height = heights.Find(h => h.Index == index);
            if (height.Cells <= 0f)
            {
                Row(ui, id, grid, state, t, columns, listId, row, width, rowHeight, focused, ref result);
                continue;
            }
            using (ui.Column(new Box { Width = width, Height = height.Cells + height.Details, Gap = 0f }))
            {
                Row(ui, id, grid, state, t, columns, listId, row, width, height.Cells, focused, ref result);
                if (height.Details > 0f)
                    MetadataDetailsView.Render(ui, grid, state.Details, t, row, height.Details);
            }
        }
    }

    private static void Row(Ui ui, string id, MetadataGrid grid, MetadataGridState state, Strings t, TableColumns columns, int listId, int row, float width,
        float height, bool focused, ref MetadataGridResult result)
    {
        var theme = ui.Theme;
        var hoveredRow = false;
        Span<Interaction> cells = stackalloc Interaction[grid.Columns.Count];
        for (var c = 0; c < cells.Length; c++)
        {
            cells[c] = ui.ListCell(listId, HashCode.Combine(listId, CellSalt, row, c), ref hoveredRow);
            ClickCell(ui, id, grid, state, cells[c], row, c, ref result);
        }

        var selected = row == state.Selected;
        using (ui.Row(new Box
        {
            Width = width,
            Height = height,
            Gap = 0f,
            Padding = new Spacing(theme.ShellPaddingX, 0f),
            Background = theme.PickRowBackground(selected, focused, hoveredRow),
        }))
        {
            for (var c = 0; c < cells.Length; c++)
            {
                var current = selected && c == state.CurrentColumn;
                Cell(ui, grid, state, t, columns, row, c, cells[c], current, focused, ref result);
            }
        }
    }

    private static void ClickCell(Ui ui, string id, MetadataGrid grid, MetadataGridState state, Interaction cell, int row, int column,
        ref MetadataGridResult result)
    {
        var rightClicked = cell.ClickedWith(PointerButton.Right);
        var middleClicked = cell.ClickedWith(PointerButton.Middle);
        var selects = cell.Pressed || rightClicked || middleClicked;
        if (selects)
            state.SelectCell(row, column);
        if (cell.Pressed && CanExpand(grid, state, row, column))
            state.Expand(row, column);
        if (cell.DoubleClicked || middleClicked)
            OpenDefinition(grid, row, !cell.DoubleClicked, ref result);
        if (rightClicked)
            ui.OpenContextMenu($"{id}-menu");
    }

    private static void OpenDefinition(MetadataGrid grid, int row, bool inNewTab, ref MetadataGridResult result)
    {
        var definition = grid.Definition(row);
        if (definition == null) return;
        result = new MetadataGridResult(null, definition, inNewTab);
    }

    private static bool CanExpand(MetadataGrid grid, MetadataGridState state, int row, int column)
    {
        if (state.IsOpenAt(row, column))
            return false;
        return grid.Lines(row, column).MoreLines > 0;
    }

    private static void Cell(Ui ui, MetadataGrid grid, MetadataGridState state, Strings t, TableColumns columns, int row, int c, Interaction cell,
        bool current, bool focused, ref MetadataGridResult result)
    {
        var theme = ui.Theme;
        var column = grid.Columns[c];
        var target = column.IsToken ? grid.Link(row, c) : null;
        var expanded = state.IsOpenAt(row, c);
        uint? color = theme.Text;
        UiIcon? link = null;
        if (target != null)
        {
            color = null;
            link = TokenLink;
        }
        float? size = null;
        string? family = null;
        if (IsMono(column))
        {
            size = theme.FontSize;
            family = MonoFonts.Regular;
        }
        string? more = null;
        if (!expanded)
            more = MoreLines(ui, grid, t, row, c, cell.Id);
        var followed = ui.TableCellText(columns, c, cell, state.ShownAt(row, c), state.Cell, current, focused, color, size, family, link: link,
            linkTooltip: t.MetadataGoToToken, suffix: more, expanded: expanded);
        if (followed)
            result = new MetadataGridResult(target, null, false);
        Tooltip(ui, grid, row, c, cell.Id);
    }

    private static bool IsMono(MetadataColumn column) => column.IsNumeric || column.IsToken;

    /// <summary>How many lines a closed multi-line cell hides, as its suffix; null for a single line.</summary>
    private static string? MoreLines(Ui ui, MetadataGrid grid, Strings t, int row, int column, int cellId)
    {
        var more = grid.Lines(row, column).MoreLines;
        if (more == 0) return null;
        using (ui.Id(cellId))
            return ui.Memo("more-lines", (more, t), static a => a.t.MetadataMoreLines(a.more));
    }

    private static TableColumns Measure(Ui ui, MetadataGrid grid)
    {
        var theme = ui.Theme;
        var padding = theme.GapTight * 2;
        var widths = new float[grid.Columns.Count];
        var rows = Math.Min(grid.RowCount, AutoFitRows);
        for (var c = 0; c < widths.Length; c++)
        {
            var width = MeasureColumn(ui, grid, c, rows);
            widths[c] = MathF.Ceiling(MathF.Min(MaxAutoWidth, width + padding + theme.GapTight));
        }
        return TableColumns.Sized(widths);
    }

    private static float MeasureColumn(Ui ui, MetadataGrid grid, int c, int rows)
    {
        var theme = ui.Theme;
        var column = grid.Columns[c];
        float? size = null;
        string? family = null;
        if (IsMono(column))
        {
            size = theme.FontSize;
            family = MonoFonts.Regular;
        }
        var width = ui.MeasureText(column.Name, theme.FontSizeSmall) + theme.IconSize * 2 + theme.GapTight * 2;
        for (var row = 0; row < rows; row++)
            width = MathF.Max(width, ui.MeasureText(grid.Text(row, c), size, family: family));
        return width;
    }

    private static void Heading(Ui ui, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t, int listId, int column, bool hovered)
    {
        var filter = state.FilterOf(column, flags);
        var textId = HashCode.Combine(listId, FilterTextSalt, column);
        if (state.EditingColumn == column)
            EditFilterText(ui, state, filter, textId);
        else if (filter.Text.Length > 0)
            FilterText(ui, state, filter, textId, column);
        else
            ColumnName(ui, grid.Columns[column].Name, hovered);
        Funnel(ui, grid, state, filter, t, listId, column);
    }

    private static void EditFilterText(Ui ui, MetadataGridState state, MetadataColumnFilter filter, int textId)
    {
        var text = filter.Text;
        using (ui.Id(textId))
        {
            var edit = ui.InlineEdit("filter", ref text, state.BeginEdit, Sizing.Grow());
            state.BeginEdit = false;
            filter.Text = edit == InlineEditResult.Cancelled ? "" : text;
            if (edit != InlineEditResult.Editing)
                state.StopEditingFilter();
        }
    }

    private static void FilterText(Ui ui, MetadataGridState state, MetadataColumnFilter filter, int textId, int column)
    {
        var theme = ui.Theme;
        var shown = ui.Interact(textId);
        if (shown.Hovered)
            ui.SetCursor(MouseCursor.Text);
        if (shown.Clicked)
            state.StartEditingFilter(column);
        ui.Text(filter.Text, new Box { Width = Sizing.Grow() }, color: theme.AccentText, size: theme.FontSizeSmall, id: shown.Id,
            overflow: TextOverflow.Ellipsis);
    }

    private static void ColumnName(Ui ui, string name, bool hovered)
    {
        var theme = ui.Theme;
        var color = hovered ? theme.Text : theme.TextMuted;
        ui.Text(name, new Box { Width = Sizing.Grow() }, color: color, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
    }

    private static void Funnel(Ui ui, MetadataGrid grid, MetadataGridState state, MetadataColumnFilter filter, Strings t, int listId, int column)
    {
        var theme = ui.Theme;
        var funnel = ui.Clickable(HashCode.Combine(listId, FunnelSalt, column));
        if (funnel.Clicked)
            ClickFunnel(state, filter, column);
        var glyph = FunnelGlyph(filter);
        var color = FunnelColor(theme, filter, funnel.Hovered);
        using (ui.Box(funnel.Id, new Box { Width = theme.IconSize, Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
            ui.Text(glyph, color: color, size: theme.FontSizeSmall, family: LucideIcons.Family);
        if (!state.IsFlagsFlyoutOpenFor(column))
            ui.Tooltip(funnel.Id, t.MetadataFilter(grid.Columns[column].Name));
    }

    private static void ClickFunnel(MetadataGridState state, MetadataColumnFilter filter, int column)
    {
        if (filter.Flags != null)
        {
            state.ToggleFlagsFlyout(column);
            return;
        }
        if (filter.Text.Length > 0)
        {
            filter.Text = "";
            state.StopEditingFilter();
            return;
        }
        state.StartEditingFilter(column);
    }

    private static string FunnelGlyph(MetadataColumnFilter filter)
    {
        if (filter.Flags != null)
            return LucideIcons.Filter;
        if (filter.Text.Length > 0)
            return LucideIcons.FilterX;
        return LucideIcons.Filter;
    }

    private static uint FunnelColor(UiTheme theme, MetadataColumnFilter filter, bool hovered)
    {
        if (filter.IsActive)
            return theme.AccentText;
        if (hovered)
            return theme.Text;
        return theme.TextMuted;
    }

    private static void FlagsFlyout(Ui ui, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t, int listId)
    {
        if (state.FlagsColumn < 0) return;
        var theme = ui.Theme;
        var filter = flags.For(grid, state.FlagsColumn);
        var width = FlagsFilterView.ContentWidth + theme.TooltipPaddingX * 2 + theme.ScrollBarHitWidth;
        using var flyout = ui.Flyout(HashCode.Combine(listId, FlyoutSalt), ref state.FlagsOpen, HashCode.Combine(listId, FunnelSalt, state.FlagsColumn),
            new Box { Width = Sizing.Fit(FlyoutMinWidth, width), Gap = theme.Gap });
        if (!flyout.IsOpen) return;
        FlagsFilterView.Render(ui, filter, t);
    }

    private readonly record struct RowHeights(int Index, float Cells, float Details);

    /// <summary>The rows in view order taller than one line: those with cells open in place, as
    /// tall as their longest, and the selected row with the details under it.</summary>
    private static List<RowHeights> TallRows(Ui ui, MetadataGrid grid, MetadataGridState state, float rowHeight, float width)
    {
        var cells = new Dictionary<int, float>();
        foreach (var ((row, _), lines) in state.Open)
            cells[row] = MathF.Max(cells.GetValueOrDefault(row, rowHeight), lines * rowHeight);
        var details = MetadataDetailsView.Height(ui, grid, state.Details, state.Selected, width);
        if (details > 0f)
            cells.TryAdd(state.Selected, rowHeight);
        var tall = new List<RowHeights>(cells.Count);
        foreach (var (row, height) in cells)
        {
            var index = state.IndexOf(row);
            if (index < 0) continue;
            var below = row == state.Selected ? details : 0f;
            tall.Add(new RowHeights(index, height, below));
        }
        tall.Sort(static (a, b) => a.Index.CompareTo(b.Index));
        return tall;
    }

    private static void Arrive(MetadataGridState state, Ui.VirtualList list, float rowHeight)
    {
        if (list.Viewport <= 0f) return;
        if (state.PendingRestore != null)
            ArriveAtRestored(state, list, rowHeight);
        else if (state.PendingReveal >= 0)
            ArriveAtRevealed(state, list, rowHeight);
    }

    private static void ArriveAtRestored(MetadataGridState state, Ui.VirtualList list, float rowHeight)
    {
        var restore = state.PendingRestore!.Value;
        state.PendingRestore = null;
        state.ScrollX = restore.ScrollX;
        list.ScrollTo(restore.TopLine * rowHeight);
        state.SelectCell(restore.Caret.Line, restore.Caret.Column);
    }

    private static void ArriveAtRevealed(MetadataGridState state, Ui.VirtualList list, float rowHeight)
    {
        var row = state.PendingReveal;
        state.PendingReveal = MetadataGridState.None;
        state.SelectCell(row, state.CurrentColumn);
        var index = state.IndexOf(row);
        if (index < 0) return;
        var top = list.TopOf(index);
        var outOfView = top < list.Offset || top + rowHeight > list.Offset + list.Viewport;
        if (outOfView)
            list.ScrollTo(MathF.Max(0f, top - list.Viewport / 3f));
    }

    private static void Tooltip(Ui ui, MetadataGrid grid, int row, int column, int cellId)
    {
        if (!ui.IsHovered(cellId)) return;
        var content = grid.Tooltip(row, column);
        if (content == null) return;
        using var tooltip = ui.TooltipContent(cellId);
        if (!tooltip.IsOpen) return;
        MetadataTooltipView.Render(ui, content);
    }

    private static MetadataGridResult? Menu(Ui ui, string id, MetadataGrid grid, MetadataGridState state, Strings t)
    {
        using var menu = ui.ContextMenu($"{id}-menu");
        if (!menu.IsOpen) return null;
        if (state.Selected < 0) return null;
        if (state.Selected >= grid.RowCount) return null;
        var row = state.Selected;
        var column = state.CurrentColumn;
        MetadataGridResult? result = null;
        if (ui.MenuItem(t.CodeCopy, "Ctrl+C", enabled: column >= 0, id: "copy"))
            ui.ClipboardText = state.CellText();
        if (ui.MenuItem(t.MetadataCopyRow, id: "copy-row"))
            ui.ClipboardText = state.RowText(row);
        var target = column >= 0 ? grid.Link(row, column) : null;
        var definition = grid.Definition(row);
        if (target == null && definition == null) return null;
        ui.MenuSeparator();
        if (target != null && ui.MenuItem(t.MetadataGoToToken, "Ctrl+G", id: "go-to-token"))
            result = new MetadataGridResult(target, null, false);
        if (definition != null)
        {
            if (ui.MenuItem(t.CodeGoToDefinition, "Enter", id: "go-to-definition"))
                result = new MetadataGridResult(null, definition, false);
            if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
                result = new MetadataGridResult(null, definition, true);
        }
        return result;
    }

    private static MetadataGridResult Keys(Ui ui, MetadataGrid grid, MetadataGridState state, int listId, float rowHeight)
    {
        var count = state.View.Length;
        if (count == 0) return default;
        var page = ui.MeasureListPage(listId, rowHeight);
        var result = default(MetadataGridResult);
        foreach (var stroke in ui.Input.Keys)
        {
            var index = state.IndexOf(state.Selected);
            if (UiWidgets.ApplyListKey(stroke, ref index, count, page))
            {
                state.SelectCell(state.RowAt(index), Math.Max(0, state.CurrentColumn));
                ui.ScrollRowIntoView(listId, index);
                continue;
            }
            switch (state.Cell.ApplyKey(stroke, 0, grid.Columns.Count - 1, out var column))
            {
                case TableCellKey.Column:
                    state.SelectCell(state.Selected, column);
                    continue;
                case TableCellKey.Copy:
                    ui.ClipboardText = state.CopiedText();
                    continue;
                case TableCellKey.Collapsed:
                    continue;
            }
            var modifiers = stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super);
            switch (modifiers, stroke.Key)
            {
                case (0, KeyboardKey.Space) when state.IsOpen:
                case (0, KeyboardKey.Escape) when state.IsOpen:
                    state.CollapseCurrent();
                    state.SelectAllText();
                    break;
                case (0, KeyboardKey.Space) when state.HasCurrentCell && grid.Lines(state.Selected, state.CurrentColumn).MoreLines > 0:
                    state.Expand(state.Selected, state.CurrentColumn);
                    state.SelectAllText();
                    break;
                case (KeyModifiers.Control, KeyboardKey.A) when state.HasCurrentCell:
                    state.SelectAllText();
                    break;
                case (0, KeyboardKey.Enter) when state.Selected >= 0 && grid.Definition(state.Selected) is { } definition:
                    result = new MetadataGridResult(null, definition, false);
                    break;
                case (KeyModifiers.Control, KeyboardKey.G) when state.HasCurrentCell && grid.Link(state.Selected, state.CurrentColumn) is { } target:
                    result = new MetadataGridResult(target, null, false);
                    break;
            }
        }
        return result;
    }
}

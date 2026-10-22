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
    public static MetadataGridResult MetadataGridView(this Ui ui, string id, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t)
    {
        var frame = new MetadataGridFrame(ui, id, grid, state, flags, t);
        return frame.Render();
    }
}

/// <summary>One frame of a <see cref="MetadataGridWidget.MetadataGridView"/>: what every step of
/// drawing it shares, kept on the stack.</summary>
internal ref struct MetadataGridFrame
{
    private const int AutoFitRows = 200;
    private const float MaxAutoWidth = 480f;
    private const float FlyoutMinWidth = 320f;
    private const int CellSalt = 0x4D444743;
    private const int FunnelSalt = 0x4D444746;
    private const int FilterTextSalt = 0x4D444754;
    private const int FlyoutSalt = 0x4D44464C;

    private static readonly UiIcon TokenLink = new(LucideIcons.ExternalLink, LucideIcons.Family);

    private readonly Ui _ui;
    private readonly string _id;
    private readonly MetadataGrid _grid;
    private readonly MetadataGridState _state;
    private readonly MetadataFilters _flags;
    private readonly Strings _t;
    private readonly int _listId;
    private bool _focused;
    private MetadataGridResult _result;

    public MetadataGridFrame(Ui ui, string id, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t)
    {
        _ui = ui;
        _id = id;
        _grid = grid;
        _state = state;
        _flags = flags;
        _t = t;
        _listId = ui.GetId(id);
    }

    public MetadataGridResult Render()
    {
        _state.Show(_grid);
        if (_state.Columns == null)
            _state.Columns = Measure();
        _state.RowHeight = _ui.Theme.RowHeight;
        _state.UpdateView(_flags);

        using var pane = _ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = _ui.Theme.PaneBackground });
        Header();
        FlagsFlyout();
        _state.UpdateView(_flags);

        _focused = _ui.ListArea(_listId);
        if (_focused)
            _result = Keys();
        Rows();
        var picked = Menu();
        if (picked != null)
            _result = picked.Value;
        return _result;
    }

    private TableColumns Measure()
    {
        var theme = _ui.Theme;
        var padding = theme.GapTight * 2;
        var widths = new float[_grid.Columns.Count];
        var rows = Math.Min(_grid.RowCount, AutoFitRows);
        for (var c = 0; c < widths.Length; c++)
        {
            var width = MeasureColumn(c, rows);
            widths[c] = MathF.Ceiling(MathF.Min(MaxAutoWidth, width + padding + theme.GapTight));
        }
        return TableColumns.Sized(widths);
    }

    private float MeasureColumn(int c, int rows)
    {
        var theme = _ui.Theme;
        var column = _grid.Columns[c];
        float? size = null;
        string? family = null;
        if (IsMono(column))
        {
            size = theme.FontSize;
            family = MonoFonts.Regular;
        }
        var width = _ui.MeasureText(column.Name, theme.FontSizeSmall) + theme.IconSize * 2 + theme.GapTight * 2;
        for (var row = 0; row < rows; row++)
            width = MathF.Max(width, _ui.MeasureText(_grid.Text(row, c), size, family: family));
        return width;
    }

    private static bool IsMono(MetadataColumn column) => column.IsNumeric || column.IsToken;

    private void Header()
    {
        var headers = _grid.Columns
            .Select(static c => c.Name)
            .ToArray();
        var sortGlyph = _state.Descending ? LucideIcons.ChevronDown : LucideIcons.ChevronUp;
        var sort = new TableSort(_state.SortColumn, new UiIcon(sortGlyph, LucideIcons.Family));
        var heading = CreateHeading(_grid, _state, _flags, _t, _listId);
        var clicked = _ui.TableHeader($"{_id}-header", _state.Columns!, _listId, headers, _ui.Theme.ShellPaddingX, _state.ScrollX, sort, heading);
        if (clicked < 0) return;
        if (clicked == _state.EditingColumn) return;
        _state.SortBy(clicked);
    }

    private static TableHeading CreateHeading(MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t, int listId)
    {
        return (ui, column, hovered) => Heading(ui, grid, state, flags, t, listId, column, hovered);
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

    private void FlagsFlyout()
    {
        if (_state.FlagsColumn < 0) return;
        var theme = _ui.Theme;
        var filter = _flags.For(_grid, _state.FlagsColumn);
        var width = FlagsFilterView.ContentWidth + theme.TooltipPaddingX * 2 + theme.ScrollBarHitWidth;
        using var flyout = _ui.Flyout(HashCode.Combine(_listId, FlyoutSalt), ref _state.FlagsOpen, HashCode.Combine(_listId, FunnelSalt, _state.FlagsColumn),
            new Box { Width = Sizing.Fit(FlyoutMinWidth, width), Gap = theme.Gap });
        if (!flyout.IsOpen) return;
        FlagsFilterView.Render(_ui, filter, _t);
    }

    private MetadataGridResult Keys()
    {
        var count = _state.View.Length;
        if (count == 0) return default;
        var page = _ui.MeasureListPage(_listId, _state.RowHeight);
        var result = default(MetadataGridResult);
        foreach (var stroke in _ui.Input.Keys)
        {
            var index = _state.IndexOf(_state.Selected);
            if (UiWidgets.ApplyListKey(stroke, ref index, count, page))
            {
                _state.SelectCell(_state.RowAt(index), Math.Max(0, _state.CurrentColumn));
                _ui.ScrollRowIntoView(_listId, index);
                continue;
            }
            switch (_state.Cell.ApplyKey(stroke, 0, _grid.Columns.Count - 1, out var column))
            {
                case TableCellKey.Column:
                    _state.SelectCell(_state.Selected, column);
                    continue;
                case TableCellKey.Copy:
                    _ui.ClipboardText = _state.CopiedText();
                    continue;
                case TableCellKey.Collapsed:
                    continue;
            }
            var modifiers = stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super);
            switch (modifiers, stroke.Key)
            {
                case (0, KeyboardKey.Space) when _state.IsOpen:
                case (0, KeyboardKey.Escape) when _state.IsOpen:
                    _state.CollapseCurrent();
                    _state.SelectAllText();
                    break;
                case (0, KeyboardKey.Space) when _state.HasCurrentCell && _grid.Lines(_state.Selected, _state.CurrentColumn).MoreLines > 0:
                    _state.Expand(_state.Selected, _state.CurrentColumn);
                    _state.SelectAllText();
                    break;
                case (KeyModifiers.Control, KeyboardKey.A) when _state.HasCurrentCell:
                    _state.SelectAllText();
                    break;
                case (0, KeyboardKey.Enter) when _state.Selected >= 0 && _grid.Definition(_state.Selected) is { } definition:
                    result = new MetadataGridResult(null, definition, false);
                    break;
                case (KeyModifiers.Control, KeyboardKey.G) when _state.HasCurrentCell && _grid.Link(_state.Selected, _state.CurrentColumn) is { } target:
                    result = new MetadataGridResult(target, null, false);
                    break;
            }
        }
        return result;
    }

    private void Rows()
    {
        var rowHeight = _state.RowHeight;
        var width = _state.Columns!.TotalWidth + _ui.Theme.ShellPaddingX * 2;
        var heights = TallRows(rowHeight, width);
        var tall = heights.ConvertAll(static h => new TallRow(h.Index, h.Cells + h.Details));
        using var list = _ui.VirtualScroll(_listId, _state.View.Length, rowHeight, ref _state.ScrollX, ref _state.ScrollY, default, tall);
        Arrive(list, rowHeight);
        for (var index = list.First; index < list.End; index++)
        {
            var row = _state.RowAt(index);
            var height = HeightOf(heights, index);
            if (height.Cells <= 0f)
            {
                Row(row, width, rowHeight);
                continue;
            }
            using (_ui.Column(new Box { Width = width, Height = height.Cells + height.Details, Gap = 0f }))
            {
                Row(row, width, height.Cells);
                if (height.Details > 0f)
                    MetadataDetailsView.Render(_ui, _grid, _state.Details, _t, row, height.Details);
            }
        }
    }

    private readonly record struct RowHeights(int Index, float Cells, float Details);

    /// <summary>The rows in view order taller than one line: those with cells open in place, as
    /// tall as their longest, and the selected row with the details under it.</summary>
    private List<RowHeights> TallRows(float rowHeight, float width)
    {
        var cells = new Dictionary<int, float>();
        foreach (var ((row, _), lines) in _state.Open)
            cells[row] = MathF.Max(cells.GetValueOrDefault(row, rowHeight), lines * rowHeight);
        var details = MetadataDetailsView.Height(_ui, _grid, _state.Details, _state.Selected, width);
        if (details > 0f)
            cells.TryAdd(_state.Selected, rowHeight);
        var tall = new List<RowHeights>(cells.Count);
        foreach (var (row, height) in cells)
        {
            var index = _state.IndexOf(row);
            if (index < 0) continue;
            var below = row == _state.Selected ? details : 0f;
            tall.Add(new RowHeights(index, height, below));
        }
        tall.Sort(static (a, b) => a.Index.CompareTo(b.Index));
        return tall;
    }

    private void Arrive(Ui.VirtualList list, float rowHeight)
    {
        if (list.Viewport <= 0f) return;
        if (_state.PendingRestore != null)
            ArriveAtRestored(list, rowHeight);
        else if (_state.PendingReveal >= 0)
            ArriveAtRevealed(list, rowHeight);
    }

    private void ArriveAtRestored(Ui.VirtualList list, float rowHeight)
    {
        var restore = _state.PendingRestore!.Value;
        _state.PendingRestore = null;
        _state.ScrollX = restore.ScrollX;
        list.ScrollTo(restore.TopLine * rowHeight);
        _state.SelectCell(restore.Caret.Line, restore.Caret.Column);
    }

    private void ArriveAtRevealed(Ui.VirtualList list, float rowHeight)
    {
        var row = _state.PendingReveal;
        _state.PendingReveal = MetadataGridState.None;
        _state.SelectCell(row, _state.CurrentColumn);
        var index = _state.IndexOf(row);
        if (index < 0) return;
        var top = list.TopOf(index);
        var outOfView = top < list.Offset || top + rowHeight > list.Offset + list.Viewport;
        if (outOfView)
            list.ScrollTo(MathF.Max(0f, top - list.Viewport / 3f));
    }

    private static RowHeights HeightOf(List<RowHeights> heights, int index)
    {
        foreach (var height in heights)
        {
            if (height.Index == index)
                return height;
        }
        return default;
    }

    private void Row(int row, float width, float height)
    {
        var theme = _ui.Theme;
        var hoveredRow = false;
        Span<Interaction> cells = stackalloc Interaction[_grid.Columns.Count];
        for (var c = 0; c < cells.Length; c++)
        {
            cells[c] = _ui.ListCell(_listId, HashCode.Combine(_listId, CellSalt, row, c), ref hoveredRow);
            ClickCell(cells[c], row, c);
        }

        var selected = row == _state.Selected;
        using (_ui.Row(new Box
        {
            Width = width,
            Height = height,
            Gap = 0f,
            Padding = new Spacing(theme.ShellPaddingX, 0f),
            Background = theme.PickRowBackground(selected, _focused, hoveredRow),
        }))
        {
            for (var c = 0; c < cells.Length; c++)
            {
                var current = selected && c == _state.CurrentColumn;
                Cell(row, c, cells[c], current);
            }
        }
    }

    private void ClickCell(Interaction cell, int row, int column)
    {
        var rightClicked = cell.ClickedWith(PointerButton.Right);
        var middleClicked = cell.ClickedWith(PointerButton.Middle);
        var selects = cell.Pressed || rightClicked || middleClicked;
        if (selects)
            _state.SelectCell(row, column);
        if (cell.Pressed && CanExpand(row, column))
            _state.Expand(row, column);
        if (cell.DoubleClicked || middleClicked)
            OpenDefinition(row, !cell.DoubleClicked);
        if (rightClicked)
            _ui.OpenContextMenu($"{_id}-menu");
    }

    private bool CanExpand(int row, int column)
    {
        if (_state.IsOpenAt(row, column))
            return false;
        return _grid.Lines(row, column).MoreLines > 0;
    }

    private void OpenDefinition(int row, bool inNewTab)
    {
        var definition = _grid.Definition(row);
        if (definition == null) return;
        _result = new MetadataGridResult(null, definition, inNewTab);
    }

    private void Cell(int row, int c, Interaction cell, bool current)
    {
        var theme = _ui.Theme;
        var column = _grid.Columns[c];
        var target = column.IsToken ? _grid.Link(row, c) : null;
        var expanded = _state.IsOpenAt(row, c);
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
            more = MoreLines(row, c, cell.Id);
        var followed = _ui.TableCellText(_state.Columns!, c, cell, _state.ShownAt(row, c), _state.Cell, current, _focused, color, size, family, link: link,
            linkTooltip: _t.MetadataGoToToken, suffix: more, expanded: expanded);
        if (followed)
            _result = new MetadataGridResult(target, null, false);
        Tooltip(row, c, cell.Id);
    }

    /// <summary>How many lines a closed multi-line cell hides, as its suffix; null for a single line.</summary>
    private string? MoreLines(int row, int column, int cellId)
    {
        var more = _grid.Lines(row, column).MoreLines;
        if (more == 0) return null;
        var t = _t;
        using (_ui.Id(cellId))
            return _ui.Memo("more-lines", (more, t), static a => a.t.MetadataMoreLines(a.more));
    }

    private void Tooltip(int row, int column, int cellId)
    {
        if (!_ui.IsHovered(cellId)) return;
        var content = _grid.Tooltip(row, column);
        if (content == null) return;
        using var tooltip = _ui.TooltipContent(cellId);
        if (!tooltip.IsOpen) return;
        MetadataTooltipView.Render(_ui, content);
    }

    private MetadataGridResult? Menu()
    {
        using var menu = _ui.ContextMenu($"{_id}-menu");
        if (!menu.IsOpen) return null;
        if (_state.Selected < 0) return null;
        if (_state.Selected >= _grid.RowCount) return null;
        var row = _state.Selected;
        var column = _state.CurrentColumn;
        MetadataGridResult? result = null;
        if (_ui.MenuItem(_t.CodeCopy, "Ctrl+C", enabled: column >= 0, id: "copy"))
            _ui.ClipboardText = _state.CellText();
        if (_ui.MenuItem(_t.MetadataCopyRow, id: "copy-row"))
            _ui.ClipboardText = _state.RowText(row);
        var target = column >= 0 ? _grid.Link(row, column) : null;
        var definition = _grid.Definition(row);
        if (target == null && definition == null) return null;
        _ui.MenuSeparator();
        if (target != null && _ui.MenuItem(_t.MetadataGoToToken, "Ctrl+G", id: "go-to-token"))
            result = new MetadataGridResult(target, null, false);
        if (definition != null)
        {
            if (_ui.MenuItem(_t.CodeGoToDefinition, "Enter", id: "go-to-definition"))
                result = new MetadataGridResult(null, definition, false);
            if (_ui.MenuItem(_t.CodeOpenInNewTab, id: "open-in-new-tab"))
                result = new MetadataGridResult(null, definition, true);
        }
        return result;
    }
}

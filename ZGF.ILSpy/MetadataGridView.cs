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
    private const int FlyoutSalt = 0x4D44464C;
    private const int MenuSalt = 0x4D44474D;

    private static readonly UiIcon TokenLink = new(LucideIcons.ExternalLink, LucideIcons.Family);

    private readonly Ui _ui;
    private readonly MetadataGrid _grid;
    private readonly MetadataGridState _state;
    private readonly MetadataFilters _flags;
    private readonly Strings _t;
    private readonly int _listId;
    private readonly int _menuId;
    private bool _focused;
    private MetadataGridResult _result;

    public MetadataGridFrame(Ui ui, string id, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t)
    {
        _ui = ui;
        _grid = grid;
        _state = state;
        _flags = flags;
        _t = t;
        _listId = ui.GetId(id);
        _menuId = HashCode.Combine(_listId, MenuSalt);
    }

    public MetadataGridResult Render()
    {
        _state.Show(_grid);
        if (_state.Columns == null)
            _state.Columns = Measure();
        _state.RowHeight = _ui.Theme.RowHeight;
        _state.UpdateView(_flags);

        using var pane = _ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = _ui.Theme.PaneBackground });
        var table = _ui.Table(_listId, _state.Columns!, _state.Cell, _state.Scroll);
        Header(table);
        FlagsFlyout();
        _state.UpdateView(_flags);

        _focused = table.Focused;
        table.TakeKeys(_state.View.Length);
        if (_focused)
            _result = Keys();
        Rows(table);
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

    private void Header(TableFrame table)
    {
        var sortGlyph = _state.Descending ? LucideIcons.ChevronDown : LucideIcons.ChevronUp;
        var sort = new TableSort(_state.SortColumn, new UiIcon(sortGlyph, LucideIcons.Family));
        var heading = new MetadataGridHeading(_grid, _state, _flags, _t, _listId);
        var clicked = table.Header(_state.Headers, sort, MetadataGridHeading.Render, heading);
        if (clicked < 0) return;
        if (clicked == _state.EditingColumn) return;
        _state.SortBy(clicked);
    }

    private void FlagsFlyout()
    {
        if (_state.FlagsColumn < 0) return;
        var theme = _ui.Theme;
        var filter = _flags.For(_grid, _state.FlagsColumn);
        var width = FlagsFilterView.ContentWidth + theme.TooltipPaddingX * 2 + theme.ScrollBarHitWidth;
        var funnelId = MetadataGridHeading.FunnelId(_listId, _state.FlagsColumn);
        using var flyout = _ui.Flyout(HashCode.Combine(_listId, FlyoutSalt), ref _state.FlagsOpen, funnelId,
            new Box { Width = Sizing.Fit(FlyoutMinWidth, width), Gap = theme.Gap });
        if (!flyout.IsOpen) return;
        FlagsFilterView.Render(_ui, filter, _t);
    }

    private MetadataGridResult Keys()
    {
        if (_state.View.Length == 0) return default;
        var result = default(MetadataGridResult);
        foreach (var stroke in _ui.Input.Keys)
        {
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
                case (KeyModifiers.Control, KeyboardKey.G) when _state.HasCurrentCell && _grid.Link(_state.Selected, _state.CurrentColumn) is { } target:
                    result = new MetadataGridResult(target, null, false);
                    break;
            }
        }
        return result;
    }

    private void Rows(TableFrame table)
    {
        var rowHeight = _state.RowHeight;
        var width = _state.Columns!.TotalWidth + _ui.Theme.ShellPaddingX * 2;
        var details = MetadataDetailsView.Height(_ui, _grid, _state.Details, _state.Selected, width);
        var tall = _state.TallRows;
        tall.Measure(_state, rowHeight, details);
        using var body = table.Body(_state.View.Length, tall: tall.Rows);
        Arrive(body, rowHeight);
        for (var index = body.First; index < body.End; index++)
        {
            var row = _state.RowAt(index);
            var height = tall.At(index);
            if (height.Cells <= 0f)
            {
                Row(body, index, row, rowHeight);
                continue;
            }
            using (_ui.Column(new Box { Width = width, Height = height.Cells + height.Details, Gap = 0f }))
            {
                Row(body, index, row, height.Cells);
                if (height.Details > 0f)
                    MetadataDetailsView.Render(_ui, _grid, _state.Details, _t, row, height.Details);
            }
        }
        Answer(body);
    }

    private void Arrive(TableBody body, float rowHeight)
    {
        if (body.Viewport <= 0f) return;
        if (_state.PendingRestore != null)
            ArriveAtRestored(body, rowHeight);
        else if (_state.PendingReveal >= 0)
            ArriveAtRevealed(body, rowHeight);
    }

    private void ArriveAtRestored(TableBody body, float rowHeight)
    {
        var restore = _state.PendingRestore!.Value;
        _state.PendingRestore = null;
        _state.Scroll.X = restore.ScrollX;
        body.ScrollTo(restore.TopLine * rowHeight);
        _state.SelectCell(restore.Caret.Line, restore.Caret.Column);
    }

    private void ArriveAtRevealed(TableBody body, float rowHeight)
    {
        var row = _state.PendingReveal;
        _state.PendingReveal = MetadataGridState.None;
        _state.SelectCell(row, _state.CurrentColumn);
        var index = _state.IndexOf(row);
        if (index < 0) return;
        var top = body.TopOf(index);
        var outOfView = top < body.Offset || top + rowHeight > body.Offset + body.Viewport;
        if (outOfView)
            body.ScrollTo(MathF.Max(0f, top - body.Viewport / 3f));
    }

    private void Row(TableBody body, int index, int row, float height)
    {
        using var cells = body.Row(index, row, height);
        for (var c = 0; c < _grid.Columns.Count; c++)
        {
            ExpandOnPress(cells.CellAt(c), row, c);
            Cell(cells, row, c);
        }
    }

    private void ExpandOnPress(Interaction cell, int row, int column)
    {
        if (!cell.Pressed) return;
        if (!CanExpand(row, column)) return;
        _state.Expand(row, column);
    }

    private bool CanExpand(int row, int column)
    {
        if (_state.IsOpenAt(row, column))
            return false;
        return _grid.Lines(row, column).MoreLines > 0;
    }

    /// <summary>Does what the user asked of the rows this frame: open, copy or show the menu.</summary>
    private void Answer(TableBody body)
    {
        if (body.Activated)
            OpenDefinition(_state.Selected, body.InNewTab);
        if (body.CopyRequested)
            _ui.ClipboardText = _state.CopiedText();
        if (body.ContextMenuRequested)
            _ui.OpenContextMenu(_menuId, body.ContextMenuAt);
    }

    private void OpenDefinition(int row, bool inNewTab)
    {
        var definition = _grid.Definition(row);
        if (definition == null) return;
        _result = new MetadataGridResult(null, definition, inNewTab);
    }

    private void Cell(TableRow cells, int row, int c)
    {
        var theme = _ui.Theme;
        var column = _grid.Columns[c];
        var cellId = cells.CellAt(c).Id;
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
            more = MoreLines(row, c, cellId);
        var followed = cells.TextCell(c, _state.ShownAt(row, c), color, size, family, link: link, linkTooltip: _t.MetadataGoToToken, suffix: more,
            expanded: expanded);
        if (followed)
            _result = new MetadataGridResult(target, null, false);
        Tooltip(row, c, cellId);
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
        using var menu = _ui.ContextMenu(_menuId);
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

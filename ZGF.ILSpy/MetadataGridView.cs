using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>Everything a <see cref="MetadataGridWidget.MetadataGridView"/> keeps between frames:
/// the columns' widths, the sort and text filters, the selected cell and the text selected in it,
/// the scroll and the selected row's decoded details. One per open tab, owned by the app.</summary>
public sealed class MetadataGridState
{
    internal const int None = -1;

    internal MetadataGrid? Grid;
    internal TableColumns? Columns;
    internal MetadataColumnFilter?[] Filters = [];
    internal int SortColumn = None;
    internal bool Descending;
    internal int[]? Order;
    internal int[] View = [];
    internal int ViewKey;
    internal readonly TableCellSelection Cell = new();
    internal float ScrollX;
    internal float ScrollY;
    internal float RowHeight;
    internal int PendingReveal = None;
    internal CodeViewPosition? PendingRestore;
    internal int EditingColumn = None;
    internal bool BeginEdit;
    internal int FlagsColumn = None;
    internal bool FlagsOpen;
    internal readonly Dictionary<(int Row, int Column), int> Open = new();
    internal readonly Dictionary<int, (DecompiledDocument Document, CodeViewState View)> DetailTexts = new();
    internal readonly Dictionary<int, (int Lines, int Longest)> DetailSizes = new();

    /// <summary>Where the view stands, as a code view's position: the top row as the top line, the
    /// selected row and cell as the caret.</summary>
    public CodeViewPosition Position =>
        new(RowHeight > 0f ? (int)(ScrollY / RowHeight) : 0, ScrollX, default, new TextPosition(Selected, CurrentColumn));

    /// <summary>Starts over on <paramref name="grid"/>: unsorted, unfiltered but for the flags
    /// filters kept for its kind of rows, nothing selected, at the top.</summary>
    public void Show(MetadataGrid grid)
    {
        if (ReferenceEquals(grid, Grid)) return;
        Grid = grid;
        Columns = null;
        Filters = new MetadataColumnFilter?[grid.Columns.Count];
        SortColumn = PendingReveal = EditingColumn = FlagsColumn = None;
        Cell.Clear();
        Descending = BeginEdit = FlagsOpen = false;
        Order = null;
        View = [];
        ViewKey = 0;
        ScrollX = ScrollY = 0f;
        PendingRestore = null;
        Open.Clear();
        DetailTexts.Clear();
        DetailSizes.Clear();
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

    /// <summary>Makes <paramref name="row"/>'s cell in <paramref name="column"/> the current one
    /// with all its text selected.</summary>
    internal void SelectCell(int row, int column) => Cell.Select(row, column, ShownAt(row, column));

    /// <summary>The selected row, or <see cref="None"/>.</summary>
    internal int Selected => Cell.Row;

    /// <summary>The current cell's column, or <see cref="None"/>.</summary>
    internal int CurrentColumn => Cell.Column;

    /// <summary>Whether the current cell is a multi-line one opened in place.</summary>
    internal bool IsOpen => Open.ContainsKey((Selected, CurrentColumn));

    /// <summary>The current cell's text as shown: all of it while open, else its first line.</summary>
    internal string ShownText => ShownAt(Selected, CurrentColumn);

    internal void SelectAllText() => Cell.SelectAll(ShownText);

    internal string ShownAt(int row, int column) =>
        row < 0 || column < 0 ? "" : Open.ContainsKey((row, column)) ? Grid!.Text(row, column) : Grid!.Lines(row, column).FirstLine;

    internal int RowAt(int index) => View[index];

    internal int IndexOf(int row) => Array.IndexOf(View, row);
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
    private const float FlyoutMinWidth = 320f;
    private const int MaxTextLines = 12;
    private const float TextPaddingX = 24f;
    private const float MaxTableDetails = 250f;
    private const int CellSalt = 0x4D444743;
    private const int FunnelSalt = 0x4D444746;
    private const int FilterTextSalt = 0x4D444754;
    private const int FlyoutSalt = 0x4D44464C;

    private static readonly UiIcon TokenLink = new(LucideIcons.ExternalLink, LucideIcons.Family);

    public static MetadataGridResult MetadataGridView(this Ui ui, string id, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t)
    {
        var theme = ui.Theme;
        var listId = ui.GetId(id);
        state.Show(grid);
        var columns = state.Columns ??= Measure(ui, grid);
        var rowHeight = state.RowHeight = theme.RowHeight;
        var result = default(MetadataGridResult);
        UpdateView(grid, state, flags);

        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = theme.PaneBackground });

        var headers = grid.Columns.Select(static c => c.Name).ToArray();
        var sortIcon = new UiIcon(state.Descending ? LucideIcons.ChevronDown : LucideIcons.ChevronUp, LucideIcons.Family);
        var clicked = ui.TableHeader($"{id}-header", columns, listId, headers, theme.ShellPaddingX, state.ScrollX, new TableSort(state.SortColumn, sortIcon),
            (u, column, hovered) => Heading(u, grid, state, flags, t, listId, column, hovered));
        if (clicked >= 0 && clicked != state.EditingColumn)
            Sort(state, clicked);
        FlagsFlyout(ui, grid, state, flags, t, listId);
        UpdateView(grid, state, flags);

        var focused = ui.ListArea(listId);
        if (focused)
            result = Keys(ui, grid, state, listId, rowHeight);

        var width = columns.TotalWidth + theme.ShellPaddingX * 2;
        var heights = TallRows(ui, grid, state, rowHeight, width);
        var tall = heights.ConvertAll(static h => new TallRow(h.Index, h.Cells + h.Details));
        using (var list = ui.VirtualScroll(listId, state.View.Length, rowHeight, ref state.ScrollX, ref state.ScrollY, default, tall))
        {
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
                        Details(ui, grid, state, t, row, height.Details);
                }
            }
        }

        if (Menu(ui, id, grid, state, t) is { } picked)
            result = picked;
        return result;
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
            if (cells[c].Pressed || cells[c].ClickedWith(PointerButton.Right) || cells[c].ClickedWith(PointerButton.Middle))
            {
                state.SelectCell(row, c);
                if (cells[c].Pressed && !state.Open.ContainsKey((row, c)) && grid.Lines(row, c).MoreLines > 0)
                    Expand(grid, state, row, c);
            }
            if (cells[c].DoubleClicked && grid.Definition(row) is { } definition)
                result = new MetadataGridResult(null, definition, false);
            else if (cells[c].ClickedWith(PointerButton.Middle) && grid.Definition(row) is { } inNewTab)
                result = new MetadataGridResult(null, inNewTab, true);
            if (cells[c].ClickedWith(PointerButton.Right))
                ui.OpenContextMenu($"{id}-menu");
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

    private static void Cell(Ui ui, MetadataGrid grid, MetadataGridState state, Strings t, TableColumns columns, int row, int c, Interaction cell,
        bool current, bool focused, ref MetadataGridResult result)
    {
        var theme = ui.Theme;
        var column = grid.Columns[c];
        var target = column.IsToken ? grid.Link(row, c) : null;
        var expanded = state.Open.ContainsKey((row, c));
        uint? color = theme.Text;
        UiIcon? link = null;
        if (target != null)
        {
            color = null;
            link = TokenLink;
        }
        float? size = null;
        string? family = null;
        if (column.IsNumeric || column.IsToken)
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

    /// <summary>How many lines a closed multi-line cell hides, as its suffix; null for a single line.</summary>
    private static string? MoreLines(Ui ui, MetadataGrid grid, Strings t, int row, int column, int cellId)
    {
        var more = grid.Lines(row, column).MoreLines;
        if (more == 0) return null;
        using (ui.Id(cellId))
            return ui.Memo("more-lines", (more, t), static a => a.t.MetadataMoreLines(a.more));
    }

    /// <summary>Opens <paramref name="row"/>'s multi-line cell in <paramref name="column"/> in place:
    /// the cell grown downward to show every line, which leaves the rows above where they were.</summary>
    private static void Expand(MetadataGrid grid, MetadataGridState state, int row, int column) =>
        state.Open[(row, column)] = grid.Text(row, column).Count(static ch => ch == '\n') + 1;

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
            var width = ui.MeasureText(column.Name, theme.FontSizeSmall) + theme.IconSize * 2 + theme.GapTight * 2;
            for (var row = 0; row < rows; row++)
                width = MathF.Max(width, ui.MeasureText(grid.Text(row, c), mono ? theme.FontSize : null, family: mono ? MonoFonts.Regular : null));
            widths[c] = MathF.Ceiling(MathF.Min(MaxAutoWidth, width + padding + theme.GapTight));
        }
        return TableColumns.Sized(widths);
    }

    /// <summary>Filters and sorts the rows again when a filter or the sort changed.</summary>
    private static void UpdateView(MetadataGrid grid, MetadataGridState state, MetadataFilters flags)
    {
        var key = new HashCode();
        key.Add(state.SortColumn);
        key.Add(state.Descending);
        for (var c = 0; c < grid.Columns.Count; c++)
        {
            if (grid.Columns[c].Flags != null)
            {
                var filter = state.Filters[c] ??= new MetadataColumnFilter { Flags = flags.For(grid, c) };
                key.Add(filter.Flags!.Revision);
            }
            key.Add(state.Filters[c]?.Text);
        }
        var hash = key.ToHashCode() | 1;
        if (hash == state.ViewKey) return;
        state.ViewKey = hash;
        state.View = grid.Filter(state.Order, state.Filters);
    }

    private static void Heading(Ui ui, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t, int listId, int column, bool hovered)
    {
        var theme = ui.Theme;
        var info = grid.Columns[column];
        var filter = state.Filters[column] ??= info.Flags != null ? new MetadataColumnFilter { Flags = flags.For(grid, column) } : new MetadataColumnFilter();
        var textId = HashCode.Combine(listId, FilterTextSalt, column);
        if (state.EditingColumn == column)
        {
            var text = filter.Text;
            using (ui.Id(textId))
            {
                var edit = ui.InlineEdit("filter", ref text, state.BeginEdit, Sizing.Grow());
                state.BeginEdit = false;
                filter.Text = edit == InlineEditResult.Cancelled ? "" : text;
                if (edit != InlineEditResult.Editing)
                    state.EditingColumn = MetadataGridState.None;
            }
        }
        else if (filter.Text.Length > 0)
        {
            var shown = ui.Interact(textId);
            if (shown.Hovered)
                ui.SetCursor(MouseCursor.Text);
            if (shown.Clicked)
                StartEditing(state, column);
            ui.Text(filter.Text, new Box { Width = Sizing.Grow() }, color: theme.AccentText, size: theme.FontSizeSmall, id: shown.Id,
                overflow: TextOverflow.Ellipsis);
        }
        else
        {
            ui.Text(info.Name, new Box { Width = Sizing.Grow() }, color: hovered ? theme.Text : theme.TextMuted, size: theme.FontSizeSmall,
                overflow: TextOverflow.Ellipsis);
        }

        var isFlags = filter.Flags != null;
        var flagsActive = filter.Flags is { IsEmpty: false };
        var funnel = ui.Clickable(HashCode.Combine(listId, FunnelSalt, column));
        if (funnel.Clicked)
        {
            if (isFlags)
            {
                state.FlagsOpen = !(state.FlagsOpen && state.FlagsColumn == column);
                state.FlagsColumn = column;
            }
            else if (filter.Text.Length > 0)
            {
                filter.Text = "";
                state.EditingColumn = MetadataGridState.None;
            }
            else
            {
                StartEditing(state, column);
            }
        }
        var glyph = !isFlags && filter.Text.Length > 0 ? LucideIcons.FilterX : LucideIcons.Filter;
        var color = flagsActive || filter.Text.Length > 0 ? theme.AccentText : funnel.Hovered ? theme.Text : theme.TextMuted;
        using (ui.Box(funnel.Id, new Box { Width = theme.IconSize, Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
            ui.Text(glyph, color: color, size: theme.FontSizeSmall, family: LucideIcons.Family);
        if (!(state.FlagsOpen && state.FlagsColumn == column))
            ui.Tooltip(funnel.Id, t.MetadataFilter(info.Name));
    }

    private static void StartEditing(MetadataGridState state, int column)
    {
        state.EditingColumn = column;
        state.BeginEdit = true;
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
        var details = state.Selected >= 0 && grid.HasDetails && grid.Details(state.Selected) is { } found
            ? DetailsHeight(ui, state, state.Selected, found, width)
            : 0f;
        if (details > 0f)
            cells.TryAdd(state.Selected, rowHeight);
        var tall = new List<RowHeights>(cells.Count);
        foreach (var (row, height) in cells)
        {
            if (state.IndexOf(row) is >= 0 and var index)
                tall.Add(new RowHeights(index, height, row == state.Selected ? details : 0f));
        }
        tall.Sort(static (a, b) => a.Index.CompareTo(b.Index));
        return tall;
    }

    private static float DetailsHeight(Ui ui, MetadataGridState state, int row, MetadataRowDetails details, float width)
    {
        var theme = ui.Theme;
        var padding = theme.GapTight * 2;
        switch (details)
        {
            case MetadataTextDetails text:
                if (!state.DetailSizes.TryGetValue(row, out var size))
                    state.DetailSizes[row] = size = Size(text.Text);
                return TextHeight(ui, size, width - DetailsLeft(theme) - theme.ShellPaddingX) + padding;
            case MetadataTableDetails table:
                return MathF.Min(MaxTableDetails, (table.Rows.Count + 1) * theme.RowHeight + padding);
            default:
                return 0f;
        }
    }

    private static (int Lines, int Longest) Size(string text)
    {
        var lines = text.Split('\n');
        return (lines.Length, lines.Max(static l => l.TrimEnd('\r').Length));
    }

    /// <summary>How tall a text is in a code view <paramref name="width"/> wide: a code line per
    /// line, up to <see cref="MaxTextLines"/> before it scrolls, plus the horizontal scrollbar's room
    /// when its longest line is wider.</summary>
    private static float TextHeight(Ui ui, (int Lines, int Longest) size, float width)
    {
        var theme = ui.Theme;
        var cell = ui.MeasureCell(theme.MonoFontSize, family: MonoFonts.Regular);
        var lineHeight = MathF.Max(theme.CodeLineHeight, MathF.Ceiling(cell.Height));
        var overflows = TextPaddingX + size.Longest * cell.Advance > width;
        var bar = overflows ? theme.ScrollBarWidth + theme.ScrollBarInset * 2 : 0f;
        return Math.Min(size.Lines, MaxTextLines) * lineHeight + bar;
    }

    private static float DetailsLeft(UiTheme theme) => theme.ShellPaddingX + theme.IconSize;

    private static void Details(Ui ui, MetadataGrid grid, MetadataGridState state, Strings t, int row, float height)
    {
        var theme = ui.Theme;
        using var box = ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Height = height,
            Gap = 0f,
            Padding = new Spacing(DetailsLeft(theme), theme.GapTight, theme.ShellPaddingX, theme.GapTight),
            Background = theme.Surface,
        });
        using var scope = ui.Id(row);
        switch (grid.Details(row))
        {
            case MetadataTextDetails text:
                if (!state.DetailTexts.TryGetValue(row, out var shown))
                    state.DetailTexts[row] = shown = (DecompiledDocument.FromText(grid.Title, text.Text), new CodeViewState());
                ui.CodeView("details", shown.Document, shown.View, t, gutter: false);
                break;
            case MetadataTableDetails table:
                var share = 1f / table.Headers.Count;
                using (ui.Row(new Box { Height = theme.RowHeight, Gap = theme.GapTight, AlignY = Align.Center }))
                {
                    foreach (var header in table.Headers)
                        ui.Text(header, new Box { Width = Sizing.Percent(share) }, color: theme.TextMuted, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
                }
                using (ui.Scroll("details", new Box { Height = Sizing.Grow(), Gap = 0f }))
                {
                    foreach (var cells in table.Rows)
                    {
                        using (ui.Row(new Box { Height = theme.RowHeight, Gap = theme.GapTight, AlignY = Align.Center }))
                        {
                            foreach (var cell in cells)
                                ui.Text(cell, new Box { Width = Sizing.Percent(share) }, overflow: TextOverflow.Ellipsis);
                        }
                    }
                }
                break;
        }
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
            state.SelectCell(restore.Caret.Line, restore.Caret.Column);
        }
        else if (state.PendingReveal is >= 0 and var row)
        {
            state.PendingReveal = MetadataGridState.None;
            state.SelectCell(row, state.CurrentColumn);
            var index = state.IndexOf(row);
            if (index < 0) return;
            var top = list.TopOf(index);
            if (top < list.Offset || top + rowHeight > list.Offset + list.Viewport)
                list.ScrollTo(MathF.Max(0f, top - list.Viewport / 3f));
        }
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
        if (!menu.IsOpen || state.Selected < 0 || state.Selected >= grid.RowCount) return null;
        var row = state.Selected;
        var column = state.CurrentColumn;
        MetadataGridResult? result = null;
        if (ui.MenuItem(t.CodeCopy, "Ctrl+C", enabled: column >= 0, id: "copy"))
            ui.ClipboardText = CellText(grid, state);
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

    /// <summary>What copying the current cell takes: the part of its text selected, else all of it,
    /// lines hidden while it is closed included.</summary>
    private static string CellText(MetadataGrid grid, MetadataGridState state) =>
        state.Cell.Copied(state.ShownText, grid.Text(state.Selected, state.CurrentColumn));

    /// <summary>What copying takes: the current cell's text, or the whole row's without one.</summary>
    private static string CopiedText(MetadataGrid grid, MetadataGridState state)
    {
        if (state.CurrentColumn < 0) return RowText(grid, state.Selected);
        return CellText(grid, state);
    }

    private static string RowText(MetadataGrid grid, int row) =>
        string.Join('\t', Enumerable.Range(0, grid.Columns.Count).Select(c => grid.Text(row, c)));

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
                    ui.ClipboardText = CopiedText(grid, state);
                    continue;
                case TableCellKey.Collapsed:
                    continue;
            }
            var modifiers = stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super);
            switch (modifiers, stroke.Key)
            {
                case (0, KeyboardKey.Space) when state.IsOpen:
                case (0, KeyboardKey.Escape) when state.IsOpen:
                    state.Open.Remove((state.Selected, state.CurrentColumn));
                    state.SelectAllText();
                    break;
                case (0, KeyboardKey.Space) when state is { Selected: >= 0, CurrentColumn: >= 0 } && grid.Lines(state.Selected, state.CurrentColumn).MoreLines > 0:
                    Expand(grid, state, state.Selected, state.CurrentColumn);
                    state.SelectAllText();
                    break;
                case (KeyModifiers.Control, KeyboardKey.A) when state is { Selected: >= 0, CurrentColumn: >= 0 }:
                    state.SelectAllText();
                    break;
                case (0, KeyboardKey.Enter) when state.Selected >= 0 && grid.Definition(state.Selected) is { } definition:
                    result = new MetadataGridResult(null, definition, false);
                    break;
                case (KeyModifiers.Control, KeyboardKey.G) when state is { Selected: >= 0, CurrentColumn: >= 0 }
                                                                && grid.Link(state.Selected, state.CurrentColumn) is { } target:
                    result = new MetadataGridResult(target, null, false);
                    break;
            }
        }
        return result;
    }
}

using System.Collections.Immutable;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>Everything a <see cref="MetadataGridWidget.MetadataGridView"/> keeps between frames:
/// the columns' widths, the sort and text filters, the selected row and cell, the scroll and the
/// selected row's decoded details. One per open tab, owned by the app.</summary>
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
    internal int Selected = None;
    internal int CurrentColumn = None;
    internal float ScrollX;
    internal float ScrollY;
    internal float RowHeight;
    internal int PendingReveal = None;
    internal CodeViewPosition? PendingRestore;
    internal bool ScrollToSelected;
    internal int EditingColumn = None;
    internal bool BeginEdit;
    internal int FlagsColumn = None;
    internal bool FlagsOpen;
    internal readonly HashSet<int> Unfolded = new();
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
        SortColumn = Selected = CurrentColumn = PendingReveal = EditingColumn = FlagsColumn = None;
        Descending = ScrollToSelected = BeginEdit = FlagsOpen = false;
        Order = null;
        View = [];
        ViewKey = 0;
        ScrollX = ScrollY = 0f;
        PendingRestore = null;
        DetailTexts.Clear();
        DetailSizes.Clear();
        Unfolded.Clear();
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
    private const float FlyoutContentWidth = 440f;
    private const int MaxTextDetailLines = 12;
    private const float TextDetailPaddingX = 24f;
    private const float MaxTableDetails = 250f;
    private const int CellSalt = 0x4D444743;
    private const int FunnelSalt = 0x4D444746;
    private const int FilterTextSalt = 0x4D444754;
    private const int FlyoutSalt = 0x4D44464C;
    private const int FoldSalt = 0x4D44464F;

    public static MetadataGridResult MetadataGridView(this Ui ui, string id, MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t)
    {
        var theme = ui.Theme;
        var listId = ui.GetId(id);
        state.Show(grid);
        var columns = state.Columns ??= Measure(ui, grid);
        var rowHeight = state.RowHeight = theme.RowHeight;
        var result = default(MetadataGridResult);
        UpdateView(grid, state, flags);

        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = theme.EditorBackground });

        var headers = grid.Columns.Select(static c => c.Name).ToArray();
        var sortIcon = new UiIcon(state.Descending ? LucideIcons.ChevronDown : LucideIcons.ChevronUp, LucideIcons.Family);
        var clicked = ui.TableHeader($"{id}-header", columns, listId, headers, theme.ShellPaddingX, state.ScrollX, new TableSort(state.SortColumn, sortIcon),
            (u, column, hovered) => Heading(u, grid, state, flags, t, listId, column, hovered));
        if (clicked >= 0 && clicked != state.EditingColumn)
            Sort(state, clicked);
        FlagsFlyout(ui, grid, state, flags, t, listId);
        UpdateView(grid, state, flags);

        var area = ui.Interact(listId);
        if (area.Pressed)
            ui.Focus(listId, takesText: false);
        var focused = ui.HasFocus(listId);
        if (focused)
            result = Keys(ui, grid, state, listId, rowHeight);

        var width = columns.TotalWidth + theme.ShellPaddingX * 2;
        var tall = TallRows(ui, grid, state, rowHeight, width);
        using (var list = ui.VirtualScroll(listId, state.View.Length, rowHeight, ref state.ScrollX, ref state.ScrollY, new Box { Gap = 0f }, tall))
        {
            Arrive(state, list, rowHeight);
            if (state.ScrollToSelected && list.ScrollIntoView(state.IndexOf(state.Selected)))
                state.ScrollToSelected = false;
            for (var index = list.First; index < list.End; index++)
            {
                var row = state.RowAt(index);
                var height = rowHeight;
                foreach (var tallRow in tall)
                {
                    if (tallRow.Index == index)
                        height = tallRow.Height;
                }
                if (height > rowHeight)
                {
                    using (ui.Column(new Box { Width = width, Height = height, Gap = 0f }))
                    {
                        Row(ui, id, grid, state, t, columns, listId, row, width, rowHeight, focused, ref result);
                        Details(ui, grid, state, t, row, height - rowHeight);
                    }
                }
                else
                {
                    Row(ui, id, grid, state, t, columns, listId, row, width, rowHeight, focused, ref result);
                }
            }
        }

        if (Menu(ui, id, grid, state, t) is { } picked)
            result = picked;
        return result;
    }

    private static void Row(Ui ui, string id, MetadataGrid grid, MetadataGridState state, Strings t, TableColumns columns, int listId, int row, float width,
        float rowHeight, bool focused, ref MetadataGridResult result)
    {
        var theme = ui.Theme;
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

        var foldable = grid.DetailsMode == MetadataDetailsMode.Foldable;
        var fold = foldable && grid.Details(row) != null ? ui.Interact(HashCode.Combine(listId, FoldSalt, row)) : default;
        if (fold.Hovered || fold.Id != 0 && cells[0].Hovered)
            ui.SetCursor(MouseCursor.Hand);
        if (fold.Clicked || fold.Id != 0 && cells[0].Pressed && cells[0].ClickCount == 1)
            Toggle(state, row);
        var unfolded = foldable && state.Unfolded.Contains(row);
        var selected = row == state.Selected;
        var background = selected ? focused ? theme.Selection : theme.SelectionInactive : hoveredRow || fold.Hovered ? theme.RowHover : unfolded ? theme.Surface : 0u;
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
                var mono = column.IsNumeric || column.IsToken;
                using (ui.TableCell(columns, c, cells[c].Id))
                {
                    if (foldable && c == 0)
                    {
                        using (ui.Box(fold.Id, new Box { Width = theme.IconSize, Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
                        {
                            if (fold.Id != 0)
                                ui.Text(unfolded ? LucideIcons.ChevronDown : LucideIcons.ChevronRight, color: fold.Hovered ? theme.Text : theme.TextMuted,
                                    size: theme.FontSizeSmall, family: LucideIcons.Family);
                        }
                    }
                    var (firstLine, moreLines) = grid.Lines(row, c);
                    ui.Text(firstLine, new Box { Width = Sizing.Fit() }, color: color, size: mono ? theme.FontSize : null,
                        family: mono ? MonoFonts.Regular : null, overflow: TextOverflow.Ellipsis);
                    if (moreLines > 0)
                    {
                        using (ui.Id(cells[c].Id))
                            ui.Text(ui.Memo("more-lines", (moreLines, t), static a => a.t.MetadataMoreLines(a.moreLines)), new Box { Width = Sizing.Fit() },
                                color: selected && focused ? theme.SelectionText : theme.TextMuted, size: theme.FontSizeSmall);
                    }
                }
                Tooltip(ui, grid, row, c, cells[c].Id);
            }
        }
    }

    private static void Toggle(MetadataGridState state, int row)
    {
        if (!state.Unfolded.Remove(row))
            state.Unfolded.Add(row);
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
            var width = ui.MeasureText(column.Name, theme.FontSizeSmall) + theme.IconSize * 2 + theme.GapTight * 2;
            for (var row = 0; row < rows; row++)
                width = MathF.Max(width, ui.MeasureText(grid.Text(row, c), mono ? theme.FontSize : null, family: mono ? MonoFonts.Regular : null));
            if (c == 0 && grid.DetailsMode == MetadataDetailsMode.Foldable)
                width += theme.IconSize + theme.GapTight;
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
        var funnel = ui.Interact(HashCode.Combine(listId, FunnelSalt, column));
        if (funnel.Hovered)
            ui.SetCursor(MouseCursor.Hand);
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
        using var flyout = ui.Flyout(HashCode.Combine(listId, FlyoutSalt), ref state.FlagsOpen, HashCode.Combine(listId, FunnelSalt, state.FlagsColumn),
            new Box { Width = Sizing.Fit(320f, FlyoutContentWidth + theme.TooltipPaddingX * 2 + theme.ScrollBarHitWidth), Gap = theme.Gap });
        if (!flyout.IsOpen) return;

        using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            ui.Text(t.MetadataFilterHint, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
            if (ui.Button(t.MetadataFilterClear, id: "clear"))
                filter.Clear();
        }
        ui.Text(FilterStatePresenter.Describe(filter), color: theme.AccentText, size: theme.FontSizeSmall, wrap: true);
        var shift = (ui.Input.Modifiers & KeyModifiers.Shift) != 0;
        foreach (var group in filter.Schema.MutexGroups)
        {
            using (ui.Id(group.Name))
            using (ui.Column(new Box { Gap = theme.GapTight }))
            {
                ui.Text(group.Name, color: theme.Text, size: theme.FontSizeSmall, bold: true);
                var selection = filter.MutexSelections.GetValueOrDefault(group.Name);
                var chips = new List<(string Label, uint? Value)>(group.Values.Count + 1) { (t.MetadataFilterAnyValue, null) };
                chips.AddRange(group.Values.Select(static v => (v.Label, (uint?)v.Value)));
                var first = 0;
                while (first < chips.Count)
                {
                    var end = first + 1;
                    var used = ChipWidth(ui, chips[first].Label);
                    while (end < chips.Count && used + theme.GapTight + ChipWidth(ui, chips[end].Label) <= FlyoutContentWidth)
                        used += theme.GapTight + ChipWidth(ui, chips[end++].Label);
                    using (ui.Row(new Box { Gap = theme.GapTight }))
                    {
                        for (var i = first; i < end; i++)
                        {
                            var (label, value) = chips[i];
                            var on = value is { } v ? selection == null || selection.Contains(v) : selection == null;
                            if (Chip(ui, label, on, i))
                                filter.SetMutexSelection(group.Name, Pick(group, selection, value, shift));
                        }
                    }
                    first = end;
                }
            }
        }
        if (filter.Schema.IndependentFlags.Count > 0)
        {
            using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
            {
                ui.Text(t.MetadataFilterRequired, color: theme.Text, size: theme.FontSizeSmall, bold: true);
                var mode = filter.IndependentMode;
                if (ui.SegmentedControl("mode", ref mode, [new Segment<MatchMode>(MatchMode.All, t.MetadataFilterAll), new Segment<MatchMode>(MatchMode.Any, t.MetadataFilterAny)]))
                    filter.IndependentMode = mode;
            }
            using (ui.Column(new Box { Gap = 0f }))
            {
                foreach (var flag in filter.Schema.IndependentFlags)
                {
                    using (ui.Id(flag.Name))
                    using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center, Height = theme.RowHeight }))
                    {
                        var current = filter.FlagStates.GetValueOrDefault(flag.Name);
                        if (Pill(ui, current))
                            filter.SetFlagState(flag.Name, current switch
                            {
                                TriState.DontCare => TriState.Required,
                                TriState.Required => TriState.Excluded,
                                _ => TriState.DontCare,
                            });
                        ui.Text(flag.Name, color: current == TriState.DontCare ? theme.TextMuted : theme.Text, size: theme.FontSizeSmall);
                    }
                }
            }
        }
    }

    /// <summary>What a group selects after a click on <paramref name="value"/>, or on its "any" chip
    /// when null: the value alone, or with Shift the selection with it added or taken away; null for
    /// "any", which an empty or full selection also is.</summary>
    private static ImmutableHashSet<uint>? Pick(MutexGroup group, ImmutableHashSet<uint>? selection, uint? value, bool shift)
    {
        if (value is not { } picked) return null;
        if (!shift) return [picked];
        var current = selection ?? group.Values.Select(static v => v.Value).ToImmutableHashSet();
        var next = current.Contains(picked) ? current.Remove(picked) : current.Add(picked);
        return next.IsEmpty || next.Count == group.Values.Count ? null : next;
    }

    private static float ChipWidth(Ui ui, string label) =>
        ui.MeasureText(label, ui.Theme.FontSizeSmall) + ui.Theme.Gap * 2 + ui.Theme.BorderWidth * 2;

    private static bool Chip(Ui ui, string label, bool on, int index)
    {
        var theme = ui.Theme;
        var chip = ui.Interact($"chip-{index}");
        if (chip.Hovered)
            ui.SetCursor(MouseCursor.Hand);
        using (ui.Row(chip.Id, new Box
        {
            Width = Sizing.Fit(),
            Padding = new Spacing(theme.Gap, theme.GapTight),
            Radius = theme.CornerRadius,
            Background = on ? theme.Selection : chip.Hovered ? theme.RowHover : 0u,
            BorderColor = on ? theme.Accent : theme.Border,
            BorderWidth = theme.BorderWidth,
        }))
            ui.Text(label, color: on ? theme.SelectionText : theme.TextMuted, size: theme.FontSizeSmall);
        return chip.Clicked;
    }

    private static bool Pill(Ui ui, TriState state)
    {
        var theme = ui.Theme;
        var pill = ui.Interact("pill");
        if (pill.Hovered)
            ui.SetCursor(MouseCursor.Hand);
        using (ui.Box(pill.Id, new Box
        {
            Width = theme.IconButtonSize,
            Height = theme.IconSize + theme.GapTight * 2,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Radius = theme.CornerRadius,
            Background = pill.Hovered ? theme.RowHover : 0u,
            BorderColor = theme.Border,
            BorderWidth = theme.BorderWidth,
        }))
        {
            if (state != TriState.DontCare)
                ui.Text(state == TriState.Required ? LucideIcons.Check : LucideIcons.X, color: theme.AccentText, size: theme.FontSizeSmall,
                    family: LucideIcons.Family);
        }
        return pill.Clicked;
    }

    /// <summary>The rows in view order that show details under them, and how tall they are with
    /// them: the rows folded out, or only the selected one, as the grid says.</summary>
    private static List<TallRow> TallRows(Ui ui, MetadataGrid grid, MetadataGridState state, float rowHeight, float width)
    {
        var tall = new List<TallRow>();
        switch (grid.DetailsMode)
        {
            case MetadataDetailsMode.Foldable:
                for (var index = 0; index < state.View.Length; index++)
                {
                    if (state.Unfolded.Contains(state.View[index]) && grid.Details(state.View[index]) is { } details)
                        tall.Add(new TallRow(index, rowHeight + DetailsHeight(ui, grid, state, state.View[index], details, width)));
                }
                break;
            case MetadataDetailsMode.WhenSelected when state.Selected >= 0 && state.IndexOf(state.Selected) is >= 0 and var selected
                                                        && grid.Details(state.Selected) is { } details:
                tall.Add(new TallRow(selected, rowHeight + DetailsHeight(ui, grid, state, state.Selected, details, width)));
                break;
        }
        return tall;
    }

    private static float DetailsHeight(Ui ui, MetadataGrid grid, MetadataGridState state, int row, MetadataRowDetails details, float width)
    {
        var theme = ui.Theme;
        var padding = theme.GapTight * 2;
        return details switch
        {
            MetadataBitDetails bits => bits.Bits.Count * theme.RowHeight + padding,
            MetadataTextDetails text => TextHeight(ui, grid, state, row, text.Text, width),
            MetadataTableDetails table => MathF.Min(MaxTableDetails, (table.Rows.Count + 1) * theme.RowHeight + padding),
            _ => 0f,
        };
    }

    /// <summary>How tall a text under <paramref name="row"/> is: a code line per line, up to
    /// <see cref="MaxTextDetailLines"/> before it scrolls, plus the horizontal scrollbar's room when its
    /// longest line is wider than the space the row leaves it.</summary>
    private static float TextHeight(Ui ui, MetadataGrid grid, MetadataGridState state, int row, string text, float width)
    {
        var theme = ui.Theme;
        if (!state.DetailSizes.TryGetValue(row, out var size))
        {
            var lines = text.Split('\n');
            state.DetailSizes[row] = size = (lines.Length, lines.Max(static l => l.TrimEnd('\r').Length));
        }
        var cell = ui.MeasureCell(theme.MonoFontSize, family: MonoFonts.Regular);
        var lineHeight = MathF.Max(theme.CodeLineHeight, MathF.Ceiling(cell.Height));
        var (left, top, right, bottom) = DetailsPadding(theme, grid);
        var overflows = TextDetailPaddingX + size.Longest * cell.Advance > width - left - right;
        var bar = overflows ? theme.ScrollBarWidth + theme.ScrollBarInset * 2 : 0f;
        return Math.Min(size.Lines, MaxTextDetailLines) * lineHeight + top + bottom + bar;
    }

    /// <summary>The room around a row's details: past the guide line of a fold-out, or indented under
    /// the selected row.</summary>
    private static (float Left, float Top, float Right, float Bottom) DetailsPadding(UiTheme theme, MetadataGrid grid) =>
        grid.DetailsMode == MetadataDetailsMode.Foldable
            ? (MathF.Floor(theme.ShellPaddingX + theme.GapTight + theme.IconSize / 2) + theme.SeparatorWidth + theme.IconSize / 2, 0f, theme.ShellPaddingX,
                theme.GapTight)
            : (theme.ShellPaddingX + theme.IconSize, theme.GapTight, theme.ShellPaddingX, theme.GapTight);

    private static void Details(Ui ui, MetadataGrid grid, MetadataGridState state, Strings t, int row, float height)
    {
        var theme = ui.Theme;
        var details = grid.Details(row);
        var guide = theme.ShellPaddingX + theme.GapTight + theme.IconSize / 2;
        using var frame = ui.Row(new Box { Width = Sizing.Grow(), Height = height, Gap = 0f, Background = theme.Surface });
        if (grid.DetailsMode == MetadataDetailsMode.Foldable)
        {
            ui.Leaf(new Box { Width = MathF.Floor(guide), Height = Sizing.Grow() });
            using (ui.Column(new Box { Width = theme.SeparatorWidth, Height = Sizing.Grow(), Padding = new Spacing(0f, 0f, 0f, theme.GapTight) }))
                ui.Leaf(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Background = theme.Separator });
        }
        using var box = ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Grow(),
            Gap = 0f,
            Padding = grid.DetailsMode == MetadataDetailsMode.Foldable
                ? new Spacing(theme.IconSize / 2, 0f, theme.ShellPaddingX, theme.GapTight)
                : new Spacing(theme.ShellPaddingX + theme.IconSize, theme.GapTight, theme.ShellPaddingX, theme.GapTight),
        });
        using var scope = ui.Id(row);
        switch (details)
        {
            case MetadataBitDetails bits:
                foreach (var bit in bits.Bits)
                {
                    using (ui.Row(new Box { Height = theme.RowHeight, Gap = theme.GapTight, AlignY = Align.Center }))
                    {
                        ui.Text(bit.IsSet ? LucideIcons.CheckSquare : LucideIcons.Square, color: bit.IsSet ? theme.AccentText : theme.TextMuted,
                            family: LucideIcons.Family);
                        ui.Text(bit.Name, color: bit.IsSet ? theme.Text : theme.TextMuted);
                    }
                }
                break;
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
            state.Selected = restore.Caret.Line;
            state.CurrentColumn = restore.Caret.Column;
        }
        else if (state.PendingReveal is >= 0 and var row)
        {
            state.PendingReveal = MetadataGridState.None;
            state.Selected = row;
            var index = state.IndexOf(row);
            if (index < 0) return;
            var top = list.TopOf(index);
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
        var count = state.View.Length;
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
                case (0, KeyboardKey.Space) when grid.DetailsMode == MetadataDetailsMode.Foldable && state.Selected >= 0 && grid.Details(state.Selected) != null:
                    Toggle(state, state.Selected);
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

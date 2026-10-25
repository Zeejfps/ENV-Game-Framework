using System.Collections.Immutable;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The content of a flags column's filter flyout: a chip per value of each mutually exclusive
/// group, and a pill per independent flag.</summary>
internal static class FlagsFilterView
{
    public const float ContentWidth = 440f;

    private readonly record struct FilterChip(string Label, uint? Value);

    public static void Render(Ui<UiState> ui, FilterState filter)
    {
        Hint(ui, filter);
        Describe(ui, filter);
        var shift = IsShiftHeld(ui);
        foreach (var group in filter.Schema.MutexGroups)
            MutexGroup(ui, filter, group, shift);
        if (filter.Schema.IndependentFlags.Count > 0)
            IndependentFlags(ui, filter);
    }

    private static void Hint(Ui<UiState> ui, FilterState filter)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            ui.Text(t.MetadataFilterHint, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
            if (ui.Button(t.MetadataFilterClear, id: "clear"))
                filter.Clear();
        }
    }

    private static void Describe(Ui<UiState> ui, FilterState filter)
    {
        var theme = ui.Theme;
        var description = FilterStatePresenter.Describe(filter);
        ui.Text(description, color: theme.AccentText, size: theme.FontSizeSmall, wrap: true);
    }

    private static bool IsShiftHeld(Ui<UiState> ui) => (ui.Input.Modifiers & KeyModifiers.Shift) != 0;

    private static void MutexGroup(Ui<UiState> ui, FilterState filter, MutexGroup group, bool shift)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(group.Name))
        using (ui.Column(new Box { Gap = theme.GapTight }))
        {
            ui.Text(group.Name, color: theme.Text, size: theme.FontSizeSmall, bold: true);
            var selection = filter.MutexSelections.GetValueOrDefault(group.Name);
            var chips = ListChips(group, t);
            var first = 0;
            while (first < chips.Count)
            {
                var end = FindRowEnd(ui, chips, first);
                ChipRow(ui, filter, group, selection, chips, first, end, shift);
                first = end;
            }
        }
    }

    private static List<FilterChip> ListChips(MutexGroup group, Strings t)
    {
        var chips = new List<FilterChip>(group.Values.Count + 1);
        chips.Add(new FilterChip(t.MetadataFilterAnyValue, null));
        foreach (var value in group.Values)
            chips.Add(new FilterChip(value.Label, value.Value));
        return chips;
    }

    private static int FindRowEnd(Ui<UiState> ui, List<FilterChip> chips, int first)
    {
        var gap = ui.Theme.GapTight;
        var end = first + 1;
        var used = ChipWidth(ui, chips[first].Label);
        while (end < chips.Count)
        {
            var width = ChipWidth(ui, chips[end].Label);
            if (used + gap + width > ContentWidth)
                break;
            used += gap + width;
            end++;
        }
        return end;
    }

    private static float ChipWidth(Ui<UiState> ui, string label) =>
        ui.MeasureText(label, ui.Theme.FontSizeSmall) + ui.Theme.Gap * 2 + ui.Theme.BorderWidth * 2;

    private static void ChipRow(Ui<UiState> ui, FilterState filter, MutexGroup group, ImmutableHashSet<uint>? selection, List<FilterChip> chips, int first,
        int end, bool shift)
    {
        using (ui.Row(new Box { Gap = ui.Theme.GapTight }))
        {
            for (var i = first; i < end; i++)
            {
                var chip = chips[i];
                var on = IsOn(chip, selection);
                if (Chip(ui, chip.Label, on, i))
                    filter.SetMutexSelection(group.Name, Pick(group, selection, chip.Value, shift));
            }
        }
    }

    private static bool IsOn(FilterChip chip, ImmutableHashSet<uint>? selection)
    {
        if (selection == null)
            return true;
        if (chip.Value == null)
            return false;
        return selection.Contains(chip.Value.Value);
    }

    private static bool Chip(Ui<UiState> ui, string label, bool on, int index)
    {
        var theme = ui.Theme;
        var chip = ui.Clickable($"chip-{index}");
        using (ui.Row(chip.Id, new Box
        {
            Width = Sizing.Fit(),
            Padding = new Spacing(theme.Gap, theme.GapTight),
            Radius = theme.CornerRadius,
            Background = ChipBackground(theme, on, chip.Hovered),
            BorderColor = on ? theme.Accent : theme.Border,
            BorderWidth = theme.BorderWidth,
        }))
            ui.Text(label, color: on ? theme.SelectionText : theme.TextMuted, size: theme.FontSizeSmall);
        return chip.Clicked;
    }

    private static uint ChipBackground(UiTheme theme, bool on, bool hovered)
    {
        if (on)
            return theme.Selection;
        if (hovered)
            return theme.RowHover;
        return 0u;
    }

    /// <summary>What a group selects after a click on <paramref name="value"/>, or on its "any" chip
    /// when null: the value alone, or with Shift the selection with it added or taken away; null for
    /// "any", which an empty or full selection also is.</summary>
    private static ImmutableHashSet<uint>? Pick(MutexGroup group, ImmutableHashSet<uint>? selection, uint? value, bool shift)
    {
        if (value == null)
            return null;
        var picked = value.Value;
        if (!shift)
            return [picked];
        var current = selection ?? group.Values.Select(static v => v.Value).ToImmutableHashSet();
        var next = Toggle(current, picked);
        var isAny = next.IsEmpty || next.Count == group.Values.Count;
        if (isAny)
            return null;
        return next;
    }

    private static ImmutableHashSet<uint> Toggle(ImmutableHashSet<uint> values, uint value)
    {
        if (values.Contains(value))
            return values.Remove(value);
        return values.Add(value);
    }

    private static void IndependentFlags(Ui<UiState> ui, FilterState filter)
    {
        var theme = ui.Theme;
        MatchModeRow(ui, filter);
        using (ui.Column(new Box { Gap = 0f }))
        {
            foreach (var flag in filter.Schema.IndependentFlags)
            {
                using (ui.Id(flag.Name))
                using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center, Height = theme.RowHeight }))
                    FlagRow(ui, filter, flag.Name);
            }
        }
    }

    private static void MatchModeRow(Ui<UiState> ui, FilterState filter)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            ui.Text(t.MetadataFilterRequired, color: theme.Text, size: theme.FontSizeSmall, bold: true);
            var mode = filter.IndependentMode;
            ReadOnlySpan<Segment<MatchMode>> modes = [new Segment<MatchMode>(MatchMode.All, t.MetadataFilterAll), new Segment<MatchMode>(MatchMode.Any, t.MetadataFilterAny)];
            if (ui.SegmentedControl("mode", ref mode, modes))
                filter.IndependentMode = mode;
        }
    }

    private static void FlagRow(Ui<UiState> ui, FilterState filter, string flag)
    {
        var theme = ui.Theme;
        var current = filter.FlagStates.GetValueOrDefault(flag);
        if (Pill(ui, current))
            filter.SetFlagState(flag, Next(current));
        var color = current == TriState.DontCare ? theme.TextMuted : theme.Text;
        ui.Text(flag, color: color, size: theme.FontSizeSmall);
    }

    private static TriState Next(TriState state) => state switch
    {
        TriState.DontCare => TriState.Required,
        TriState.Required => TriState.Excluded,
        _ => TriState.DontCare,
    };

    private static bool Pill(Ui<UiState> ui, TriState state)
    {
        var theme = ui.Theme;
        var pill = ui.Clickable("pill");
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
                PillGlyph(ui, state);
        }
        return pill.Clicked;
    }

    private static void PillGlyph(Ui<UiState> ui, TriState state)
    {
        var theme = ui.Theme;
        var icons = ui.State.Icons;
        var glyph = state == TriState.Required ? icons.MetadataFlagRequired : icons.MetadataFlagExcluded;
        ui.Glyph(glyph, theme.AccentText, theme.FontSizeSmall);
    }
}

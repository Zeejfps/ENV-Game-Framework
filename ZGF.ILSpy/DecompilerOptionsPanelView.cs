using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The decompiler's toggles, grouped by the C# version that brought each feature, newest
/// first, then the rest. Every change shows in the open documents at once. Features newer than the
/// picked version are greyed out but keep their state for when a newer version is picked.</summary>
internal static class DecompilerOptionsPanelView
{
    private sealed record Section(string Key, DecompilerToggleGroup Group, CSharpVersion? Version, DecompilerToggle[] Toggles);

    private static readonly Section[] Sections = BuildSections();

    private static readonly CSharpVersion Newest = Sections.Max(static s => s.Version ?? 0);

    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        var options = w.DecompilerOptions;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("collapse", LucideIcons.ChevronsDownUp, LucideIcons.Family, t.CodeCollapseAll, !AllFolded(w)))
                w.FoldedOptionGroups.UnionWith(Sections.Select(static s => s.Key));
            if (ui.IconButton("expand", LucideIcons.ChevronsUpDown, LucideIcons.Family, t.CodeExpandAll, w.FoldedOptionGroups.Count > 0))
                w.FoldedOptionGroups.Clear();
            ui.Separator(theme.IconSize);
            if (ui.IconButton("reset", LucideIcons.RotateCcw, LucideIcons.Family, t.DecompilerOptionsReset, options.Toggles.Count > 0))
                w.ResetDecompilerToggles();
        }
        ui.Separator();
        var tooNew = options.LanguageVersion < Newest ? ui.Memo("too-new", (t, options.LanguageVersion), static a => TooNew(a.t, a.LanguageVersion)) : null;
        if (tooNew != null)
        {
            using (ui.Column(new Box { Padding = new Spacing(theme.ShellPaddingX, theme.ToolbarPaddingY) }))
                ui.Text(tooNew, color: theme.TextMuted, wrap: true);
            ui.Separator();
        }

        var rowHeight = theme.RowHeight;
        string? flipped = null;
        using (var list = ui.VirtualScroll("decompiler-options", RowCount(w), rowHeight))
        {
            for (var i = list.First; i < list.End; i++)
            {
                var (section, toggle) = RowAt(w, i);
                using (ui.Id(i))
                {
                    if (toggle is null)
                    {
                        if (Header(ui, w, t, section, options, rowHeight, tooNew))
                            flipped = section.Key;
                    }
                    else
                    {
                        Toggle(ui, w, t, toggle, options, rowHeight);
                    }
                }
            }
        }
        if (flipped != null && !w.FoldedOptionGroups.Remove(flipped))
            w.FoldedOptionGroups.Add(flipped);
    }

    /// <summary>Draws a section's header: a chevron and a checkbox that sets every toggle in it,
    /// showing a square when only some are on. Returns true when the row besides the checkbox was
    /// clicked to fold or unfold.</summary>
    private static bool Header(Ui ui, Workspace w, Strings t, Section section, DecompilerOptions options, float rowHeight, string? tooNew)
    {
        var theme = ui.Theme;
        var folded = w.FoldedOptionGroups.Contains(section.Key);
        var allowed = IsAllowed(section, options);
        var item = ui.Interact(section.Key);
        using (ui.Row(item.Id, new Box
        {
            Width = Sizing.Grow(),
            Height = rowHeight,
            Gap = theme.GapTight,
            Padding = new Spacing(theme.ShellPaddingX, 0f),
            AlignY = Align.Center,
            Background = item.Hovered ? theme.RowHover : 0u,
        }))
        {
            ui.Icon(new UiIcon(folded ? LucideIcons.ChevronRight : LucideIcons.ChevronDown, LucideIcons.Family, theme.TextMuted));
            var all = State(section, options);
            if (ui.Checkbox(Title(t, section), ref all, id: "all", enabled: allowed, width: Sizing.Grow(), bold: Changed(section, options)))
                w.SetDecompilerToggles(section.Toggles, all == true);
        }
        if (!allowed && tooNew != null)
            ui.Tooltip(item.Id, tooNew);
        return item.Clicked;
    }

    private static bool IsAllowed(Section section, DecompilerOptions options)
    {
        var version = section.Version;
        if (version == null)
            return true;
        return version.Value <= options.LanguageVersion;
    }

    /// <summary>True when every toggle in <paramref name="section"/> is on, false when none is, null otherwise.</summary>
    private static bool? State(Section section, DecompilerOptions options)
    {
        var on = section.Toggles.Count(options.IsOn);
        if (on == section.Toggles.Length)
            return true;
        if (on == 0)
            return false;
        return null;
    }

    /// <summary>Whether any toggle in <paramref name="section"/> is set away from its default.</summary>
    private static bool Changed(Section section, DecompilerOptions options)
    {
        foreach (var toggle in section.Toggles)
        {
            if (options.Toggles.ContainsKey(toggle.Name)) return true;
        }
        return false;
    }

    private static void Toggle(Ui ui, Workspace w, Strings t, DecompilerToggle toggle, DecompilerOptions options, float rowHeight)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box
        {
            Width = Sizing.Grow(),
            Height = rowHeight,
            Padding = new Spacing(theme.ShellPaddingX + theme.IconSize + theme.GapTight + theme.TreeIndent, 0f),
            AlignY = Align.Center,
        }))
        {
            var on = options.IsOn(toggle);
            if (ui.Checkbox(DecompilerOptionLabels.Label(t, toggle.Name) ?? toggle.Name, ref on, id: toggle.Name, enabled: options.Allows(toggle),
                    width: Sizing.Grow(), bold: options.Toggles.ContainsKey(toggle.Name)))
                w.SetDecompilerToggle(toggle, on);
        }
    }

    private static string TooNew(Strings t, CSharpVersion version) => t.DecompilerOptionsTooNew(CSharpVersions.Label(version));

    private static string Title(Strings t, Section section) => section.Group switch
    {
        DecompilerToggleGroup.Language => CSharpVersions.Label(section.Version!.Value),
        DecompilerToggleGroup.FSharp => t.DecompilerOptionsFsharp,
        _ => t.DecompilerOptionsOther,
    };

    private static bool AllFolded(Workspace w)
    {
        foreach (var section in Sections)
        {
            if (!w.FoldedOptionGroups.Contains(section.Key)) return false;
        }
        return true;
    }

    private static int RowCount(Workspace w)
    {
        var count = 0;
        foreach (var section in Sections)
            count += 1 + (w.FoldedOptionGroups.Contains(section.Key) ? 0 : section.Toggles.Length);
        return count;
    }

    /// <summary>The section a row is in, and the toggle it shows, or null for the section's header.</summary>
    private static (Section Section, DecompilerToggle? Toggle) RowAt(Workspace w, int row)
    {
        foreach (var section in Sections)
        {
            if (row == 0) return (section, null);
            var shown = w.FoldedOptionGroups.Contains(section.Key) ? 0 : section.Toggles.Length;
            if (row <= shown) return (section, section.Toggles[row - 1]);
            row -= 1 + shown;
        }
        throw new ArgumentOutOfRangeException(nameof(row));
    }

    private static Section[] BuildSections()
    {
        var all = DecompilerToggle.All;
        var language = all.Where(static t => t.Group == DecompilerToggleGroup.Language)
            .GroupBy(static t => t.IntroducedIn!.Value)
            .OrderByDescending(static g => g.Key)
            .Select(static g => new Section(g.Key.ToString(), DecompilerToggleGroup.Language, g.Key, g.ToArray()));
        var rest = new[] { DecompilerToggleGroup.Other, DecompilerToggleGroup.FSharp }
            .Select(group => new Section(group.ToString(), group, null, all.Where(t => t.Group == group).ToArray()))
            .Where(static s => s.Toggles.Length > 0);
        return language.Concat(rest).ToArray();
    }
}

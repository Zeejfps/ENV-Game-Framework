using ZGF.Gui;

namespace GitGui;

/// <summary>
/// Semantic design tokens grouped by area. One field per current per-area palette entry,
/// strongly-typed. Cross-area derivations live on sub-records that hold a back-reference
/// to the owning <see cref="ThemeTokens"/> — wired by <see cref="Create"/>.
/// </summary>
public sealed record ThemeTokens
{
    public SurfacesTokens Surfaces { get; }
    public TextTokens Text { get; }
    public DialogTokens Dialog { get; }
    public CommitsTokens Commits { get; }
    public DiffTokens Diff { get; }
    public FileChangesTokens FileChanges { get; }
    public CommitDetailsTokens CommitDetails { get; }
    public TooltipTokens Tooltip { get; }

    private ThemeTokens(
        SurfacesTokens surfaces,
        TextTokens text,
        DialogTokens dialog,
        CommitsTokens commits,
        DiffTokens diff,
        FileChangesTokens fileChanges,
        CommitDetailsTokens commitDetails,
        TooltipTokens tooltip)
    {
        Surfaces = surfaces;
        Text = text;
        Dialog = dialog;
        Commits = commits;
        Diff = diff;
        FileChanges = fileChanges;
        CommitDetails = commitDetails;
        Tooltip = tooltip;
    }

    public static ThemeTokens Create(
        SurfacesTokens surfaces,
        TextTokens text,
        DialogTokens dialog,
        CommitsTokens commits,
        DiffTokens diff,
        FileChangesTokens fileChanges,
        CommitDetailsTokens commitDetails,
        TooltipTokens tooltip)
    {
        var theme = new ThemeTokens(surfaces, text, dialog, commits, diff, fileChanges, commitDetails, tooltip);
        diff.Wire(theme);
        fileChanges.Wire(theme);
        return theme;
    }
}

public sealed record SurfacesTokens
{
    public required uint BgPanel { get; init; }
    public required uint BgHeader { get; init; }
    public required uint BgDeep { get; init; }
    public required uint Border { get; init; }
}

public sealed record TextTokens
{
    public required uint Strong { get; init; }
    public required uint Primary { get; init; }
    public required uint Row { get; init; }
    public required uint Dim { get; init; }
    public required uint Header { get; init; }
}

public sealed record DialogTokens
{
    public required uint Background { get; init; }
    public required uint Border { get; init; }
    public required uint Separator { get; init; }
    public required uint TitleText { get; init; }
    public required uint BodyText { get; init; }

    public required uint ButtonNormal { get; init; }
    public required uint ButtonHover { get; init; }
    public required uint ButtonBorder { get; init; }
    public required uint ButtonBorderHover { get; init; }

    public required uint CloseNormal { get; init; }
    public required uint CloseHover { get; init; }
    public required uint CloseTextNormal { get; init; }
    public required uint CloseTextHover { get; init; }

    public required uint RowTransparent { get; init; }
    public required uint RowHover { get; init; }
    public required uint RowActive { get; init; }
    public required uint RowText { get; init; }
    public required uint RowTextActive { get; init; }
    public required uint RowTextMissing { get; init; }
    public required uint SectionHeaderText { get; init; }

    public required uint IconAccentWorktree { get; init; }
    public required uint IconAccentSubmodule { get; init; }
}

public sealed record CommitsTokens
{
    public required uint Background { get; init; }
    public required uint Border { get; init; }
    public required uint HeaderBg { get; init; }
    public required uint HeaderText { get; init; }
    public required uint RowText { get; init; }
    public required uint RowTextDim { get; init; }
    public required uint RowHighlight { get; init; }
    public required uint RowTextActive { get; init; }
    public required uint Placeholder { get; init; }

    public required uint ScrollTrackBg { get; init; }
    public required uint ScrollTrackBorder { get; init; }
    public required uint ScrollThumbBg { get; init; }
    public required uint ScrollThumbHoverBg { get; init; }
    public required uint ScrollThumbBorder { get; init; }

    public required uint DividerHoverBg { get; init; }
    public required uint DividerHoverLine { get; init; }

    public required uint WarningBg { get; init; }
    public required uint WarningBorder { get; init; }
    public required uint WarningText { get; init; }

    public required uint BadgeLocalBg { get; init; }
    public required uint BadgeRemoteBg { get; init; }
    public required uint BadgeHeadBg { get; init; }
    public required uint BadgeText { get; init; }

    public required uint AheadColor { get; init; }
    public required uint BehindColor { get; init; }

    public uint PreviewCleanColor => AheadColor;
    public uint PreviewConflictColor => BehindColor;

    public required IReadOnlyList<uint> LanePalette { get; init; }

    public uint LaneColor(int lane) =>
        LanePalette[((lane % LanePalette.Count) + LanePalette.Count) % LanePalette.Count];
}

// Plain sealed class (not record). The private _theme back-ref makes the synthesized
// record Equals/PrintMembers walk into the owning ThemeTokens — which contains this
// instance — and StackOverflow on ToString. Plain class equality is reference-equality,
// which matches our intent: tokens are identity-scoped to their parent ThemeTokens.
public sealed class DiffTokens
{
    private ThemeTokens? _theme;
    internal void Wire(ThemeTokens theme) => _theme = theme;

    public required uint LineNumberText { get; init; }

    // Blended derivations — re-computed from sibling token groups so theme changes
    // automatically refresh them.
    public uint LineAddedBg => ColorMath.Blend(
        _theme!.FileChanges.StatusAdded, _theme.Commits.Background, 0.18f);
    public uint LineRemovedBg => ColorMath.Blend(
        _theme!.FileChanges.StatusDeleted, _theme.Commits.Background, 0.18f);

    public uint LineAddedGlyphText => _theme!.FileChanges.StatusAdded;
    public uint LineRemovedGlyphText => _theme!.FileChanges.StatusDeleted;
    public uint LineContextGlyphText => _theme!.FileChanges.HeaderText;

    public uint HunkSeparatorBg => _theme!.FileChanges.HeaderBg;
    public uint HunkSeparatorRangeText => _theme!.FileChanges.HeaderText;
    public uint HunkSeparatorContextText => _theme!.CommitDetails.Secondary;

    public uint BannerBg => HunkSeparatorBg;
    public uint BannerText => HunkSeparatorContextText;

    public uint LineText => _theme!.CommitDetails.Primary;

    public uint TruncatedFooterBg => HunkSeparatorBg;
    public uint TruncatedFooterText => HunkSeparatorRangeText;
}

// See DiffTokens: plain sealed class so the _theme back-ref doesn't participate in
// synthesized record Equals / PrintMembers (which would StackOverflow on ToString).
public sealed class FileChangesTokens
{
    private ThemeTokens? _theme;
    internal void Wire(ThemeTokens theme) => _theme = theme;

    public required uint StatusAdded { get; init; }
    public required uint StatusModified { get; init; }
    public required uint StatusDeleted { get; init; }
    public required uint StatusRenamed { get; init; }
    public required uint StatusConflicted { get; init; }
    public required uint StatusSubmodule { get; init; }
    public required uint StatusOther { get; init; }

    public uint BadgeText => _theme!.Surfaces.BgDeep;
    public uint RowText => _theme!.Text.Row;

    public required uint HeaderBg { get; init; }
    public uint HeaderBorder => _theme!.Surfaces.Border;
    public uint HeaderText => _theme!.Text.Header;

    public uint StatusColor(FileChangeStatus status) => status switch
    {
        FileChangeStatus.Added => StatusAdded,
        FileChangeStatus.Modified => StatusModified,
        FileChangeStatus.Deleted => StatusDeleted,
        FileChangeStatus.Renamed => StatusRenamed,
        FileChangeStatus.Conflicted => StatusConflicted,
        FileChangeStatus.Submodule => StatusSubmodule,
        _ => StatusOther,
    };
}

public sealed record CommitDetailsTokens
{
    public required uint Background { get; init; }
    public required uint Border { get; init; }
    public required uint Primary { get; init; }
    public required uint Secondary { get; init; }
    public required uint Muted { get; init; }
    public required uint Placeholder { get; init; }

    public required IReadOnlyList<uint> AvatarPalette { get; init; }

    public uint AvatarColor(string seed)
    {
        if (string.IsNullOrEmpty(seed)) return AvatarPalette[0];
        var h = 0;
        foreach (var ch in seed) h = unchecked(h * 31 + char.ToLowerInvariant(ch));
        var idx = ((h % AvatarPalette.Count) + AvatarPalette.Count) % AvatarPalette.Count;
        return AvatarPalette[idx];
    }
}

public sealed record TooltipTokens
{
    public required uint Background { get; init; }
    public required uint Border { get; init; }
    public required uint Text { get; init; }
    public required uint ShadowColor { get; init; }
}

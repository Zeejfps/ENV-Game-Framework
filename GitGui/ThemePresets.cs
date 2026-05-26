namespace GitGui;

/// <summary>
/// Built-in theme presets. <see cref="Dark"/> mirrors the values from the legacy
/// <c>Theme.cs</c> + every per-area palette one-to-one — visual diff against pre-Phase-2
/// must be zero. <see cref="Light"/> is a minimal swap-test theme (a few flipped scalars)
/// used to prove runtime swap works; not a real production light theme.
/// </summary>
public static class ThemePresets
{
    private static readonly IReadOnlyList<uint> DarkLanePalette = new uint[]
    {
        0xFF5865F2,
        0xFFEB459E,
        0xFF57F287,
        0xFFFEE75C,
        0xFFED4245,
        0xFF9B59B6,
        0xFFE67E22,
        0xFF1ABC9C,
    };

    private static readonly IReadOnlyList<uint> DarkAvatarPalette = new uint[]
    {
        0xFF5865F2,
        0xFFEB459E,
        0xFF57F287,
        0xFFFEE75C,
        0xFFED4245,
        0xFF9B59B6,
        0xFFE67E22,
        0xFF1ABC9C,
    };

    public static ThemeTokens Dark { get; } = ThemeTokens.Create(
        surfaces: new SurfacesTokens
        {
            BgPanel = 0xFF1E1F22,
            BgHeader = 0xFF2A2C30,
            BgDeep = 0xFF1A1B1E,
            Border = 0xFF313338,
        },
        text: new TextTokens
        {
            Strong = 0xFFFFFFFF,
            Primary = 0xFFE6E6E6,
            Row = 0xFFB5B9C0,
            Dim = 0xFF7A7C81,
            Header = 0xFF96989D,
        },
        dialog: new DialogTokens
        {
            Background = 0xFF1E1F22,
            Border = 0xFF313338,
            Separator = 0xFF2A2C30,
            TitleText = 0xFFE6E6E6,
            BodyText = 0xFFDCDDDE,

            ButtonNormal = 0xFF2B2D31,
            ButtonHover = 0xFF3A3D43,
            ButtonBorder = 0xFF3E4047,
            ButtonBorderHover = 0xFF5865F2,

            CloseNormal = 0x00000000,
            CloseHover = 0xFF3A3D43,
            CloseTextNormal = 0xFFB5B9C0,
            CloseTextHover = 0xFFFFFFFF,

            RowTransparent = 0x00000000,
            RowHover = 0xFF2B2D31,
            RowActive = 0xFF404C8C,
            RowText = 0xFFB5B9C0,
            RowTextActive = 0xFFFFFFFF,
            RowTextMissing = 0x80B5B9C0,
            SectionHeaderText = 0xFF96989D,

            IconAccentWorktree = 0xFF5DADE2,
            IconAccentSubmodule = 0xFFB57EDC,
        },
        commits: new CommitsTokens
        {
            Background = 0xFF1E1F22,
            Border = 0xFF313338,
            HeaderBg = 0xFF2A2C30,
            HeaderText = 0xFF96989D,
            RowText = 0xFFB5B9C0,
            RowTextDim = 0xFF7A7C81,
            RowHighlight = 0xFF404C8C,
            RowTextActive = 0xFFFFFFFF,
            Placeholder = 0xFF96989D,

            ScrollTrackBg = 0xFF26272B,
            ScrollTrackBorder = 0xFF313338,
            ScrollThumbBg = 0xFF4A4D52,
            ScrollThumbHoverBg = 0xFF6A6D72,
            ScrollThumbBorder = 0xFF2A2C30,

            DividerHoverBg = 0xFF4A5680,
            DividerHoverLine = 0xFF7A8DC8,

            WarningBg = 0xFF3D2E14,
            WarningBorder = 0xFFB89050,
            WarningText = 0xFFE9C77A,

            BadgeLocalBg = 0xFF2F4A6B,
            BadgeRemoteBg = 0xFF4A2F6B,
            BadgeHeadBg = 0xFF6B4A2F,
            BadgeText = 0xFFE6E6E6,

            AheadColor = 0xFF9DD17B,
            BehindColor = 0xFFE6A85C,

            LanePalette = DarkLanePalette,
        },
        diff: new DiffTokens
        {
            LineNumberText = 0xFF7A7C81,
        },
        fileChanges: new FileChangesTokens
        {
            StatusAdded = 0xFF57F287,
            StatusModified = 0xFFE9C77A,
            StatusDeleted = 0xFFED4245,
            StatusRenamed = 0xFF5DADE2,
            StatusConflicted = 0xFFED4245,
            StatusSubmodule = 0xFFB57EDC,
            StatusOther = 0xFF9B59B6,
            HeaderBg = 0xFF222326,
        },
        commitDetails: new CommitDetailsTokens
        {
            Background = 0xFF1A1B1E,
            Border = 0xFF313338,
            Primary = 0xFFE6E6E6,
            Secondary = 0xFFB5B9C0,
            Muted = 0xFF7A7C81,
            Placeholder = 0xFF96989D,
            AvatarPalette = DarkAvatarPalette,
        },
        tooltip: new TooltipTokens
        {
            Background = 0xFF2A2C30,
            Border = 0xFF313338,
            Text = 0xFFE6E6E6,
            ShadowColor = 0x80000000,
        });

    /// <summary>
    /// Minimal alternative theme — flips a few surface and dialog scalars so the runtime
    /// swap test produces visible change without committing to a real light palette. Not
    /// production-ready; a proper light theme is out of scope for Phase 2.
    /// </summary>
    public static ThemeTokens Light { get; } = ThemeTokens.Create(
        surfaces: new SurfacesTokens
        {
            BgPanel = 0xFFF4F4F5,
            BgHeader = 0xFFE4E4E7,
            BgDeep = 0xFFFFFFFF,
            Border = 0xFFD4D4D8,
        },
        text: new TextTokens
        {
            Strong = 0xFF000000,
            Primary = 0xFF18181B,
            Row = 0xFF3F3F46,
            Dim = 0xFF71717A,
            Header = 0xFF52525B,
        },
        dialog: new DialogTokens
        {
            Background = 0xFFF4F4F5,
            Border = 0xFFD4D4D8,
            Separator = 0xFFE4E4E7,
            TitleText = 0xFF18181B,
            BodyText = 0xFF27272A,

            ButtonNormal = 0xFFE4E4E7,
            ButtonHover = 0xFFD4D4D8,
            ButtonBorder = 0xFFA1A1AA,
            ButtonBorderHover = 0xFF5865F2,

            CloseNormal = 0x00000000,
            CloseHover = 0xFFD4D4D8,
            CloseTextNormal = 0xFF3F3F46,
            CloseTextHover = 0xFF000000,

            RowTransparent = 0x00000000,
            RowHover = 0xFFE4E4E7,
            RowActive = 0xFFC7D2FE,
            RowText = 0xFF3F3F46,
            RowTextActive = 0xFF18181B,
            RowTextMissing = 0x803F3F46,
            SectionHeaderText = 0xFF52525B,

            IconAccentWorktree = 0xFF2563EB,
            IconAccentSubmodule = 0xFF9333EA,
        },
        commits: new CommitsTokens
        {
            Background = 0xFFF4F4F5,
            Border = 0xFFD4D4D8,
            HeaderBg = 0xFFE4E4E7,
            HeaderText = 0xFF52525B,
            RowText = 0xFF3F3F46,
            RowTextDim = 0xFF71717A,
            RowHighlight = 0xFFC7D2FE,
            RowTextActive = 0xFF18181B,
            Placeholder = 0xFF52525B,

            ScrollTrackBg = 0xFFE4E4E7,
            ScrollTrackBorder = 0xFFD4D4D8,
            ScrollThumbBg = 0xFFA1A1AA,
            ScrollThumbHoverBg = 0xFF71717A,
            ScrollThumbBorder = 0xFFD4D4D8,

            DividerHoverBg = 0xFFC7D2FE,
            DividerHoverLine = 0xFF6366F1,

            WarningBg = 0xFFFEF3C7,
            WarningBorder = 0xFFD97706,
            WarningText = 0xFF92400E,

            BadgeLocalBg = 0xFFDBEAFE,
            BadgeRemoteBg = 0xFFEDE9FE,
            BadgeHeadBg = 0xFFFED7AA,
            BadgeText = 0xFF18181B,

            AheadColor = 0xFF16A34A,
            BehindColor = 0xFFEA580C,

            LanePalette = DarkLanePalette,
        },
        diff: new DiffTokens
        {
            LineNumberText = 0xFF71717A,
        },
        fileChanges: new FileChangesTokens
        {
            StatusAdded = 0xFF16A34A,
            StatusModified = 0xFFD97706,
            StatusDeleted = 0xFFDC2626,
            StatusRenamed = 0xFF2563EB,
            StatusConflicted = 0xFFDC2626,
            StatusSubmodule = 0xFF9333EA,
            StatusOther = 0xFF9333EA,
            HeaderBg = 0xFFE4E4E7,
        },
        commitDetails: new CommitDetailsTokens
        {
            Background = 0xFFFFFFFF,
            Border = 0xFFD4D4D8,
            Primary = 0xFF18181B,
            Secondary = 0xFF3F3F46,
            Muted = 0xFF71717A,
            Placeholder = 0xFF52525B,
            AvatarPalette = DarkAvatarPalette,
        },
        tooltip: new TooltipTokens
        {
            Background = 0xFFE4E4E7,
            Border = 0xFFD4D4D8,
            Text = 0xFF18181B,
            ShadowColor = 0x40000000,
        });
}

namespace GitGui;

/// <summary>
/// Legacy compile-time consts kept alive while non-Commits-cluster consumers (BranchesView,
/// ActionsToolbar, ScrollBarStyles, CommitBarView, ...) still reference them. Values mirror
/// <see cref="ThemePresets"/>.Dark.Commits + the lane palette. Each consumer cluster migrates
/// to <see cref="IThemeService"/> on its own pass; this file goes away once those are done.
/// </summary>
internal static class CommitsPalette
{
    public const uint Background = Theme.BgPanel;
    public const uint Border = Theme.Border;
    public const uint HeaderBg = Theme.BgHeader;
    public const uint HeaderText = Theme.TextHeader;
    public const uint RowText = Theme.TextRow;
    public const uint RowTextDim = Theme.TextDim;
    public const uint RowHighlight = 0xFF404C8C;
    public const uint RowTextActive = Theme.TextStrong;
    public const uint Placeholder = Theme.TextHeader;

    public const uint ScrollTrackBg = 0xFF26272B;
    public const uint ScrollTrackBorder = 0xFF313338;
    public const uint ScrollThumbBg = 0xFF4A4D52;
    public const uint ScrollThumbHoverBg = 0xFF6A6D72;
    public const uint ScrollThumbBorder = 0xFF2A2C30;

    public const uint DividerHoverBg = 0xFF4A5680;
    public const uint DividerHoverLine = 0xFF7A8DC8;

    public const uint WarningBg = 0xFF3D2E14;
    public const uint WarningBorder = 0xFFB89050;
    public const uint WarningText = 0xFFE9C77A;

    public const uint BadgeLocalBg = 0xFF2F4A6B;
    public const uint BadgeRemoteBg = 0xFF4A2F6B;
    public const uint BadgeHeadBg = 0xFF6B4A2F;
    public const uint BadgeText = 0xFFE6E6E6;

    public const uint AheadColor = 0xFF9DD17B;
    public const uint BehindColor = 0xFFE6A85C;

    public const uint PreviewCleanColor = AheadColor;
    public const uint PreviewConflictColor = BehindColor;

    public static readonly uint[] LanePalette =
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

    public static uint LaneColor(int lane) =>
        LanePalette[((lane % LanePalette.Length) + LanePalette.Length) % LanePalette.Length];
}

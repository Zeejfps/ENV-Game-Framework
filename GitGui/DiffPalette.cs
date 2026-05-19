namespace GitGui;

internal static class DiffPalette
{
    // ~18% alpha blend of FileChangesPalette.StatusAdded over CommitsPalette.Background.
    public const uint LineAddedBg = 0xFF284534;
    // ~18% alpha blend of FileChangesPalette.StatusDeleted over CommitsPalette.Background.
    public const uint LineRemovedBg = 0xFF432528;

    public const uint LineAddedGlyphText = FileChangesPalette.StatusAdded;
    public const uint LineRemovedGlyphText = FileChangesPalette.StatusDeleted;
    public const uint LineContextGlyphText = FileChangesPalette.HeaderText;
    public const uint LineNumberText = 0xFF7A7C81;

    public const uint HunkSeparatorBg = FileChangesPalette.HeaderBg;
    public const uint HunkSeparatorRangeText = FileChangesPalette.HeaderText;
    public const uint HunkSeparatorContextText = CommitDetailsPalette.Secondary;

    public const uint BannerBg = HunkSeparatorBg;
    public const uint BannerText = HunkSeparatorContextText;

    public const uint LineText = CommitDetailsPalette.Primary;

    public const uint TruncatedFooterBg = HunkSeparatorBg;
    public const uint TruncatedFooterText = HunkSeparatorRangeText;
}
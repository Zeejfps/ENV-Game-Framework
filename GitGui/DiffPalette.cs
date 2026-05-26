namespace GitGui;

// Legacy compile-time consts used by DiffContentView / DiffView until the Diff cluster sweep
// migrates them to the runtime ThemeTokens. Values mirror ThemePresets.Dark.Diff and the
// FileChanges / Commits / CommitDetails fields it derives from. Single source of truth lives
// in ThemePresets — this file goes away once Diff consumers migrate.
internal static class DiffPalette
{
    // ~18% alpha blend of FileChanges.StatusAdded (0xFF57F287) over Commits.Background (0xFF1E1F22).
    public const uint LineAddedBg = 0xFF284534;
    // ~18% alpha blend of FileChanges.StatusDeleted (0xFFED4245) over Commits.Background (0xFF1E1F22).
    public const uint LineRemovedBg = 0xFF432528;

    public const uint LineAddedGlyphText = 0xFF57F287;     // FileChanges.StatusAdded
    public const uint LineRemovedGlyphText = 0xFFED4245;   // FileChanges.StatusDeleted
    public const uint LineContextGlyphText = 0xFF96989D;   // Text.Header (== FileChanges.HeaderText)
    public const uint LineNumberText = 0xFF7A7C81;

    public const uint HunkSeparatorBg = 0xFF222326;        // FileChanges.HeaderBg
    public const uint HunkSeparatorRangeText = 0xFF96989D; // Text.Header
    public const uint HunkSeparatorContextText = 0xFFB5B9C0; // CommitDetails.Secondary

    public const uint BannerBg = HunkSeparatorBg;
    public const uint BannerText = HunkSeparatorContextText;

    public const uint LineText = 0xFFE6E6E6; // CommitDetails.Primary

    public const uint TruncatedFooterBg = HunkSeparatorBg;
    public const uint TruncatedFooterText = HunkSeparatorRangeText;
}

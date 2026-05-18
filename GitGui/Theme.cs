namespace GitGui;

/// <summary>
/// Foundation colors shared across the per-area palettes. Per-area palettes
/// (<see cref="CommitsPalette"/>, <see cref="DialogPalette"/>, etc.) still own
/// names with semantic meaning ("HeaderBg", "RowText"); they reference these
/// constants for the values that genuinely belong to the whole app, so a brand-wide
/// adjustment changes one number.
/// </summary>
internal static class Theme
{
    // Surfaces
    public const uint BgPanel = 0xFF1E1F22;        // default panel/dialog background
    public const uint BgHeader = 0xFF2A2C30;       // header strips, hovered rows
    public const uint BgDeep = 0xFF1A1B1E;         // commit-details background
    public const uint Border = 0xFF313338;         // 1-px panel/section borders

    // Text
    public const uint TextStrong = 0xFFFFFFFF;
    public const uint TextPrimary = 0xFFE6E6E6;
    public const uint TextRow = 0xFFB5B9C0;
    public const uint TextDim = 0xFF7A7C81;
    public const uint TextHeader = 0xFF96989D;
}

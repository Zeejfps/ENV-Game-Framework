namespace GitGui;

/// <summary>
/// Foundation colors referenced by the few remaining static palettes that haven't
/// been migrated to <see cref="ThemeStyles"/> yet (<see cref="CommitsPalette"/>,
/// <see cref="FileChangesPalette"/>). New code should prefer themed bindings against
/// <c>ThemeStyles</c> directly so theme toggles take effect without rebuilding views.
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

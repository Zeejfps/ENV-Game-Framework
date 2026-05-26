using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Legacy compile-time consts kept alive while non-migrated dialog views still reference
/// them. Values mirror <see cref="ThemePresets"/>.Dark.Dialog one-to-one. Each consumer
/// migrates to <see cref="IThemeService"/> on its own pass — this class goes away once
/// the Dialogs sweep is complete and Phase 4 cleanup runs.
/// </summary>
internal static class DialogPalette
{
    public const uint Background = Theme.BgPanel;
    public const uint Border = Theme.Border;
    public const uint Separator = Theme.BgHeader;
    public const uint TitleText = Theme.TextPrimary;
    public const uint BodyText = 0xFFDCDDDE;

    public const uint ButtonNormal = 0xFF2B2D31;
    public const uint ButtonHover = 0xFF3A3D43;
    public const uint ButtonBorder = 0xFF3E4047;
    public const uint ButtonBorderHover = 0xFF5865F2;

    public const uint CloseNormal = 0x00000000;
    public const uint CloseHover = 0xFF3A3D43;
    public const uint CloseTextNormal = Theme.TextRow;
    public const uint CloseTextHover = Theme.TextStrong;

    public const uint RowTransparent = 0x00000000;
    public const uint RowHover = 0xFF2B2D31;
    public const uint RowActive = 0xFF404C8C;
    public const uint RowText = Theme.TextRow;
    public const uint RowTextActive = Theme.TextStrong;
    public const uint RowTextMissing = 0x80B5B9C0;
    public const uint SectionHeaderText = Theme.TextHeader;

    public const uint IconAccentWorktree = 0xFF5DADE2;
    public const uint IconAccentSubmodule = 0xFFB57EDC;

    /// <summary>
    /// Apply the bordered-button chrome to <paramref name="background"/> by tagging it with
    /// <see cref="StyleClassNames.DialogButton"/> and binding a <c>hovered</c> modifier. The
    /// sheet (built by <see cref="StyleSheetBuilder"/>) supplies the normal + hovered colors,
    /// so callers automatically pick up theme swaps without further changes.
    /// </summary>
    public static void BindBorderedButtonChrome(RectView background, IReadable<bool> isHovered)
    {
        background.StyleClasses.Add(StyleClassNames.DialogButton);
        background.BindModifier(ModifierNames.Hovered, isHovered);
    }

    public static void BindBorderedButtonChrome(RectView background, Func<bool> isEffectivelyHovered)
    {
        background.StyleClasses.Add(StyleClassNames.DialogButton);
        background.BindModifier(ModifierNames.Hovered, isEffectivelyHovered);
    }
}

using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

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

    // Icon-only accents for child rows nested under a primary in the RepoBar. The icon
    // stays tinted regardless of hover/active so it remains a stable "what kind of row
    // is this" identifier; the label still tracks the active/missing palette.
    public const uint IconAccentWorktree = 0xFF5DADE2;
    public const uint IconAccentSubmodule = 0xFFB57EDC;

    public static void BindBorderedButtonChrome(RectView background, IReadable<bool> isHovered)
    {
        background.BindThemedBackgroundColor(s =>
            isHovered.Value ? s.BorderedButton.BackgroundHover : s.BorderedButton.BackgroundIdle);
        background.BindThemedBorderColor(s =>
            BorderColorStyle.All(isHovered.Value ? s.BorderedButton.BorderHover : s.BorderedButton.BorderIdle));
    }

    /// <summary>
    /// Same chrome bindings as the IReadable&lt;bool&gt; overload but driven by a derived
    /// predicate — useful when "is hovered" needs to be combined with another state (e.g.
    /// disabled buttons that shouldn't react to the pointer).
    /// </summary>
    public static void BindBorderedButtonChrome(RectView background, Func<bool> isEffectivelyHovered)
    {
        background.BindThemedBackgroundColor(s =>
            isEffectivelyHovered() ? s.BorderedButton.BackgroundHover : s.BorderedButton.BackgroundIdle);
        background.BindThemedBorderColor(s =>
            BorderColorStyle.All(isEffectivelyHovered() ? s.BorderedButton.BorderHover : s.BorderedButton.BorderIdle));
    }
}
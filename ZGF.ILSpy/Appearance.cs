using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The screen's colors, icons and strings.</summary>
public static class Appearance
{
    /// <summary>Takes the colors of <paramref name="theme"/> and the strings of <paramref name="locale"/>.</summary>
    public static void ApplyThemeAndLanguage(UiState uiState, AppTheme theme, Locale locale)
    {
        uiState.Theme = theme.Ui;
        uiState.CodeViewPalette = theme.Code;
        uiState.NodeIconPalette = theme.Icons;
        uiState.Strings = Strings.For(locale);
    }

    /// <summary>Takes the icons of <paramref name="iconTheme"/> and the fonts they are drawn in.</summary>
    public static void ApplyIconTheme(UiState uiState, IconTheme iconTheme)
    {
        uiState.Icons = iconTheme.Icons;
        uiState.IconFonts = iconTheme.Fonts;
    }
}

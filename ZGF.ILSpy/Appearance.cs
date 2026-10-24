using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The screen's colors and strings.</summary>
public static class Appearance
{
    /// <summary>Takes the colors of <paramref name="theme"/> and the strings of <paramref name="locale"/>.</summary>
    public static void ApplyThemeAndLanguage(UiState uiState, AppThemeId theme, Locale locale)
    {
        var colors = AppThemes.For(theme);
        uiState.Theme = colors.Ui;
        uiState.CodeViewPalette = colors.Code;
        uiState.NodeIconPalette = colors.Icons;
        uiState.Strings = Strings.For(locale);
    }
}

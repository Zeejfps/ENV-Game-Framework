using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The screen's colors and strings.</summary>
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
}

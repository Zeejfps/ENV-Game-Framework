using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>Everything the app is drawn with in one theme: the UI theme, the editor's colors and
/// the tree's icon colors.</summary>
public sealed record AppTheme(UiTheme Ui, CodeViewPalette Code, NodeIconPalette Icons)
{
    /// <summary>A theme of these colors, with the UI theme in the app's compact metrics.</summary>
    public static AppTheme FromColors(UiTheme ui, CodeViewPalette code, NodeIconPalette icons) => new(Shell(ui), code, icons);

    private static UiTheme Shell(UiTheme theme) => theme.WithDensity(UiDensity.Compact) with
    {
        ScreenPadding = 0f,
        TabMinWidth = 120f,
        TabMaxWidth = 240f,
    };
}

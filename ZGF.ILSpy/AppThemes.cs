using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

internal static class AppThemes
{
    public static readonly UiTheme Dark = Compact(UiTheme.Dark);
    public static readonly UiTheme Light = Compact(UiTheme.Light);

    public const float BarHeight = 24f;

    private static UiTheme Compact(UiTheme theme) => theme with
    {
        FontSize = 13f,
        ScreenPadding = 0f,
        ControlHeight = 26f,
        ControlPaddingX = 10f,
        CornerRadius = 4f,
        RowHeight = 22f,
        MenuItemHeight = 24f,
    };
}

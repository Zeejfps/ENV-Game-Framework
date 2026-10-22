using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>Everything the app is drawn with in one theme: the UI theme, the editor's colors and
/// the tree's icon colors.</summary>
public sealed record AppTheme(UiTheme Ui, CodeViewPalette Code, NodeIconPalette Icons);

internal static class AppThemes
{
    public static readonly AppTheme Dark = new(Shell(UiTheme.Dark), CodeViewPalette.Dark, NodeIconPalette.Dark);
    public static readonly AppTheme Light = new(Shell(UiTheme.Light), CodeViewPalette.Light, NodeIconPalette.Light);

    public static AppTheme For(bool dark) => dark ? Dark : Light;

    private static UiTheme Shell(UiTheme theme) => theme.WithDensity(UiDensity.Compact) with
    {
        ScreenPadding = 0f,
        TabMinWidth = 120f,
        TabMaxWidth = 240f,
    };
}

/// <summary>Sizes that belong to this app's layout rather than to the theme.</summary>
internal static class AppLayout
{
    public const float SearchWidth = 360f;
    public const float SearchMinWidth = 220f;
    public const float FindWidth = 260f;
    public const float FindMinWidth = 140f;
    public const float ErrorDialogWidth = 760f;
    public const float ErrorDialogHeight = 480f;
    public const int ErrorListRows = 5;
    public const float PickerWidth = 560f;
    public const float PickerHeight = 0.75f;
}

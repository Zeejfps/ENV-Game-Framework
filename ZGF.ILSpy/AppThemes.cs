using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>Everything the app is drawn with in one theme: the UI theme, the editor's colors and
/// the tree's icon colors.</summary>
public sealed record AppTheme(UiTheme Ui, CodeViewPalette Code, NodeIconPalette Icons);

/// <summary>The themes the app can be drawn in.</summary>
public enum AppThemeId
{
    Dark,
    Light,
    GitHubDark,
    OneDarkPro,
    Dracula,
    Monokai,
    CatppuccinMocha,
    GitHubLight,
    SolarizedLight,
}

internal static class AppThemes
{
    public static readonly AppTheme Dark = new(Shell(UiTheme.Dark), CodeViewPalette.Dark, NodeIconPalette.Dark);
    public static readonly AppTheme Light = new(Shell(UiTheme.Light), CodeViewPalette.Light, NodeIconPalette.Light);
    public static readonly AppTheme GitHubDark = new(Shell(UiTheme.GitHubDark), CodeViewPalette.GitHubDark, NodeIconPalette.GitHubDark);
    public static readonly AppTheme OneDarkPro = new(Shell(UiTheme.OneDarkPro), CodeViewPalette.OneDarkPro, NodeIconPalette.OneDarkPro);
    public static readonly AppTheme Dracula = new(Shell(UiTheme.Dracula), CodeViewPalette.Dracula, NodeIconPalette.Dracula);
    public static readonly AppTheme Monokai = new(Shell(UiTheme.Monokai), CodeViewPalette.Monokai, NodeIconPalette.Monokai);
    public static readonly AppTheme CatppuccinMocha = new(Shell(UiTheme.CatppuccinMocha), CodeViewPalette.CatppuccinMocha,
        NodeIconPalette.CatppuccinMocha);
    public static readonly AppTheme GitHubLight = new(Shell(UiTheme.GitHubLight), CodeViewPalette.GitHubLight, NodeIconPalette.GitHubLight);
    public static readonly AppTheme SolarizedLight = new(Shell(UiTheme.SolarizedLight), CodeViewPalette.SolarizedLight,
        NodeIconPalette.SolarizedLight);

    /// <summary>The themes named after the color schemes they follow, by their proper names, which
    /// are not translated.</summary>
    public static IReadOnlyList<(AppThemeId Id, string Name)> Named { get; } =
    [
        (AppThemeId.GitHubDark, "GitHub Dark"),
        (AppThemeId.OneDarkPro, "One Dark Pro"),
        (AppThemeId.Dracula, "Dracula"),
        (AppThemeId.Monokai, "Monokai"),
        (AppThemeId.CatppuccinMocha, "Catppuccin Mocha"),
        (AppThemeId.GitHubLight, "GitHub Light"),
        (AppThemeId.SolarizedLight, "Solarized Light"),
    ];

    public static AppTheme For(AppThemeId id) => id switch
    {
        AppThemeId.Light => Light,
        AppThemeId.GitHubDark => GitHubDark,
        AppThemeId.OneDarkPro => OneDarkPro,
        AppThemeId.Dracula => Dracula,
        AppThemeId.Monokai => Monokai,
        AppThemeId.CatppuccinMocha => CatppuccinMocha,
        AppThemeId.GitHubLight => GitHubLight,
        AppThemeId.SolarizedLight => SolarizedLight,
        _ => Dark,
    };

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

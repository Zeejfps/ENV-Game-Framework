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
    /// <summary>Every theme, the first one for an unknown id. Those named after the color schemes
    /// they follow carry their proper names, which are not translated.</summary>
    private static readonly (AppThemeId Id, string? Name, AppTheme Theme)[] All =
    [
        (AppThemeId.Dark, null, Create(UiTheme.Dark, CodeViewPalette.Dark, NodeIconPalette.Dark)),
        (AppThemeId.Light, null, Create(UiTheme.Light, CodeViewPalette.Light, NodeIconPalette.Light)),
        (AppThemeId.GitHubDark, "GitHub Dark", Create(UiTheme.GitHubDark, CodeViewPalette.GitHubDark, NodeIconPalette.GitHubDark)),
        (AppThemeId.OneDarkPro, "One Dark Pro", Create(UiTheme.OneDarkPro, CodeViewPalette.OneDarkPro, NodeIconPalette.OneDarkPro)),
        (AppThemeId.Dracula, "Dracula", Create(UiTheme.Dracula, CodeViewPalette.Dracula, NodeIconPalette.Dracula)),
        (AppThemeId.Monokai, "Monokai", Create(UiTheme.Monokai, CodeViewPalette.Monokai, NodeIconPalette.Monokai)),
        (AppThemeId.CatppuccinMocha, "Catppuccin Mocha", Create(UiTheme.CatppuccinMocha, CodeViewPalette.CatppuccinMocha, NodeIconPalette.CatppuccinMocha)),
        (AppThemeId.GitHubLight, "GitHub Light", Create(UiTheme.GitHubLight, CodeViewPalette.GitHubLight, NodeIconPalette.GitHubLight)),
        (AppThemeId.SolarizedLight, "Solarized Light", Create(UiTheme.SolarizedLight, CodeViewPalette.SolarizedLight, NodeIconPalette.SolarizedLight)),
    ];

    /// <summary>The themes that carry a proper name, for a menu to list by it.</summary>
    public static IReadOnlyList<(AppThemeId Id, string Name)> Named { get; } = ListNamed();

    public static AppTheme For(AppThemeId id)
    {
        foreach (var entry in All)
        {
            if (entry.Id == id) return entry.Theme;
        }
        return All[0].Theme;
    }

    /// <summary>The editor colors provided around this part of the UI, or the dark theme's outside any.</summary>
    public static CodeViewPalette GetCodeViewPalette(this Ui ui)
    {
        var palette = ui.Get<CodeViewPalette>();
        if (palette != null)
            return palette;
        return CodeViewPalette.Dark;
    }

    /// <summary>The icon colors provided around this part of the UI, or the dark theme's outside any.</summary>
    public static NodeIconPalette GetNodeIconPalette(this Ui ui)
    {
        var palette = ui.Get<NodeIconPalette>();
        if (palette != null)
            return palette;
        return NodeIconPalette.Dark;
    }

    private static (AppThemeId Id, string Name)[] ListNamed() => All
        .Where(static entry => entry.Name != null)
        .Select(static entry => (entry.Id, entry.Name!))
        .ToArray();

    private static AppTheme Create(UiTheme ui, CodeViewPalette code, NodeIconPalette icons) => new(Shell(ui), code, icons);

    private static UiTheme Shell(UiTheme theme) => theme.WithDensity(UiDensity.Compact) with
    {
        ScreenPadding = 0f,
        TabMinWidth = 120f,
        TabMaxWidth = 240f,
    };
}

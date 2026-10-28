using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Preferences;
using static ZGF.ILSpy.Theming;

namespace ZGF.ILSpy;

/// <summary>The icon themes the app can be drawn with: the built-in ones and those the user keeps in the
/// icon themes folder beside the settings, read again, fonts and pictures too, whenever a file in that
/// folder changes. What is wrong in an icon theme is reported once, until it is fixed.</summary>
public static class IconTheming
{
    private const string IconThemeFilePattern = "*" + IconThemeFile.Extension;
    private const string SchemaFileName = "icon-theme.schema.json";

    /// <summary>The files whose changes the icon themes folder is watched for: the theme files, the fonts
    /// and the pictures.</summary>
    public static readonly IReadOnlyList<string> IconThemeFiles = [IconThemeFilePattern, "*.ttf", "*.otf", "*.svg"];

    /// <summary>Reads the user's icon themes into the catalog and reports what is wrong in any icon theme.</summary>
    public static void LoadIconThemes(AppState appState)
    {
        LoadUserIconThemes(appState, []);
    }

    /// <summary>Reads the user's icon themes again, reporting only what is newly wrong.</summary>
    public static void ReloadUserIconThemes(AppState appState)
    {
        var known = appState.IconThemes.Errors.ToList();
        LoadUserIconThemes(appState, known);
    }

    private static void LoadUserIconThemes(AppState appState, IReadOnlyList<ThemeError> known)
    {
        var files = ReadUserIconThemes(appState.Settings.IconThemesFolder);
        appState.IconThemes.LoadUserThemes(files);
        ReportNewErrors(appState, appState.IconThemes.Errors, known, ThemeKind.Icon);
    }

    private static List<IconThemeFile> ReadUserIconThemes(string? folder)
    {
        if (folder == null)
            return new List<IconThemeFile>();
        return ListThemeFiles(folder, IconThemeFilePattern, SchemaFileName)
            .Select(IconThemeFile.ReadFile)
            .ToList();
    }

    /// <summary>The icon theme the app is drawn with: the chosen one as resolved, or the reference one while
    /// the chosen one's file is missing. The choice is kept, so the icon theme comes back with its file.</summary>
    public static IconTheme ResolveIconTheme(AppState appState) => appState.IconThemes.Find(appState.IconTheme);

    /// <summary>The user's icon themes folder, made with the icon theme schema in it when missing, for the
    /// screen to show; null when the settings live in memory only, or after reporting why it cannot be made.</summary>
    public static string? PrepareIconThemesFolder(AppState appState)
    {
        var folder = appState.Settings.IconThemesFolder;
        if (folder == null)
            return null;
        var error = CreateThemeFolder(folder, SchemaFileName, BuiltInIconThemes.ReadSchema(), BuiltInIconThemes.ReadGuide());
        if (error == null)
            return folder;
        appState.ReportError(new AppProblem.ThemesFolderNotMade(ThemeKind.Icon, error));
        return null;
    }

    /// <summary>Writes a new user icon theme that extends the icon theme shown and lists every icon it can
    /// change, commented out as shown, then reads the icon themes again and switches to it. Returns its file
    /// for the screen to open; null when the settings live in memory only, or after reporting why the
    /// file cannot be written.</summary>
    public static string? CreateCustomIconTheme(AppState appState)
    {
        var folder = PrepareIconThemesFolder(appState);
        if (folder == null)
            return null;
        var themes = appState.IconThemes;
        var parent = themes.FindShownId(appState.IconTheme);
        var id = FindFreeCustomId(parent, candidate => IsTaken(themes, folder, candidate));
        var path = Path.Combine(folder, id + IconThemeFile.Extension);
        var name = NameCustomTheme(appState, themes, parent);
        var text = IconThemeDraft.Compose(name, parent, themes.Find(parent));
        if (!TryWriteNewFile(appState, path, text, ThemeKind.Icon))
            return null;
        ReloadUserIconThemes(appState);
        SetIconTheme(appState, id);
        return path;
    }

    /// <summary>Reads the user's icon themes again whenever a file in their folder changes, until
    /// <see cref="StopWatchingUserIconThemes"/>, so the chosen icon theme redraws as its files are saved.</summary>
    public static void WatchUserIconThemes(AppState appState)
    {
        var folder = new WatchedThemeFolder(appState.IconThemesWatch, PrepareIconThemesFolder, ReloadUserIconThemes, ThemeKind.Icon);
        WatchThemeFolder(appState, folder);
    }

    public static void StopWatchingUserIconThemes(AppState appState)
    {
        appState.IconThemesWatch.Stop();
    }
}

using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Preferences;

namespace ZGF.ILSpy;

/// <summary>A folder of the user's themes of one kind as watching keeps it: the watch over it, how it is
/// made, how its themes are read again, and the title of the error that tells it cannot be watched.</summary>
internal sealed record WatchedThemeFolder(
    ThemeFolderWatch Watch,
    Func<AppState, string?> Prepare,
    Action<AppState> Reload,
    Func<Strings, string> UnwatchedTitle);

/// <summary>The themes the app can be drawn in: the built-in ones and those the user keeps in the themes
/// folder beside the settings, read again whenever that folder changes. What is wrong in a theme file is
/// reported once, until it is fixed.</summary>
public static class Theming
{
    private const string ThemeFilePattern = "*" + ThemeFile.Extension;
    private const string SchemaFileName = "theme.schema.json";
    private const string GuideFileName = "README.md";
    private const string CustomSuffix = "-custom";
    private const int FirstCustomNumber = 2;

    /// <summary>Reads the user's themes into the catalog and reports what is wrong in any theme.</summary>
    public static void LoadThemes(AppState appState)
    {
        LoadUserThemes(appState, []);
    }

    /// <summary>Reads the user's themes again, reporting only what is newly wrong.</summary>
    public static void ReloadUserThemes(AppState appState)
    {
        var known = appState.Themes.Errors.ToList();
        LoadUserThemes(appState, known);
    }

    private static void LoadUserThemes(AppState appState, IReadOnlyList<ThemeError> known)
    {
        var files = ReadUserThemes(appState.Settings.ThemesFolder);
        appState.Themes.LoadUserThemes(files);
        ReportNewErrors(appState, appState.Themes.Errors, known, TitleThemeErrors);
    }

    private static string TitleThemeErrors(Strings t, string file) => t.ErrorTheme(file);

    private static List<ThemeFile> ReadUserThemes(string? folder)
    {
        if (folder == null)
            return new List<ThemeFile>();
        return ListThemeFiles(folder, ThemeFilePattern, SchemaFileName)
            .Select(ThemeFile.ReadFile)
            .ToList();
    }

    /// <summary>The files <paramref name="pattern"/> matches in <paramref name="folder"/> by name, without
    /// the schema <paramref name="schemaFileName"/>; none when the folder is missing or cannot be listed.</summary>
    internal static List<string> ListThemeFiles(string folder, string pattern, string schemaFileName)
    {
        try
        {
            return Directory.EnumerateFiles(folder, pattern)
                .Where(p => !IsNamed(p, schemaFileName))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }

    private static bool IsNamed(string path, string fileName) => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Reports the errors of <paramref name="errors"/> not in <paramref name="known"/>, one report
    /// per file, titled by <paramref name="title"/> from the file's name.</summary>
    internal static void ReportNewErrors(AppState appState, IReadOnlyList<ThemeError> errors, IReadOnlyList<ThemeError> known,
        Func<Strings, string, string> title)
    {
        var byFile = errors
            .Where(e => !known.Contains(e))
            .GroupBy(static e => e.File);
        foreach (var fileErrors in byFile)
            ReportErrorsOf(appState, fileErrors.Key, fileErrors, title);
    }

    private static void ReportErrorsOf(AppState appState, string file, IEnumerable<ThemeError> errors, Func<Strings, string, string> title)
    {
        var t = Strings.For(appState.Locale);
        var lines = errors.Select(e => DescribeError(t, e));
        appState.ReportError(title(t, Path.GetFileName(file)), string.Join(Environment.NewLine, lines));
    }

    /// <summary><paramref name="error"/> as a line of a report: the file, the line when known, and what is wrong.</summary>
    private static string DescribeError(Strings t, ThemeError error)
    {
        var problem = DescribeProblem(t, error);
        if (error.Line == 0)
            return $"{error.File}: {problem}";
        return $"{error.File}({error.Line}): {problem}";
    }

    private static string DescribeProblem(Strings t, ThemeError error) => error.Problem switch
    {
        ThemeProblem.Unreadable => t.ThemeErrorUnreadable(error.Detail),
        ThemeProblem.InvalidJson => t.ThemeErrorInvalidJson(error.Detail),
        ThemeProblem.RootNotAnObject => t.ThemeErrorRootNotAnObject,
        ThemeProblem.NotAnObject => t.ThemeErrorNotAnObject(error.Subject),
        ThemeProblem.NotAString => t.ThemeErrorNotAString(error.Subject),
        ThemeProblem.InvalidName => t.ThemeErrorInvalidName,
        ThemeProblem.DuplicateLanguage => t.ThemeErrorDuplicateLanguage(error.Subject),
        ThemeProblem.UnknownKey => t.ThemeErrorUnknownKey(error.Subject),
        ThemeProblem.UnknownType => t.ThemeErrorUnknownType(error.Subject),
        ThemeProblem.NotAColor => t.ThemeErrorNotAColor(error.Subject, error.Detail),
        ThemeProblem.TransparentBlack => t.ThemeErrorTransparentBlack(error.Subject),
        ThemeProblem.UnknownPaletteName => t.ThemeErrorUnknownPaletteName(error.Subject, error.Detail),
        ThemeProblem.NoType => t.ThemeErrorNoType,
        ThemeProblem.UnknownParent => t.ThemeErrorUnknownParent(error.Subject),
        ThemeProblem.ExtendsCycle => t.ThemeErrorExtendsCycle(error.Detail),
        ThemeProblem.MissingColors => t.ThemeErrorMissingColors(error.Detail),
        ThemeProblem.IdTaken => t.ThemeErrorIdTaken(error.Subject),
        ThemeProblem.NotACodepoint => t.ThemeErrorNotACodepoint(error.Subject, error.Detail),
        ThemeProblem.InvalidIcon => t.ThemeErrorInvalidIcon(error.Subject),
        ThemeProblem.UnknownFont => t.ThemeErrorUnknownFont(error.Subject, error.Detail),
        ThemeProblem.AssetOutsideFolder => t.ThemeErrorAssetOutsideFolder(error.Subject, error.Detail),
        ThemeProblem.AssetUnreadable => t.ThemeErrorAssetUnreadable(error.Subject, error.Detail),
        ThemeProblem.NotAFont => t.ThemeErrorNotAFont(error.Subject, error.Detail),
        ThemeProblem.InvalidSvg => t.ThemeErrorInvalidSvg(error.Subject, error.Detail),
        ThemeProblem.MissingGlyph => t.ThemeErrorMissingGlyph(error.Subject, error.Detail),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Problem, null),
    };

    /// <summary>The theme the app is drawn in: the chosen one as resolved, or the built-in dark one while
    /// the chosen one's file is missing. The choice is kept, so the theme comes back with its file.</summary>
    public static AppTheme ResolveTheme(AppState appState) => appState.Themes.Find(appState.Theme);

    /// <summary>The user's themes folder, made with the theme schema in it when missing, for the screen to
    /// show; null when the settings live in memory only, or after reporting why it cannot be made.</summary>
    public static string? PrepareThemesFolder(AppState appState)
    {
        var folder = appState.Settings.ThemesFolder;
        if (folder == null)
            return null;
        var error = CreateThemeFolder(folder, SchemaFileName, BuiltInThemes.ReadSchema(), BuiltInThemes.ReadGuide());
        if (error == null)
            return folder;
        appState.ReportError(Strings.For(appState.Locale).ErrorThemesFolder, error);
        return null;
    }

    /// <summary>Makes <paramref name="folder"/> with <paramref name="schema"/> in it as
    /// <paramref name="schemaFileName"/>, for theme files to name in <c>$schema</c>, and a README of
    /// <paramref name="guide"/> on writing themes. Returns why it failed, or null.</summary>
    internal static string? CreateThemeFolder(string folder, string schemaFileName, string schema, string guide)
    {
        try
        {
            Directory.CreateDirectory(folder);
            WriteIfChanged(Path.Combine(folder, schemaFileName), schema);
            WriteIfChanged(Path.Combine(folder, GuideFileName), guide);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return $"{folder}{Environment.NewLine}{e.Message}";
        }
    }

    private static void WriteIfChanged(string path, string text)
    {
        if (IsWritten(path, text))
            return;
        File.WriteAllText(path, text);
    }

    private static bool IsWritten(string path, string text)
    {
        if (!File.Exists(path))
            return false;
        return File.ReadAllText(path) == text;
    }

    /// <summary>Writes a new user theme that extends the theme shown and lists every color it can change,
    /// commented out at the color shown, then reads the themes again and switches to it. Returns its file
    /// for the screen to open; null when the settings live in memory only, or after reporting why the
    /// file cannot be written.</summary>
    public static string? CreateCustomTheme(AppState appState)
    {
        var folder = PrepareThemesFolder(appState);
        if (folder == null)
            return null;
        var themes = appState.Themes;
        var parent = themes.FindShownId(appState.Theme);
        var id = FindFreeCustomId(parent, candidate => IsTaken(themes, folder, candidate));
        var path = Path.Combine(folder, id + ThemeFile.Extension);
        var name = NameCustomTheme(appState, themes, parent);
        var text = ThemeDraft.Compose(name, parent, themes.Find(parent));
        var errorTitle = Strings.For(appState.Locale).ErrorCustomTheme;
        if (!TryWriteNewFile(appState, path, text, errorTitle))
            return null;
        ReloadUserThemes(appState);
        SetTheme(appState, id);
        return path;
    }

    /// <summary><paramref name="parent"/> followed by <c>-custom</c>, numbered from 2 while
    /// <paramref name="isTaken"/> says a theme or a file already has that id.</summary>
    internal static string FindFreeCustomId(string parent, Func<string, bool> isTaken)
    {
        var id = parent + CustomSuffix;
        var number = FirstCustomNumber;
        while (isTaken(id))
        {
            id = $"{parent}{CustomSuffix}-{number}";
            number++;
        }
        return id;
    }

    /// <summary>True when a theme of <paramref name="themes"/> has the id <paramref name="id"/>, or a file
    /// in <paramref name="folder"/> is named by it.</summary>
    internal static bool IsTaken<TFile, TTheme>(ThemeCatalog<TFile, TTheme> themes, string folder, string id)
        where TFile : class, IThemeFile
        where TTheme : class
    {
        if (themes.IndexOf(id) >= 0)
            return true;
        return File.Exists(Path.Combine(folder, id + ThemeFile.Extension));
    }

    /// <summary>The name of a custom theme made from the theme <paramref name="parent"/> of <paramref name="themes"/>.</summary>
    internal static string NameCustomTheme<TFile, TTheme>(AppState appState, ThemeCatalog<TFile, TTheme> themes, string parent)
        where TFile : class, IThemeFile
        where TTheme : class
    {
        var t = Strings.For(appState.Locale);
        var parentName = themes.NameOf(parent, t.Culture);
        return t.ThemeCustomName(parentName);
    }

    /// <summary>Writes <paramref name="text"/> to a file that must not exist yet; false after reporting why
    /// it cannot, under <paramref name="errorTitle"/>.</summary>
    internal static bool TryWriteNewFile(AppState appState, string path, string text, string errorTitle)
    {
        try
        {
            using var file = new StreamWriter(path, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write });
            file.Write(text);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            appState.ReportError(errorTitle, $"{path}{Environment.NewLine}{e.Message}");
            return false;
        }
    }

    /// <summary>Reads the user's themes again whenever their folder changes, until
    /// <see cref="StopWatchingUserThemes"/>, so the chosen theme redraws as its file is saved.</summary>
    public static void WatchUserThemes(AppState appState)
    {
        var folder = new WatchedThemeFolder(appState.ThemesWatch, PrepareThemesFolder, ReloadUserThemes, TitleUnwatchedThemes);
        WatchThemeFolder(appState, folder);
    }

    private static string TitleUnwatchedThemes(Strings t) => t.ErrorWatchThemes;

    /// <summary>Reads the themes of <paramref name="folder"/> again whenever it changes, until its watch
    /// stops. Makes the folder when it is missing, and again when it is removed while watched. When the
    /// folder cannot be made or watched, reports why and leaves the themes to be read again by hand.</summary>
    internal static async void WatchThemeFolder(AppState appState, WatchedThemeFolder folder)
    {
        if (!StartWatching(appState, folder))
            return;
        var restartedSinceChange = false;
        while (true)
        {
            var outcome = await Background.Wait(folder.Watch.NextChange());
            var news = outcome.Value;
            if (news.Kind == ThemeFolderEventKind.Stopped)
                return;
            var isLost = news.Kind == ThemeFolderEventKind.Lost;
            if (isLost && !RestartWatching(appState, folder, news.Reason, restartedSinceChange))
                return;
            restartedSinceChange = isLost;
            folder.Reload(appState);
        }
    }

    /// <summary>Makes the folder and watches it; false when there is none to watch, or after reporting
    /// why it cannot be made or watched.</summary>
    private static bool StartWatching(AppState appState, WatchedThemeFolder folder)
    {
        var path = folder.Prepare(appState);
        if (path == null)
            return false;
        var error = folder.Watch.Start(path);
        if (error == null)
            return true;
        ReportUnwatched(appState, folder, error);
        return false;
    }

    /// <summary>Watches the folder again after the watch was lost, as when the folder was removed. Once
    /// it was restarted with no change seen since, it gives up rather than lose it again and again, and
    /// reports <paramref name="reason"/>.</summary>
    private static bool RestartWatching(AppState appState, WatchedThemeFolder folder, string reason, bool restartedSinceChange)
    {
        if (!restartedSinceChange)
            return StartWatching(appState, folder);
        ReportUnwatched(appState, folder, reason);
        return false;
    }

    private static void ReportUnwatched(AppState appState, WatchedThemeFolder folder, string reason)
    {
        var t = Strings.For(appState.Locale);
        appState.ReportError(folder.UnwatchedTitle(t), reason);
    }

    public static void StopWatchingUserThemes(AppState appState)
    {
        appState.ThemesWatch.Stop();
    }
}

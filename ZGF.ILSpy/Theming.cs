using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Preferences;

namespace ZGF.ILSpy;

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
        ReportNewErrors(appState, known);
    }

    private static List<ThemeFile> ReadUserThemes(string? folder)
    {
        if (folder == null)
            return new List<ThemeFile>();
        return ListThemeFiles(folder)
            .Select(ThemeFile.ReadFile)
            .ToList();
    }

    /// <summary>The theme files in <paramref name="folder"/> by name, without the schema; none when the
    /// folder is missing or cannot be listed.</summary>
    private static List<string> ListThemeFiles(string folder)
    {
        try
        {
            return Directory.EnumerateFiles(folder, ThemeFilePattern)
                .Where(static p => !IsSchema(p))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }

    private static bool IsSchema(string path) => string.Equals(Path.GetFileName(path), SchemaFileName, StringComparison.OrdinalIgnoreCase);

    private static void ReportNewErrors(AppState appState, IReadOnlyList<ThemeError> known)
    {
        var byFile = appState.Themes.Errors
            .Where(e => !known.Contains(e))
            .GroupBy(static e => e.File);
        foreach (var errors in byFile)
            ReportErrorsOf(appState, errors.Key, errors);
    }

    private static void ReportErrorsOf(AppState appState, string file, IEnumerable<ThemeError> errors)
    {
        var t = Strings.For(appState.Locale);
        var title = t.ErrorTheme(Path.GetFileName(file));
        var lines = errors.Select(e => DescribeError(t, e));
        appState.ReportError(title, string.Join(Environment.NewLine, lines));
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
        var error = CreateThemesFolder(folder);
        if (error == null)
            return folder;
        appState.ReportError(Strings.For(appState.Locale).ErrorThemesFolder, error);
        return null;
    }

    /// <summary>Makes <paramref name="folder"/> with the current theme schema in it, for theme files to
    /// name in <c>$schema</c>, and a README on writing themes. Returns why it failed, or null.</summary>
    private static string? CreateThemesFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            WriteIfChanged(Path.Combine(folder, SchemaFileName), BuiltInThemes.ReadSchema());
            WriteIfChanged(Path.Combine(folder, GuideFileName), BuiltInThemes.ReadGuide());
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
        var id = FindFreeCustomId(themes, folder, parent);
        var path = Path.Combine(folder, id + ThemeFile.Extension);
        var text = ThemeDraft.Compose(NameCustomTheme(appState, parent), parent, themes.Find(parent));
        if (!TryWriteNewFile(appState, path, text))
            return null;
        ReloadUserThemes(appState);
        SetTheme(appState, id);
        return path;
    }

    /// <summary><paramref name="parent"/> followed by <c>-custom</c>, numbered from 2 while a theme or a
    /// file already has that id.</summary>
    private static string FindFreeCustomId(ThemeCatalog themes, string folder, string parent)
    {
        var id = parent + CustomSuffix;
        var number = FirstCustomNumber;
        while (IsTaken(themes, folder, id))
        {
            id = $"{parent}{CustomSuffix}-{number}";
            number++;
        }
        return id;
    }

    private static bool IsTaken(ThemeCatalog themes, string folder, string id)
    {
        if (themes.IndexOf(id) >= 0)
            return true;
        return File.Exists(Path.Combine(folder, id + ThemeFile.Extension));
    }

    private static string NameCustomTheme(AppState appState, string parent)
    {
        var t = Strings.For(appState.Locale);
        var parentName = appState.Themes.NameOf(parent, t.Culture);
        return t.ThemeCustomName(parentName);
    }

    /// <summary>Writes <paramref name="text"/> to a file that must not exist yet; false after reporting why
    /// it cannot.</summary>
    private static bool TryWriteNewFile(AppState appState, string path, string text)
    {
        try
        {
            using var file = new StreamWriter(path, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write });
            file.Write(text);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            appState.ReportError(Strings.For(appState.Locale).ErrorCustomTheme, $"{path}{Environment.NewLine}{e.Message}");
            return false;
        }
    }

    /// <summary>Reads the user's themes again whenever their folder changes, until
    /// <see cref="StopWatchingUserThemes"/>, so the chosen theme redraws as its file is saved. Makes the
    /// folder when it is missing, and again when it is removed while watched. When the folder cannot be
    /// made or watched, reports why and leaves the themes to be read again by hand.</summary>
    public static async void WatchUserThemes(AppState appState)
    {
        if (!StartWatching(appState))
            return;
        var restartedSinceChange = false;
        while (true)
        {
            var outcome = await Background.Wait(appState.ThemesWatch.NextChange());
            var news = outcome.Value;
            if (news.Kind == ThemeFolderEventKind.Stopped)
                return;
            var isLost = news.Kind == ThemeFolderEventKind.Lost;
            if (isLost && !RestartWatching(appState, news.Reason, restartedSinceChange))
                return;
            restartedSinceChange = isLost;
            ReloadUserThemes(appState);
        }
    }

    /// <summary>Makes the themes folder and watches it; false when there is none to watch, or after
    /// reporting why it cannot be made or watched.</summary>
    private static bool StartWatching(AppState appState)
    {
        var folder = PrepareThemesFolder(appState);
        if (folder == null)
            return false;
        var error = appState.ThemesWatch.Start(folder);
        if (error == null)
            return true;
        ReportUnwatched(appState, error);
        return false;
    }

    /// <summary>Watches the folder again after the watch was lost, as when the folder was removed. Once
    /// it was restarted with no change seen since, it gives up rather than lose it again and again, and
    /// reports <paramref name="reason"/>.</summary>
    private static bool RestartWatching(AppState appState, string reason, bool restartedSinceChange)
    {
        if (!restartedSinceChange)
            return StartWatching(appState);
        ReportUnwatched(appState, reason);
        return false;
    }

    private static void ReportUnwatched(AppState appState, string reason)
    {
        appState.ReportError(Strings.For(appState.Locale).ErrorWatchThemes, reason);
    }

    public static void StopWatchingUserThemes(AppState appState)
    {
        appState.ThemesWatch.Stop();
    }
}

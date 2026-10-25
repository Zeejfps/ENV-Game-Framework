using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.AssemblyLists;

namespace ZGF.ILSpy;

/// <summary>Saving the settings when something in them changed.</summary>
public static class Settings
{
    /// <summary>Saves the settings when what they are saved from changed since the last save written,
    /// unless the assemblies are reloading or a failed write is still waiting to be tried again at
    /// <paramref name="now"/>. Returns the seconds left before a failed write is tried again, or 0.</summary>
    public static double SaveSettingsIfChanged(AppState appState, double now)
    {
        if (appState.IsReloading)
            return 0;
        var sources = CaptureSavedSources(appState);
        if (!appState.LastSave.IsBehind(sources))
            return 0;
        var wait = appState.SaveRetry.Remaining(now);
        if (wait > 0)
            return wait;
        SaveSettings(appState);
        return appState.SaveRetry.Remaining(now);
    }

    /// <summary>Writes the settings, counting what they were copied from as saved only once the write succeeds.</summary>
    public static void SaveSettings(AppState appState)
    {
        RememberActiveAssemblyList(appState);
        CopyStateToSettings(appState);
        var sources = CaptureSavedSources(appState);
        var error = appState.Settings.Save();
        TakeWriteOutcome(appState, sources, error);
    }

    internal static SavedSources CaptureSavedSources(AppState appState)
    {
        return new SavedSources(
            appState.Assemblies.Version,
            appState.Lists.Revision,
            appState.Bookmarks.Revision,
            appState.Analyzer.Revision,
            appState.ViewSettingsChanges,
            appState.Theme,
            appState.IconTheme,
            appState.Language,
            appState.DecompilerOptions,
            appState.ApiVisibility,
            appState.Locale);
    }

    private static void CopyStateToSettings(AppState appState)
    {
        var settings = appState.Settings;
        settings.AssemblyLists = appState.Lists.Save();
        settings.ActiveAssemblyList = appState.Lists.Active;
        settings.Theme = appState.Theme;
        settings.IconTheme = appState.IconTheme;
        settings.Language = appState.Language;
        settings.Decompiler = appState.DecompilerOptions;
        settings.ApiVisibility = appState.ApiVisibility;
        settings.Locale = appState.Locale;
        settings.Bookmarks = appState.Bookmarks.Save();
    }

    private static void TakeWriteOutcome(AppState appState, SavedSources sources, string? error)
    {
        if (error == null)
        {
            appState.LastSave.Record(sources);
            appState.SaveRetry.Succeed();
            return;
        }
        ReportWriteFailure(appState, error);
    }

    /// <summary>Reports a failed write, unless writes were failing already.</summary>
    private static void ReportWriteFailure(AppState appState, string error)
    {
        if (!appState.SaveRetry.Fail())
            return;
        appState.ReportError(Strings.For(appState.Locale).ErrorSaveSettings, error);
    }

    /// <summary>Takes what the settings keep of the screen, as it changed, to be written with the next save.</summary>
    public static void KeepViewSettings(AppState appState, ViewSettings view)
    {
        var settings = appState.Settings;
        settings.BookmarkColumns = view.BookmarkColumns;
        settings.SearchColumns = view.SearchColumns;
        settings.ExportColumns = view.ExportColumns;
        settings.FoldedOptionGroups = view.FoldedOptionGroups;
        settings.SearchKinds = view.SearchKinds;
        settings.FlagsFilters = view.FlagsFilters;
        settings.ExportWindow = view.ExportWindow;
        settings.Layout = view.Layout;
        settings.Display = view.Display;
        settings.NuGet = view.NuGet;
        appState.ViewSettingsChanges++;
    }

    /// <summary>What the settings kept of the screen when they were last saved.</summary>
    public static ViewSettings ReadViewSettings(AppState appState)
    {
        var settings = appState.Settings;
        return new ViewSettings(
            settings.BookmarkColumns,
            settings.SearchColumns,
            settings.ExportColumns,
            settings.FoldedOptionGroups,
            settings.SearchKinds,
            settings.FlagsFilters,
            settings.ExportWindow,
            settings.Layout,
            settings.Display,
            settings.NuGet);
    }
}

using System.Diagnostics;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.AssemblyLists;

namespace ZGF.ILSpy;

/// <summary>Saving the settings when something in them changed.</summary>
public static class Settings
{
    private static readonly Saving.Unchanged Unchanged = new();

    /// <summary>Saves the settings when what they are saved from changed since the last save written,
    /// unless the assemblies are reloading or a failed write is still waiting to be tried again at
    /// <paramref name="now"/>.</summary>
    public static Saving SaveSettingsIfChanged(AppState appState, double now)
    {
        if (appState.IsReloading)
            return Unchanged;
        var sources = CaptureSavedSources(appState);
        if (!appState.LastSave.IsBehind(sources))
            return Unchanged;
        var wait = appState.SaveRetry.Remaining(now);
        if (wait > 0)
            return new Saving.Waiting(wait);
        return SaveSettings(appState) switch
        {
            SettingsWrite.Written => new Saving.Saved(),
            SettingsWrite.Failed failed => new Saving.Failed(failed.Reason, appState.SaveRetry.Remaining(now)),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>Tells the user what was wrong with the settings file, if anything.</summary>
    public static void ReportSettingsProblem(AppState appState, SettingsProblem problem)
    {
        if (problem is SettingsProblem.None)
            return;
        appState.ReportError(new AppProblem.SettingsUnreadable(appState.Settings.FilePath, problem));
    }

    /// <summary>Writes the settings, counting what they were copied from as saved only once the write
    /// succeeds. A failed write is reported, unless writes were failing already.</summary>
    public static SettingsWrite SaveSettings(AppState appState)
    {
        RememberActiveAssemblyList(appState);
        CopyStateToSettings(appState);
        var sources = CaptureSavedSources(appState);
        var write = appState.Settings.Save();
        TakeWrite(appState, sources, write);
        return write;
    }

    internal static SavedSources CaptureSavedSources(AppState appState)
    {
        return new SavedSources(
            appState.Assemblies.Version,
            appState.Lists.Revision,
            appState.Bookmarks.Revision,
            appState.Analyzer.Revision,
            appState.Tabs.Changes,
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

    private static void TakeWrite(AppState appState, SavedSources sources, SettingsWrite write)
    {
        switch (write)
        {
            case SettingsWrite.Written:
                appState.LastSave.Record(sources);
                appState.SaveRetry.Succeed();
                break;
            case SettingsWrite.Failed failed:
                ReportWriteFailure(appState, failed.Reason);
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>Reports a failed write, unless writes were failing already.</summary>
    private static void ReportWriteFailure(AppState appState, string error)
    {
        if (!appState.SaveRetry.Fail())
            return;
        appState.ReportError(new AppProblem.SettingsNotSaved(error));
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
        settings.NuGetWindow = view.NuGetWindow;
        KeepTabPanels(appState, view.Tabs);
        appState.ViewSettingsChanges++;
    }

    private static void KeepTabPanels(AppState appState, IReadOnlyList<TabPanel>? panels)
    {
        if (panels == null)
            return;
        appState.TabPanels = panels;
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
            settings.NuGet,
            settings.NuGetWindow,
            Tabs: null);
    }
}

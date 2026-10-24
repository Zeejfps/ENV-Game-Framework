using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Saving the settings when something in them changed, and saving resources to files.</summary>
public static class Saving
{
    /// <summary>Saves the settings when what they are saved from changed since the last save written,
    /// unless the assemblies are reloading or a failed write is still waiting to be tried again at
    /// <paramref name="now"/>. Returns the seconds left before a failed write is tried again, or 0.</summary>
    public static double SaveSettingsIfChanged(this Workspace w, double now)
    {
        if (w.IsReloading)
            return 0;
        var sources = w.CaptureSavedSources();
        if (!w.LastSave.IsBehind(sources))
            return 0;
        var wait = w.SaveRetry.Remaining(now);
        if (wait > 0)
            return wait;
        w.SaveSettings();
        return w.SaveRetry.Remaining(now);
    }

    /// <summary>Writes the settings, counting what they were copied from as saved only once the write succeeds.</summary>
    public static void SaveSettings(this Workspace w)
    {
        w.RememberActiveList();
        w.CopyStateToSettings();
        var sources = w.CaptureSavedSources();
        var error = w.Settings.Save();
        w.TakeWriteOutcome(sources, error);
    }

    internal static SavedSources CaptureSavedSources(this Workspace w)
    {
        return new SavedSources(
            w.Assemblies.Version,
            w.Lists.Revision,
            w.Bookmarks.Revision,
            w.Analyzer.Revision,
            w.MetadataFilters.Revision,
            w.Theme,
            w.Language,
            w.DecompilerOptions,
            w.ShowNonPublic,
            w.Locale,
            w.Search.Kinds);
    }

    private static void CopyStateToSettings(this Workspace w)
    {
        var settings = w.Settings;
        settings.AssemblyLists = w.Lists.Save();
        settings.ActiveAssemblyList = w.Lists.Active;
        settings.Theme = w.Theme;
        settings.Language = w.Language;
        settings.Decompiler = w.DecompilerOptions;
        settings.FoldedOptionGroups = w.FoldedOptionGroups
            .Order()
            .ToList();
        settings.ShowNonPublic = w.ShowNonPublic;
        settings.SearchKinds = w.Search.Kinds;
        settings.Locale = w.Locale;
        settings.Layout = w.Layout;
        settings.Bookmarks = w.Bookmarks.Save();
        settings.BookmarkColumns = w.BookmarkColumns.Weights.ToList();
        settings.SearchColumns = w.SearchColumns.Weights.ToList();
        settings.ExportColumns = w.ExportColumns.Weights.ToList();
        settings.FlagsFilters = w.MetadataFilters.Save();
        settings.ExportWindow = w.ExportSetup.Window;
    }

    private static void TakeWriteOutcome(this Workspace w, SavedSources sources, string? error)
    {
        if (error == null)
        {
            w.LastSave.Record(sources);
            w.SaveRetry.Succeed();
            return;
        }
        w.ReportWriteFailure(error);
    }

    /// <summary>Reports a failed write, unless writes were failing already.</summary>
    private static void ReportWriteFailure(this Workspace w, string error)
    {
        if (!w.SaveRetry.Fail())
            return;
        w.ReportError(Strings.For(w.Locale).ErrorSaveSettings, error);
    }

    /// <summary>Writes the bytes of the resource <paramref name="node"/> stands for to the file at
    /// <paramref name="path"/>.</summary>
    public static void SaveResource(this Workspace w, BrowseNode node, string path)
    {
        try
        {
            var bytes = node.ReadResource();
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception e) when (IsFileError(e))
        {
            w.ReportError(Strings.For(w.Locale).ErrorSaveResource(node.Text), e.Message);
        }
    }

    private static bool IsFileError(Exception e)
    {
        if (e is IOException)
            return true;
        if (e is UnauthorizedAccessException)
            return true;
        return e is InvalidDataException;
    }
}

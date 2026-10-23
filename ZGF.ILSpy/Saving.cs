using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Saving the settings when something in them changed, and saving resources to files.</summary>
public static class Saving
{
    /// <summary>Saves the settings when the assembly list, the bookmarks, the analyzer or the
    /// metadata filters changed since the last save, unless the assemblies are reloading.</summary>
    public static void SaveSettingsIfChanged(this Workspace w)
    {
        if (w.IsReloading)
            return;
        if (!w.LastSave.IsBehind(w.Assemblies, w.Bookmarks, w.Analyzer, w.MetadataFilters))
            return;
        w.SaveSettings();
    }

    public static void SaveSettings(this Workspace w)
    {
        w.CopyStateToSettings();
        w.LastSave.Record(w.Assemblies, w.Bookmarks, w.Analyzer, w.MetadataFilters);
        w.WriteSettingsFile();
    }

    private static void CopyStateToSettings(this Workspace w)
    {
        var settings = w.Settings;
        settings.Assemblies = w.Assemblies.FilePaths.ToList();
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
        settings.Analyzed = w.LastSessionAnalyses.ListToSave(w.Analyzer);
        settings.BookmarkColumns = w.BookmarkColumns.Weights.ToList();
        settings.SearchColumns = w.SearchColumns.Weights.ToList();
        settings.ExportColumns = w.ExportColumns.Weights.ToList();
        settings.FlagsFilters = w.MetadataFilters.Save();
        settings.ExportWindow = w.ExportSetup.Window;
    }

    private static void WriteSettingsFile(this Workspace w)
    {
        var error = w.Settings.Save();
        if (error == null)
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

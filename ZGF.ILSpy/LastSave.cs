using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What the settings are saved from: the versions of the open assemblies, the named lists, the bookmarks,
/// the analyzer, the metadata filters, the layout, what the screen handed over of its view state and the export
/// window, and the preferences themselves.</summary>
public readonly record struct SavedSources(
    int Assemblies,
    int Lists,
    int Bookmarks,
    int Analyzer,
    int Filters,
    int Layout,
    int View,
    int ExportWindow,
    AppThemeId Theme,
    DecompilationLanguage Language,
    DecompilerOptions DecompilerOptions,
    bool ShowNonPublic,
    Locale Locale,
    SearchKinds SearchKinds);

/// <summary>What the settings were saved from the last time, to save again only when it changed.</summary>
public sealed class LastSave
{
    private SavedSources _saved;

    public bool IsBehind(SavedSources sources) => sources != _saved;

    public void Record(SavedSources sources)
    {
        _saved = sources;
    }
}

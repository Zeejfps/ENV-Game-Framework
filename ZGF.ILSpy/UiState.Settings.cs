using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy;

public sealed partial class UiState
{
    private ViewSources _keptView;

    /// <summary>Hands the app state what its settings keep of the screen, when that changed since it last did.</summary>
    private void KeepChangedViewSettings()
    {
        var sources = CaptureViewSources();
        if (sources == _keptView)
            return;
        HandOverViewSettings();
    }

    /// <summary>Hands the app state what its settings keep of the screen as it is now, changed or not,
    /// as before the last save.</summary>
    public void HandOverViewSettings()
    {
        _keptView = CaptureViewSources();
        KeepViewSettings(_appState, CreateViewSettings());
    }

    private ViewSources CaptureViewSources()
    {
        return new ViewSources(
            BookmarkColumns.Changes,
            SearchColumns.Changes,
            ExportColumns.Changes,
            FoldedOptionGroups.Revision,
            Search.Kinds,
            MetadataFilters.Revision,
            ExportSetup.WindowChanges,
            Layout.Changes);
    }

    private ViewSettings CreateViewSettings()
    {
        return new ViewSettings(
            BookmarkColumns.Weights.ToList(),
            SearchColumns.Weights.ToList(),
            ExportColumns.Weights.ToList(),
            FoldedOptionGroups.Save(),
            Search.Kinds,
            MetadataFilters.Save(),
            ExportSetup.Window,
            Layout.CopyArrangement());
    }

    /// <summary>What the settings keep of the screen is made from: the change counts of the columns,
    /// the folded groups, the flags filters, the export window and the layout, and what the search looks for.</summary>
    private readonly record struct ViewSources(
        int BookmarkColumns,
        int SearchColumns,
        int ExportColumns,
        int FoldedOptionGroups,
        SearchKinds SearchKinds,
        int FlagsFilters,
        int ExportWindow,
        int Layout);
}

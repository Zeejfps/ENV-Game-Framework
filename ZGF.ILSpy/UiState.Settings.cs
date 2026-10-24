using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

public sealed partial class UiState
{
    private ViewSources _keptView;

    /// <summary>Hands the workspace what its settings keep of the screen, when that changed since it last did.</summary>
    private void KeepViewSettings()
    {
        var sources = CaptureViewSources();
        if (sources == _keptView)
            return;
        _keptView = sources;
        _workspace.KeepViewSettings(CreateViewSettings());
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
            ExportSetup.WindowChanges);
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
            ExportSetup.Window);
    }

    /// <summary>What the settings keep of the screen is made from: the change counts of the columns,
    /// the folded groups, the flags filters and the export window, and what the search looks for.</summary>
    private readonly record struct ViewSources(
        int BookmarkColumns,
        int SearchColumns,
        int ExportColumns,
        int FoldedOptionGroups,
        SearchKinds SearchKinds,
        int FlagsFilters,
        int ExportWindow);
}

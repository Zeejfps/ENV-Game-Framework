using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What the settings last kept of the screen, by what it was made from, so that it is handed
/// over again only once something in it changed.</summary>
public sealed class KeptView
{
    private ViewSources _kept;

    public bool HasChanged(ViewSources sources) => sources != _kept;

    public void Keep(ViewSources sources)
    {
        _kept = sources;
    }
}

/// <summary>What the settings keep of the screen is made from: the change counts of the columns,
/// the folded groups, the flags filters, the export window, the layout and the NuGet dialog's search,
/// what the search looks for, and the display options.</summary>
public readonly record struct ViewSources(
    int BookmarkColumns,
    int SearchColumns,
    int ExportColumns,
    int FoldedOptionGroups,
    SearchKinds SearchKinds,
    int FlagsFilters,
    int ExportWindow,
    int Layout,
    DisplayOptions Display,
    int NuGetSearch);

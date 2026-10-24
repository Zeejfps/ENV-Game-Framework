using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>What the settings keep of the screen: the column widths of the bookmarks, search results
/// and export tables, the decompiler options panel's folded groups, what the search looks for, the
/// metadata grids' flags filters, where the export dialog's window was left, and the layout.</summary>
public sealed record ViewSettings(
    List<float> BookmarkColumns,
    List<float> SearchColumns,
    List<float> ExportColumns,
    List<string> FoldedOptionGroups,
    SearchKinds SearchKinds,
    Dictionary<string, SavedFilterState> FlagsFilters,
    WindowBounds? ExportWindow,
    DockLayout Layout);

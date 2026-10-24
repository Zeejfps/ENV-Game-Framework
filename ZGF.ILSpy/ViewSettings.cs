namespace ZGF.ILSpy;

/// <summary>What the settings keep of the screen: the column widths of the bookmarks, search results
/// and export tables, and the decompiler options panel's folded groups.</summary>
public sealed record ViewSettings(
    List<float> BookmarkColumns,
    List<float> SearchColumns,
    List<float> ExportColumns,
    List<string> FoldedOptionGroups);

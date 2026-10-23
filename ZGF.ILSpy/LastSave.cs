using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The versions of the assembly list, the bookmarks, the analyzer and the metadata filters
/// as of the last settings save, to save again only when one changed.</summary>
public sealed class LastSave
{
    private int _assemblies;
    private int _bookmarks;
    private int _analyzer;
    private int _filters;

    public bool IsBehind(AssemblyList assemblies, BookmarkList bookmarks, AnalyzerTree analyzer, MetadataFilters filters)
    {
        if (assemblies.Version != _assemblies)
            return true;
        if (bookmarks.Revision != _bookmarks)
            return true;
        if (analyzer.Revision != _analyzer)
            return true;
        return filters.Revision != _filters;
    }

    public void Record(AssemblyList assemblies, BookmarkList bookmarks, AnalyzerTree analyzer, MetadataFilters filters)
    {
        _assemblies = assemblies.Version;
        _bookmarks = bookmarks.Revision;
        RecordAnalyzer(analyzer);
        _filters = filters.Revision;
    }

    /// <summary>Counts the analyzer as saved, for changes that were not the user's.</summary>
    public void RecordAnalyzer(AnalyzerTree analyzer)
    {
        _analyzer = analyzer.Revision;
    }
}

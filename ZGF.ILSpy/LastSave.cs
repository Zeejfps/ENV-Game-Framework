namespace ZGF.ILSpy;

/// <summary>The revisions of the bookmarks, the analyzer and the metadata filters as of the last
/// settings save, to save again only when one changed.</summary>
public sealed class LastSave
{
    private int _bookmarks;
    private int _analyzer;
    private int _filters;

    public bool IsBehind(BookmarkList bookmarks, AnalyzerTree analyzer, MetadataFilters filters)
    {
        if (bookmarks.Revision != _bookmarks)
            return true;
        if (analyzer.Revision != _analyzer)
            return true;
        return filters.Revision != _filters;
    }

    public void Record(BookmarkList bookmarks, AnalyzerTree analyzer, MetadataFilters filters)
    {
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

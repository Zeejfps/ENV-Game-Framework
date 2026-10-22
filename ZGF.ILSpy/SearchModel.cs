using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What the search results are sorted by; <see cref="Relevance"/> puts the closest matches first.</summary>
public enum SearchSort
{
    Relevance,
    Name,
    Location,
    Assembly,
}

/// <summary>The search panel's query, streamed results and selection. While the panel is open a
/// query runs once typing pauses; assemblies opened later are searched and their hits added, and
/// hits in closed ones dropped, keeping the selection.</summary>
public sealed class SearchModel
{
    private const double DebounceSeconds = 0.15;

    private readonly Func<Strings> _strings;
    private readonly Action<string, string> _reportError;
    private readonly List<SearchResult> _rows = new();
    private readonly HashSet<LoadedAssembly> _searched = new(ReferenceEqualityComparer.Instance);
    private readonly List<CancellationTokenSource> _runs = new();
    private string _typed = "";
    private double _typedAt;
    private string? _query;
    private SearchKinds _kinds;
    private int _version = -1;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="reportError">Tells the user a search failed: a title and details.</param>
    public SearchModel(Func<Strings> strings, Action<string, string> reportError)
    {
        _strings = strings;
        _reportError = reportError;
    }

    public string Query = "";

    /// <summary>What the query looks for, unless prefixes in it say otherwise.</summary>
    public SearchKinds Kinds = SearchKinds.TypesAndMembers;

    public IReadOnlyList<SearchResult> Rows => _rows;
    public bool IsBusy => _runs.Count > 0;

    public ListSelection Selection { get; } = new();

    /// <summary>The selected row, or -1.</summary>
    public int Selected => Selection.Index;

    public SearchResult? SelectedRow => Selected >= 0 ? _rows[Selected] : null;

    public SearchSort Sort { get; private set; }
    public bool Descending { get; private set; }

    /// <summary>Starts a search when the query settled, or searches assemblies opened since. Does
    /// nothing unless the results are <paramref name="shown"/>. Returns the seconds left before a
    /// typed query settles, or 0.</summary>
    public double Update(AssemblyList assemblies, double now, bool shown)
    {
        if (Query != _typed)
        {
            _typed = Query;
            _typedAt = now;
        }
        if (!shown) return 0;
        if (_typed != _query || Kinds != _kinds)
        {
            var remaining = DebounceSeconds - (now - _typedAt);
            if (remaining > 0 && Kinds == _kinds) return remaining;
            Restart(assemblies);
        }
        else if (assemblies.Version != _version)
        {
            Extend(assemblies);
        }
        return 0;
    }

    public void Select(int index) => Selection.Select(index, _rows.Count);

    public void Move(int by) => Selection.Move(by, _rows.Count);

    public void Clear() => Query = "";

    /// <summary>Sorts the results by <paramref name="sort"/>, ascending, then descending when they
    /// already are, then by relevance again, keeping the selection in view.</summary>
    public void SortBy(SearchSort sort)
    {
        if (Sort == sort && Descending)
            sort = SearchSort.Relevance;
        Descending = Sort == sort && sort != SearchSort.Relevance;
        Sort = sort;
        var selected = SelectedRow?.Node;
        _rows.Sort(Compare);
        Reselect(selected);
        Selection.Reveal();
    }

    private int Compare(SearchResult a, SearchResult b)
    {
        var order = Sort switch
        {
            SearchSort.Name => Compare(a.Text, b.Text, a.Location, b.Location, a.Node.Assembly.NameAndVersion, b.Node.Assembly.NameAndVersion),
            SearchSort.Location => Compare(a.Location, b.Location, a.Text, b.Text, a.Node.Assembly.NameAndVersion, b.Node.Assembly.NameAndVersion),
            SearchSort.Assembly => Compare(a.Node.Assembly.NameAndVersion, b.Node.Assembly.NameAndVersion, a.Text, b.Text, a.Location, b.Location),
            _ => b.Fitness.CompareTo(a.Fitness) is var fit and not 0
                ? fit
                : Compare(a.Text, b.Text, a.Location, b.Location, a.Node.Assembly.NameAndVersion, b.Node.Assembly.NameAndVersion),
        };
        return Descending ? -order : order;
    }

    private static int Compare(string a1, string b1, string a2, string b2, string a3, string b3)
    {
        var order = StringComparer.OrdinalIgnoreCase.Compare(a1, b1);
        if (order == 0) order = StringComparer.OrdinalIgnoreCase.Compare(a2, b2);
        return order != 0 ? order : StringComparer.OrdinalIgnoreCase.Compare(a3, b3);
    }

    private void Insert(SearchResult row)
    {
        int low = 0, high = _rows.Count;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (Compare(_rows[mid], row) <= 0)
                low = mid + 1;
            else
                high = mid;
        }
        _rows.Insert(low, row);
    }

    private void Restart(AssemblyList assemblies)
    {
        foreach (var run in _runs)
            run.Cancel();
        _runs.Clear();
        _query = _typed;
        _kinds = Kinds;
        _version = assemblies.Version;
        _rows.Clear();
        _searched.Clear();
        Selection.Clear();
        var listed = assemblies.Assemblies;
        _searched.UnionWith(listed);
        if (!SearchQuery.Parse(_query, _kinds).IsEmpty)
            Start(assemblies, listed);
    }

    private void Extend(AssemblyList assemblies)
    {
        _version = assemblies.Version;
        var listed = assemblies.Assemblies;
        if (_searched.RemoveWhere(a => !listed.Contains(a)) > 0)
        {
            var selected = SelectedRow?.Node;
            _rows.RemoveAll(r => !_searched.Contains(r.Node.Assembly));
            Reselect(selected);
        }
        var added = listed.Where(_searched.Add).ToArray();
        if (added.Length > 0 && !SearchQuery.Parse(_query!, _kinds).IsEmpty)
            Start(assemblies, added);
    }

    private void Reselect(BrowseNode? selected)
    {
        if (selected == null)
        {
            Selection.Clear();
            return;
        }
        var index = _rows.FindIndex(r => ReferenceEquals(r.Node, selected));
        Selection.Select(index, _rows.Count, reveal: false);
    }

    private void Start(AssemblyList assemblies, IEnumerable<LoadedAssembly> within)
    {
        var work = new CancellationTokenSource();
        _runs.Add(work);
        _ = Run(assemblies.SearchAsync(SearchQuery.Parse(_query!, _kinds), within, work.Token), work);
    }

    private async Task Run(IAsyncEnumerable<IReadOnlyList<SearchResult>> search, CancellationTokenSource work)
    {
        try
        {
            await foreach (var batch in search)
                Deliver(batch, work);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _reportError(_strings().ErrorSearch, e.ToString());
        }
        _runs.Remove(work);
        work.Dispose();
    }

    private void Deliver(IReadOnlyList<SearchResult> batch, CancellationTokenSource work)
    {
        if (!_runs.Contains(work)) return;
        var selected = SelectedRow?.Node;
        foreach (var row in batch)
        {
            if (_searched.Contains(row.Node.Assembly))
                Insert(row);
        }
        if (selected is not null)
            Reselect(selected);
    }
}

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
public sealed class Search
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
    public Search(Func<Strings> strings, Action<string, string> reportError)
    {
        _strings = strings;
        _reportError = reportError;
    }

    public string Query = "";

    /// <summary>What the query looks for, unless prefixes in it say otherwise.</summary>
    public SearchKinds Kinds = SearchKinds.TypesAndMembers;

    public IReadOnlyList<SearchResult> Rows => _rows;
    public bool IsBusy => _runs.Count > 0;

    public ItemSelection<SearchResult> Selection { get; } = new();

    public SearchResult? SelectedRow => Selection.Item;

    public SearchSort Sort { get; private set; }
    public bool Descending { get; private set; }

    /// <summary>Starts a search when the query settled, or searches assemblies opened since. Does
    /// nothing unless the results are <paramref name="shown"/>. Returns the seconds left before a
    /// typed query settles, or 0.</summary>
    public double Update(AssemblyList assemblies, double now, bool shown)
    {
        NoteTyping(now);
        if (!shown)
            return 0;
        if (IsQueryChanged())
            return RestartWhenSettled(assemblies, now);
        if (assemblies.Version != _version)
            Extend(assemblies);
        return 0;
    }

    private void NoteTyping(double now)
    {
        if (Query == _typed)
            return;
        _typed = Query;
        _typedAt = now;
    }

    private bool IsQueryChanged() => _typed != _query || Kinds != _kinds;

    private double RestartWhenSettled(AssemblyList assemblies, double now)
    {
        var remaining = DebounceSeconds - (now - _typedAt);
        if (remaining > 0 && Kinds == _kinds)
            return remaining;
        Restart(assemblies);
        return 0;
    }

    private void Restart(AssemblyList assemblies)
    {
        CancelRuns();
        _query = _typed;
        _kinds = Kinds;
        _version = assemblies.Version;
        _rows.Clear();
        Selection.Clear();
        var listed = assemblies.Assemblies;
        _searched.Clear();
        _searched.UnionWith(listed);
        Start(assemblies, listed);
    }

    private void CancelRuns()
    {
        foreach (var run in _runs)
            run.Cancel();
        _runs.Clear();
    }

    private void Extend(AssemblyList assemblies)
    {
        _version = assemblies.Version;
        var listed = assemblies.Assemblies;
        DropClosed(listed);
        var added = MarkSearched(listed);
        if (added.Count > 0)
            Start(assemblies, added);
    }

    private void DropClosed(IReadOnlyList<LoadedAssembly> listed)
    {
        var closed = _searched.RemoveWhere(a => !listed.Contains(a));
        if (closed == 0)
            return;
        _rows.RemoveAll(r => !_searched.Contains(r.Node.Assembly));
        Selection.DropIfGone(_rows);
    }

    private List<LoadedAssembly> MarkSearched(IReadOnlyList<LoadedAssembly> listed)
    {
        var added = new List<LoadedAssembly>();
        foreach (var assembly in listed)
        {
            if (_searched.Add(assembly))
                added.Add(assembly);
        }
        return added;
    }

    private void Start(AssemblyList assemblies, IEnumerable<LoadedAssembly> within)
    {
        var query = SearchQuery.Parse(_query!, _kinds);
        if (query.IsEmpty)
            return;
        var work = new CancellationTokenSource();
        _runs.Add(work);
        var search = assemblies.SearchAsync(query, within, work.Token);
        Run(search, work);
    }

    private async void Run(IAsyncEnumerable<IReadOnlyList<SearchResult>> search, CancellationTokenSource work)
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
        if (!_runs.Contains(work))
            return;
        foreach (var row in batch)
        {
            if (_searched.Contains(row.Node.Assembly))
                Insert(row);
        }
    }

    private void Insert(SearchResult row)
    {
        var low = 0;
        var high = _rows.Count;
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

    public void Select(SearchResult row) => Selection.Select(row, _rows);

    public void Move(int by) => Selection.Move(by, _rows);

    public void Clear() => Query = "";

    /// <summary>Sorts the results by <paramref name="sort"/>, ascending, then descending when they
    /// already are, then by relevance again, keeping the selection in view.</summary>
    public void SortBy(SearchSort sort)
    {
        if (Sort == sort && Descending)
            sort = SearchSort.Relevance;
        Descending = Sort == sort && sort != SearchSort.Relevance;
        Sort = sort;
        _rows.Sort(Compare);
        Selection.Reveal(_rows);
    }

    private int Compare(SearchResult a, SearchResult b)
    {
        var order = CompareAscending(a, b);
        return Descending ? -order : order;
    }

    private int CompareAscending(SearchResult a, SearchResult b) => Sort switch
    {
        SearchSort.Name => CompareByName(a, b),
        SearchSort.Location => CompareByLocation(a, b),
        SearchSort.Assembly => CompareByAssembly(a, b),
        _ => CompareByRelevance(a, b),
    };

    private static int CompareByName(SearchResult a, SearchResult b) =>
        Compare(a.Text, b.Text, a.Location, b.Location, AssemblyOf(a), AssemblyOf(b));

    private static int CompareByLocation(SearchResult a, SearchResult b) =>
        Compare(a.Location, b.Location, a.Text, b.Text, AssemblyOf(a), AssemblyOf(b));

    private static int CompareByAssembly(SearchResult a, SearchResult b) =>
        Compare(AssemblyOf(a), AssemblyOf(b), a.Text, b.Text, a.Location, b.Location);

    private static int CompareByRelevance(SearchResult a, SearchResult b)
    {
        var fit = b.Fitness.CompareTo(a.Fitness);
        if (fit != 0)
            return fit;
        return CompareByName(a, b);
    }

    private static string AssemblyOf(SearchResult row) => row.Node.Assembly.NameAndVersion;

    private static int Compare(string a1, string b1, string a2, string b2, string a3, string b3)
    {
        var order = StringComparer.OrdinalIgnoreCase.Compare(a1, b1);
        if (order != 0)
            return order;
        order = StringComparer.OrdinalIgnoreCase.Compare(a2, b2);
        if (order != 0)
            return order;
        return StringComparer.OrdinalIgnoreCase.Compare(a3, b3);
    }
}

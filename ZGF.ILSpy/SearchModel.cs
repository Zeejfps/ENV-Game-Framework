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
    private BrowseNode? _selected;

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

    /// <summary>The selected row, or -1.</summary>
    public int Selected { get; private set; } = -1;

    public SearchResult? SelectedRow => Selected >= 0 ? _rows[Selected] : null;

    public SearchSort Sort { get; private set; }
    public bool Descending { get; private set; }

    /// <summary>Set when the selection moved and should be scrolled into view.</summary>
    public bool ScrollToSelected;

    /// <summary>Starts a search when the query settled, or searches assemblies opened since. Does
    /// nothing unless the results are <paramref name="shown"/>. Returns the seconds left before a
    /// typed query settles, or 0.</summary>
    public double Update(AssemblyList assemblies, double now, Action<Action> post, bool shown)
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
            Restart(assemblies, post);
        }
        else if (assemblies.Version != _version)
        {
            Extend(assemblies, post);
        }
        return 0;
    }

    public void Select(int index)
    {
        if (index < 0 || index >= _rows.Count) return;
        Selected = index;
        _selected = _rows[index].Node;
        ScrollToSelected = true;
    }

    public void Move(int by)
    {
        if (_rows.Count > 0)
            Select(Math.Clamp(Selected < 0 && by < 0 ? 0 : Selected + by, 0, _rows.Count - 1));
    }

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
        Reselect();
        ScrollToSelected = Selected >= 0;
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

    private void Restart(AssemblyList assemblies, Action<Action> post)
    {
        foreach (var run in _runs)
            run.Cancel();
        _runs.Clear();
        _query = _typed;
        _kinds = Kinds;
        _version = assemblies.Version;
        _rows.Clear();
        _searched.Clear();
        Selected = -1;
        _selected = null;
        var listed = assemblies.Assemblies;
        _searched.UnionWith(listed);
        if (!SearchQuery.Parse(_query, _kinds).IsEmpty)
            Start(assemblies, listed, post);
    }

    private void Extend(AssemblyList assemblies, Action<Action> post)
    {
        _version = assemblies.Version;
        var listed = assemblies.Assemblies;
        if (_searched.RemoveWhere(a => !listed.Contains(a)) > 0)
        {
            _rows.RemoveAll(r => !_searched.Contains(r.Node.Assembly));
            Reselect();
        }
        var added = listed.Where(_searched.Add).ToArray();
        if (added.Length > 0 && !SearchQuery.Parse(_query!, _kinds).IsEmpty)
            Start(assemblies, added, post);
    }

    private void Reselect()
    {
        Selected = _selected is null ? -1 : _rows.FindIndex(r => ReferenceEquals(r.Node, _selected));
        if (Selected < 0)
            _selected = null;
    }

    private void Start(AssemblyList assemblies, IEnumerable<LoadedAssembly> within, Action<Action> post)
    {
        var work = new CancellationTokenSource();
        _runs.Add(work);
        _ = Run(assemblies.SearchAsync(SearchQuery.Parse(_query!, _kinds), within, work.Token), work, post);
    }

    private async Task Run(IAsyncEnumerable<IReadOnlyList<SearchResult>> search, CancellationTokenSource work, Action<Action> post)
    {
        try
        {
            await foreach (var batch in search.ConfigureAwait(false))
            {
                post(() =>
                {
                    if (!_runs.Contains(work)) return;
                    foreach (var row in batch)
                    {
                        if (_searched.Contains(row.Node.Assembly))
                            Insert(row);
                    }
                    if (_selected is not null)
                        Reselect();
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            post(() => _reportError(_strings().ErrorSearch, e.ToString()));
        }
        post(() =>
        {
            _runs.Remove(work);
            work.Dispose();
        });
    }
}

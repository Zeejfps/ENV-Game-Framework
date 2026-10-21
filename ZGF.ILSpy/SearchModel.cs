using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A search hit with its location text formatted once, off the UI thread.</summary>
public sealed record SearchRow(SearchResult Result, string Location);

/// <summary>The search panel's query, streamed results and selection. While the panel is open a
/// query runs once typing pauses; assemblies opened later are searched and their hits added, and
/// hits in closed ones dropped, keeping the selection.</summary>
public sealed class SearchModel
{
    private const double DebounceSeconds = 0.15;

    private readonly Action<string, string> _reportError;
    private readonly List<SearchRow> _rows = new();
    private readonly HashSet<LoadedAssembly> _searched = new(ReferenceEqualityComparer.Instance);
    private readonly List<CancellationTokenSource> _runs = new();
    private string _typed = "";
    private double _typedAt;
    private string? _query;
    private int _version = -1;
    private BrowseNode? _selected;

    /// <param name="reportError">Tells the user a search failed: a title and details.</param>
    public SearchModel(Action<string, string> reportError)
    {
        _reportError = reportError;
    }

    public string Query = "";
    public bool IsOpen;

    public IReadOnlyList<SearchRow> Rows => _rows;
    public bool IsBusy => _runs.Count > 0;

    /// <summary>The selected row, or -1.</summary>
    public int Selected { get; private set; } = -1;

    public SearchRow? SelectedRow => Selected >= 0 ? _rows[Selected] : null;

    /// <summary>Set when the selection moved and should be scrolled into view.</summary>
    public bool ScrollToSelected;

    /// <summary>Starts a search when the query settled, or searches assemblies opened since. Does
    /// nothing while the panel is closed. Returns the seconds left before a typed query settles,
    /// or 0.</summary>
    public double Update(AssemblyList assemblies, double now, Action<Action> post)
    {
        if (Query != _typed)
        {
            _typed = Query;
            _typedAt = now;
        }
        if (!IsOpen) return 0;
        if (_typed != _query)
        {
            var remaining = DebounceSeconds - (now - _typedAt);
            if (remaining > 0) return remaining;
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
        _selected = _rows[index].Result.Node;
        ScrollToSelected = true;
    }

    public void Move(int by)
    {
        if (_rows.Count > 0)
            Select(Math.Clamp(Selected < 0 && by < 0 ? 0 : Selected + by, 0, _rows.Count - 1));
    }

    public void Clear()
    {
        Query = "";
        IsOpen = false;
    }

    private void Restart(AssemblyList assemblies, Action<Action> post)
    {
        foreach (var run in _runs)
            run.Cancel();
        _runs.Clear();
        _query = _typed;
        _version = assemblies.Version;
        _rows.Clear();
        _searched.Clear();
        Selected = -1;
        _selected = null;
        var listed = assemblies.Assemblies;
        _searched.UnionWith(listed);
        if (_query.Trim().Length > 0)
            Start(assemblies, listed, post);
    }

    private void Extend(AssemblyList assemblies, Action<Action> post)
    {
        _version = assemblies.Version;
        var listed = assemblies.Assemblies;
        if (_searched.RemoveWhere(a => !listed.Contains(a)) > 0)
        {
            _rows.RemoveAll(r => !_searched.Contains(r.Result.Node.Assembly));
            Reselect();
        }
        var added = listed.Where(_searched.Add).ToArray();
        if (added.Length > 0 && _query!.Trim().Length > 0)
            Start(assemblies, added, post);
    }

    private void Reselect()
    {
        Selected = _selected is null ? -1 : _rows.FindIndex(r => ReferenceEquals(r.Result.Node, _selected));
        if (Selected < 0)
            _selected = null;
    }

    private void Start(AssemblyList assemblies, IEnumerable<LoadedAssembly> within, Action<Action> post)
    {
        var work = new CancellationTokenSource();
        _runs.Add(work);
        _ = Run(assemblies.SearchAsync(_query!, within, cancellationToken: work.Token), work, post);
    }

    private async Task Run(IAsyncEnumerable<IReadOnlyList<SearchResult>> search, CancellationTokenSource work, Action<Action> post)
    {
        try
        {
            await foreach (var batch in search.ConfigureAwait(false))
            {
                var rows = new SearchRow[batch.Count];
                for (var i = 0; i < rows.Length; i++)
                    rows[i] = new SearchRow(batch[i], Locate(batch[i]));
                post(() =>
                {
                    if (!_runs.Contains(work)) return;
                    foreach (var row in rows)
                    {
                        if (_searched.Contains(row.Result.Node.Assembly))
                            _rows.Add(row);
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            post(() => _reportError("Search failed", e.ToString()));
        }
        post(() =>
        {
            _runs.Remove(work);
            work.Dispose();
        });
    }

    private static string Locate(SearchResult result)
    {
        var ns = result.Namespace.Length == 0 ? "-" : result.Namespace;
        return result.DeclaringType is { } type
            ? $"{type}  ·  {ns}  ·  {result.AssemblyName}"
            : $"{ns}  ·  {result.AssemblyName}";
    }
}

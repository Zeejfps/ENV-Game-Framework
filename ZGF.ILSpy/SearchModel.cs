using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A search hit with its location text formatted once, off the UI thread.</summary>
public sealed record SearchRow(SearchResult Result, string Location);

/// <summary>The search panel's query and streamed results. A query runs once typing pauses, and
/// again when the assembly list changes; a newer run cancels the older.</summary>
public sealed class SearchModel
{
    private const double DebounceSeconds = 0.15;

    private readonly List<SearchRow> _rows = new();
    private string _typed = "";
    private double _typedAt;
    private string _searched = "";
    private int _searchedVersion = -1;
    private CancellationTokenSource? _work;

    public string Query = "";
    public bool IsOpen;
    public int Selected = -1;

    public IReadOnlyList<SearchRow> Rows => _rows;
    public bool IsBusy => _work != null;

    /// <summary>Starts a search when the query settled or the list changed. Returns the seconds
    /// left before a typed query settles, or 0.</summary>
    public double Update(AssemblyList assemblies, double now, Action<Action> post)
    {
        if (Query != _typed)
        {
            _typed = Query;
            _typedAt = now;
        }
        if (_typed == _searched && assemblies.Version == _searchedVersion) return 0;

        var remaining = DebounceSeconds - (now - _typedAt);
        if (remaining > 0) return remaining;
        Start(assemblies, post);
        return 0;
    }

    public void Clear()
    {
        Query = "";
        IsOpen = false;
    }

    private void Start(AssemblyList assemblies, Action<Action> post)
    {
        _work?.Cancel();
        _searched = _typed;
        _searchedVersion = assemblies.Version;
        _rows.Clear();
        Selected = -1;
        if (_searched.Trim().Length == 0)
        {
            _work = null;
            return;
        }

        var work = new CancellationTokenSource();
        _work = work;
        _ = Run(assemblies, _searched, work, post);
    }

    private async Task Run(AssemblyList assemblies, string text, CancellationTokenSource work, Action<Action> post)
    {
        try
        {
            await foreach (var batch in assemblies.SearchAsync(text, cancellationToken: work.Token).ConfigureAwait(false))
            {
                var rows = new SearchRow[batch.Count];
                for (var i = 0; i < rows.Length; i++)
                    rows[i] = new SearchRow(batch[i], Locate(batch[i]));
                post(() =>
                {
                    if (_work == work)
                        _rows.AddRange(rows);
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"[ILSpy] Search failed: {e}");
        }
        post(() =>
        {
            if (_work != work) return;
            _work = null;
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

using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The bar over each document, one for all of them: which document's bar searches instead of
/// showing its path, the best matches it suggests, and the list of a crumb's children open under it.</summary>
public sealed class Omnibar
{
    /// <summary>How many of the best matches the search suggests.</summary>
    public const int MaxSuggestions = 10;

    private bool _isFocusPending;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="reportError">Tells the user a search failed.</param>
    public Omnibar(Func<Strings> strings, Action<AppProblem> reportError)
    {
        Search = new Search(strings, reportError);
    }

    /// <summary>The query typed into the bar and its matches, the closest first.</summary>
    public Search Search { get; }

    /// <summary>The document panel whose bar searches, or null while every bar shows its path.</summary>
    public string? SearchingPanel { get; private set; }

    /// <summary>The children of a crumb, open under it.</summary>
    public CrumbMenu Menu { get; } = new();

    public bool IsSearching => SearchingPanel != null;

    public bool IsSearchingIn(string panel) => SearchingPanel == panel;

    /// <summary>Has the bar of <paramref name="panel"/> search, from an empty query, its box taking focus.</summary>
    public void StartSearch(string panel)
    {
        SearchingPanel = panel;
        Search.Clear();
        _isFocusPending = true;
        Menu.IsOpen = false;
    }

    /// <summary>Has every bar show its path again.</summary>
    public void StopSearch()
    {
        SearchingPanel = null;
        Search.Clear();
        _isFocusPending = false;
    }

    /// <summary>True once after <see cref="StartSearch"/>: the search box should take focus.</summary>
    public bool TakeFocus()
    {
        var focus = _isFocusPending;
        _isFocusPending = false;
        return focus;
    }

    /// <summary>How many matches are suggested.</summary>
    public int SuggestionCount => Math.Min(Search.Rows.Count, MaxSuggestions);

    /// <summary>The suggestion selected, or null when none of those suggested is.</summary>
    public SearchResult? SelectedSuggestion
    {
        get
        {
            var index = Search.Selection.IndexIn(Search.Rows);
            if (index < 0)
                return null;
            if (index >= SuggestionCount)
                return null;
            return Search.Rows[index];
        }
    }

    /// <summary>The suggestion to go to: the one selected, else the best match; null when none is suggested.</summary>
    public SearchResult? FindSuggestionToShow()
    {
        var selected = SelectedSuggestion;
        if (selected != null)
            return selected;
        if (SuggestionCount == 0)
            return null;
        return Search.Rows[0];
    }

    /// <summary>Selects the suggestion <paramref name="by"/> rows on from the selected one, kept within those suggested.</summary>
    public void MoveSuggestion(int by)
    {
        if (SuggestionCount == 0)
            return;
        Search.Move(by);
        var index = Search.Selection.IndexIn(Search.Rows);
        if (index < SuggestionCount)
            return;
        Search.Select(Search.Rows[SuggestionCount - 1]);
    }
}

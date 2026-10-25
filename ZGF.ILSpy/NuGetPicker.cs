using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>
/// The "open from NuGet" dialog: the feed typed or picked from the user's NuGet.Config, read the
/// first time it opens, searched a page at a time once the query settles, and the package picked,
/// fetched at the version picked into the package cache for the view to open.
/// </summary>
public sealed class NuGetPicker
{
    private const double DebounceSeconds = 0.3;

    private readonly Func<Strings> _strings;
    private readonly Action<string, string> _reportError;
    private readonly List<FeedPackage> _results = new();
    private NuGetSetup? _setup;
    private bool _isReadingSetup;
    private CancellationTokenSource? _search;
    private CancellationTokenSource? _download;
    private FeedSearch? _searched;
    private NuGetFeed? _resultsFeed;
    private string _typedQuery = "";
    private string _typedUrl = "";
    private double _typedAt;
    private SavedNuGetSearch? _kept;
    private FeedPackage? _versionsOf;
    private string? _downloaded;
    private bool _focusQuery;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="reportError">Tells the user a search or a download failed: a title and details.</param>
    public NuGetPicker(Func<Strings> strings, Action<string, string> reportError)
    {
        _strings = strings;
        _reportError = reportError;
    }

    public bool IsOpen;
    public string Query = "";

    /// <summary>The URL of the service index of the feed to search, typed or picked from <see cref="Sources"/>.</summary>
    public string FeedUrl = "";

    public bool IncludePrerelease;

    /// <summary>The version of the selected package to open.</summary>
    public string Version = "";

    public ItemSelection<FeedPackage> Selection { get; } = new();

    public FeedPackage? Selected => Selection.Item;

    /// <summary>The packages on the page shown.</summary>
    public IReadOnlyList<FeedPackage> Results => _results;

    /// <summary>The page of the search shown, from 0.</summary>
    public int Page { get; private set; }

    /// <summary>How many pages the search has, at least one.</summary>
    public int PageCount { get; private set; } = 1;

    /// <summary>How many packages the search matched in all.</summary>
    public int TotalHits { get; private set; }

    public bool HasPreviousPage => Page > 0;

    public bool HasNextPage => Page + 1 < PageCount;

    /// <summary>The feeds the user's NuGet.Config lists, once the setup is read.</summary>
    public IReadOnlyList<NuGetSource> Sources
    {
        get
        {
            if (_setup == null)
                return [];
            return _setup.Sources;
        }
    }

    public bool IsReadingSetup => _setup == null;

    /// <summary>Whether <see cref="FeedUrl"/> is a URL a feed could be at.</summary>
    public bool HasFeedUrl => NuGetSource.FromUrl(FeedUrl) != null;

    /// <summary>The versions of the selected package, newest first.</summary>
    public string[] Versions { get; private set; } = [];

    public bool IsSearching => _search != null;

    /// <summary>Why the last search failed, or null.</summary>
    public string? SearchError { get; private set; }

    public bool IsDownloading => _download != null;

    /// <summary>The package and version being downloaded, while <see cref="IsDownloading"/>.</summary>
    public string DownloadingName { get; private set; } = "";

    /// <summary>How many times the feed searched or whether prereleases count changed, for the
    /// settings to keep <see cref="Save"/> once it did.</summary>
    public int SearchChanges { get; private set; }

    public bool CanOpen
    {
        get
        {
            if (Selected == null)
                return false;
            if (IsDownloading)
                return false;
            return Version.Length > 0;
        }
    }

    /// <summary>Searches the feed and counts prereleases as the settings kept them.</summary>
    public void Restore(SavedNuGetSearch? saved)
    {
        if (saved == null)
            return;
        if (string.IsNullOrWhiteSpace(saved.FeedUrl))
            return;
        _kept = saved;
        FeedUrl = saved.FeedUrl;
        IncludePrerelease = saved.IncludePrerelease;
    }

    /// <summary>The feed last searched and whether prereleases counted, for the settings to keep.</summary>
    public SavedNuGetSearch? Save() => _kept;

    public void Open()
    {
        IsOpen = true;
        _focusQuery = true;
        if (_setup == null)
            ReadSetup();
    }

    private async void ReadSetup()
    {
        if (_isReadingSetup)
            return;
        _isReadingSetup = true;
        var read = await Background.Run(NuGetSetup.ReadUser);
        _isReadingSetup = false;
        if (read.Error != null)
        {
            _reportError(_strings().ErrorReadNugetConfig, read.Error.ToString());
            return;
        }
        UseSetup(read.Value!);
    }

    /// <summary>Searches and downloads with <paramref name="setup"/> from now on, in place of the one
    /// read from the user's NuGet.Config; with no feed kept, its first feed is the one searched.</summary>
    internal void UseSetup(NuGetSetup setup)
    {
        _setup = setup;
        if (FeedUrl.Length > 0)
            return;
        if (setup.Sources.Count == 0)
            return;
        FeedUrl = setup.Sources[0].Url;
    }

    /// <summary>Searches <paramref name="source"/> at once.</summary>
    public void PickFeed(NuGetSource source)
    {
        FeedUrl = source.Url;
        _typedUrl = source.Url;
        _typedAt = double.NegativeInfinity;
    }

    /// <summary>Closes the dialog, stopping the search and the download it started.</summary>
    public void Close()
    {
        IsOpen = false;
        StopSearch();
        StopDownload();
    }

    private void StopSearch()
    {
        if (_search == null)
            return;
        _search.Cancel();
        _search = null;
        _searched = null;
    }

    private void StopDownload()
    {
        _download?.Cancel();
        _download = null;
    }

    /// <summary>True once after <see cref="Open"/>: the query box should take focus.</summary>
    public bool TakeQueryFocus()
    {
        var focus = _focusQuery;
        _focusQuery = false;
        return focus;
    }

    public void ShowPreviousPage()
    {
        if (!HasPreviousPage)
            return;
        Page--;
    }

    public void ShowNextPage()
    {
        if (!HasNextPage)
            return;
        Page++;
    }

    /// <summary>Searches again once the typed feed or query settled, or at once when another page is
    /// asked for or whether prereleases count changed; a new search starts on its first page. Returns
    /// the seconds left before what was typed settles, or 0.</summary>
    public double Update(double now)
    {
        NoteTyping(now);
        if (_setup == null)
            return 0;
        var source = NuGetSource.FromUrl(_typedUrl);
        if (source == null)
        {
            DropResults();
            return 0;
        }
        var wanted = new FeedSearch(_setup.FeedAt(source), _typedQuery, IncludePrerelease, Page);
        if (IsNewSearch(wanted))
        {
            Page = 0;
            wanted = wanted with { Page = 0 };
        }
        if (wanted == _searched)
            return 0;
        var remaining = DebounceSeconds - (now - _typedAt);
        if (remaining > 0 && IsOnlyTypingChanged(wanted))
            return remaining;
        StartSearch(wanted);
        return 0;
    }

    private void NoteTyping(double now)
    {
        var typed = Query != _typedQuery || FeedUrl != _typedUrl;
        if (!typed)
            return;
        _typedQuery = Query;
        _typedUrl = FeedUrl;
        _typedAt = now;
    }

    /// <summary>Clears what was found once the feed typed is no URL, so nothing shows from another feed.</summary>
    private void DropResults()
    {
        StopSearch();
        _searched = null;
        _resultsFeed = null;
        _results.Clear();
        Selection.Clear();
        SearchError = null;
        Page = 0;
        PageCount = 1;
        TotalHits = 0;
    }

    /// <summary>Whether <paramref name="wanted"/> asks for other packages than the last search, not
    /// just another page of them.</summary>
    private bool IsNewSearch(FeedSearch wanted)
    {
        if (_searched == null)
            return false;
        if (wanted.Feed != _searched.Feed)
            return true;
        if (wanted.Query != _searched.Query)
            return true;
        return wanted.IncludePrerelease != _searched.IncludePrerelease;
    }

    private bool IsOnlyTypingChanged(FeedSearch wanted)
    {
        if (_searched == null)
            return false;
        if (wanted.IncludePrerelease != _searched.IncludePrerelease)
            return false;
        return wanted.Page == _searched.Page;
    }

    private async void StartSearch(FeedSearch wanted)
    {
        StopSearch();
        _searched = wanted;
        KeepSearched(wanted);
        var run = new CancellationTokenSource();
        _search = run;
        var found = await Background.Wait(wanted.Feed.SearchAsync(wanted.Query, wanted.IncludePrerelease, wanted.Page, run.Token));
        if (_search != run)
            return;
        _search = null;
        ShowFound(wanted, found);
    }

    /// <summary>Has the settings keep the feed searched and whether prereleases count, once either changed.</summary>
    private void KeepSearched(FeedSearch searched)
    {
        var kept = new SavedNuGetSearch(searched.Feed.Source.Url, searched.IncludePrerelease);
        if (kept == _kept)
            return;
        _kept = kept;
        SearchChanges++;
    }

    private void ShowFound(FeedSearch searched, Outcome<FeedPage> found)
    {
        _results.Clear();
        Selection.Clear();
        if (found.Error != null)
        {
            ReportSearchFailure(searched, found.Error);
            return;
        }
        SearchError = null;
        _resultsFeed = searched.Feed;
        _results.AddRange(found.Value!.Packages);
        TotalHits = found.Value.TotalHits;
        PageCount = NuGetFeed.CountPages(TotalHits);
        if (_results.Count > 0)
            Selection.Select(_results[0], _results);
    }

    private void ReportSearchFailure(FeedSearch searched, Exception error)
    {
        SearchError = error.Message;
        _reportError(_strings().ErrorSearchNuget(searched.Feed.Source.Name), error.ToString());
    }

    /// <summary>Offers the versions of the package selected, picking its latest, once another is selected.</summary>
    public void FollowSelection() => OfferVersionsOf(Selected);

    /// <summary>Offers the versions of <paramref name="package"/>, picking its latest, unless they are
    /// on offer already. The selected row offers its versions as it draws, before the list hands its
    /// selection back.</summary>
    public void OfferVersionsOf(FeedPackage? package)
    {
        if (package == _versionsOf)
            return;
        _versionsOf = package;
        if (package == null)
        {
            Version = "";
            Versions = [];
            return;
        }
        Version = package.Version;
        Versions = package.Versions.ToArray();
    }

    /// <summary>Fetches <see cref="Version"/> of the selected package, from the feed it was found in,
    /// into the cache, in the background, for <see cref="TakeDownloaded"/> to hand over.</summary>
    public async void Download()
    {
        if (!CanOpen)
            return;
        var package = Selected!;
        var version = Version;
        var run = new CancellationTokenSource();
        _download = run;
        DownloadingName = $"{package.Id} {version}";
        var fetched = await Background.Wait(_setup!.Cache.FetchAsync(_resultsFeed!, package.Id, version, run.Token));
        if (_download != run)
            return;
        _download = null;
        if (fetched.Error != null)
        {
            _reportError(_strings().ErrorDownloadPackage(DownloadingName), fetched.Error.ToString());
            return;
        }
        _downloaded = fetched.Value;
    }

    /// <summary>The package file the last download fetched, once; null until it is in.</summary>
    public string? TakeDownloaded()
    {
        var downloaded = _downloaded;
        _downloaded = null;
        return downloaded;
    }

    /// <summary>What a search asked of which feed.</summary>
    private sealed record FeedSearch(NuGetFeed Feed, string Query, bool IncludePrerelease, int Page);
}

/// <summary>What the settings keep of the "open from NuGet" dialog: the feed last searched and
/// whether prereleases counted.</summary>
public sealed record SavedNuGetSearch(string FeedUrl, bool IncludePrerelease);

using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>
/// The "open from NuGet" dialog: the feeds of the user's NuGet.Config, read the first time it opens
/// and searched once the query settles, and the package picked, fetched at the version picked into
/// the package cache for the view to open.
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
    private string _typed = "";
    private double _typedAt;
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
    public bool IncludePrerelease;

    /// <summary>The feed searched; null until the setup is read.</summary>
    public NuGetFeed? Feed;

    /// <summary>The version of the selected package to open.</summary>
    public string Version = "";

    public ItemSelection<FeedPackage> Selection { get; } = new();

    public FeedPackage? Selected => Selection.Item;

    public IReadOnlyList<FeedPackage> Results => _results;

    /// <summary>The feeds to pick from, once the setup is read.</summary>
    public NuGetFeed[] Feeds { get; private set; } = [];

    /// <summary>The names of <see cref="Feeds"/>, in their order.</summary>
    public string[] FeedNames { get; private set; } = [];

    /// <summary>The versions of the selected package, newest first.</summary>
    public string[] Versions { get; private set; } = [];

    public bool IsSearching => _search != null;

    /// <summary>Why the last search failed, or null.</summary>
    public string? SearchError { get; private set; }

    public bool IsDownloading => _download != null;

    /// <summary>The package and version being downloaded, while <see cref="IsDownloading"/>.</summary>
    public string DownloadingName { get; private set; } = "";

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
    /// read from the user's NuGet.Config.</summary>
    internal void UseSetup(NuGetSetup setup)
    {
        _setup = setup;
        Feeds = setup.Feeds.ToArray();
        FeedNames = Feeds
            .Select(f => f.Source.Name)
            .ToArray();
        Feed = Feeds[0];
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

    /// <summary>Searches again once the typed query settled, or at once when the feed or whether
    /// prereleases count changed. Returns the seconds left before a typed query settles, or 0.</summary>
    public double Update(double now)
    {
        NoteTyping(now);
        if (Feed == null)
            return 0;
        var wanted = new FeedSearch(Feed, _typed, IncludePrerelease);
        if (wanted == _searched)
            return 0;
        var remaining = DebounceSeconds - (now - _typedAt);
        if (remaining > 0 && IsOnlyQueryChanged(wanted))
            return remaining;
        StartSearch(wanted);
        return 0;
    }

    private void NoteTyping(double now)
    {
        if (Query == _typed)
            return;
        _typed = Query;
        _typedAt = now;
    }

    private bool IsOnlyQueryChanged(FeedSearch wanted)
    {
        if (_searched == null)
            return false;
        if (wanted.Feed != _searched.Feed)
            return false;
        return wanted.IncludePrerelease == _searched.IncludePrerelease;
    }

    private async void StartSearch(FeedSearch wanted)
    {
        StopSearch();
        _searched = wanted;
        var run = new CancellationTokenSource();
        _search = run;
        var found = await Background.Wait(wanted.Feed.SearchAsync(wanted.Query, wanted.IncludePrerelease, run.Token));
        if (_search != run)
            return;
        _search = null;
        ShowFound(wanted, found);
    }

    private void ShowFound(FeedSearch searched, Outcome<IReadOnlyList<FeedPackage>> found)
    {
        _results.Clear();
        Selection.Clear();
        if (found.Error != null)
        {
            ReportSearchFailure(searched, found.Error);
            return;
        }
        SearchError = null;
        _results.AddRange(found.Value!);
        if (_results.Count > 0)
            Selection.Select(_results[0], _results);
    }

    private void ReportSearchFailure(FeedSearch searched, Exception error)
    {
        SearchError = error.Message;
        _reportError(_strings().ErrorSearchNuget(searched.Feed.Source.Name), error.ToString());
    }

    /// <summary>Offers the versions of the package selected, picking its latest, once another is selected.</summary>
    public void FollowSelection()
    {
        if (Selected == _versionsOf)
            return;
        _versionsOf = Selected;
        if (Selected == null)
        {
            Version = "";
            Versions = [];
            return;
        }
        Version = Selected.Version;
        Versions = Selected.Versions.ToArray();
    }

    /// <summary>Fetches <see cref="Version"/> of the selected package into the cache, in the
    /// background, for <see cref="TakeDownloaded"/> to hand over.</summary>
    public async void Download()
    {
        if (!CanOpen)
            return;
        var package = Selected!;
        var version = Version;
        var run = new CancellationTokenSource();
        _download = run;
        DownloadingName = $"{package.Id} {version}";
        var fetched = await Background.Wait(_setup!.Cache.FetchAsync(Feed!, package.Id, version, run.Token));
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
    private sealed record FeedSearch(NuGetFeed Feed, string Query, bool IncludePrerelease);
}

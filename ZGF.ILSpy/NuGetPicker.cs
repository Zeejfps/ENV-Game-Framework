using System.Diagnostics;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Resources;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>
/// The "open from NuGet" dialog: the feed typed or picked from the user's NuGet.Config, read the
/// first time it opens, searched a page at a time once the query settles; the packages found, with
/// their icons; the details of the version picked of the package selected; and that version, fetched
/// into the package cache for the view to open.
/// </summary>
public sealed class NuGetPicker
{
    private const double DebounceSeconds = 0.3;
    private const string IconImagePrefix = "nuget-icon-";

    /// <summary>How many pixels across a package's icon is fetched at, enough for its row on a screen
    /// of twice the usual density.</summary>
    public const int IconPixels = 64;

    private readonly Func<Strings> _strings;
    private readonly Action<AppProblem> _reportError;
    private readonly Dictionary<string, PackageIcon> _icons = new();
    private readonly Dictionary<DetailsKey, VersionDetails> _details = new();
    private CancellationTokenSource _fetches = new();
    private int _iconCount;
    private SetupState _setup = new SetupState.Unread();
    private SearchRun _search = new SearchRun.None();
    private ShownPage? _shown;
    private DownloadState _download = new DownloadState.None();
    private int _requestedPage;
    private bool _isSettling;
    private string _typedQuery = "";
    private string _typedUrl = "";
    private double _typedAt;
    private SavedNuGetSearch? _kept;
    private FeedPackage? _versionsOf;
    private bool _focusQuery;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="reportError">Tells the user a search or a download failed.</param>
    public NuGetPicker(Func<Strings> strings, Action<AppProblem> reportError)
    {
        _strings = strings;
        _reportError = reportError;
    }

    public bool IsOpen;

    /// <summary>Where the dialog's window is on the screen.</summary>
    public DialogWindow Window { get; } = new();

    public string Query = "";

    /// <summary>The URL of the service index of the feed to search, typed or picked from <see cref="Sources"/>.</summary>
    public string FeedUrl = "";

    public bool IncludePrerelease;

    /// <summary>The version of the selected package to open; null while none is selected.</summary>
    public PackageVersion? Version;

    public ItemSelection<FeedPackage> Selection { get; } = new();

    public FeedPackage? Selected => Selection.Item;

    /// <summary>The packages on the page shown.</summary>
    public IReadOnlyList<FeedPackage> Results
    {
        get
        {
            if (_shown == null)
                return [];
            return _shown.Found.Packages;
        }
    }

    /// <summary>The page shown, from 0.</summary>
    public int Page
    {
        get
        {
            if (_shown == null)
                return 0;
            return _shown.Search.Page;
        }
    }

    /// <summary>The page asked for, from 0: the one shown, or the one loading in its place.</summary>
    public int RequestedPage => _requestedPage;

    /// <summary>Whether another page of the search shown is loading in its place.</summary>
    public bool IsLoadingPage => IsSearching && _requestedPage != Page;

    /// <summary>How many pages the search shown has, at least one.</summary>
    public int PageCount => NuGetFeed.CountPages(TotalHits);

    /// <summary>How many packages the search shown matched in all.</summary>
    public int TotalHits
    {
        get
        {
            if (_shown == null)
                return 0;
            return _shown.Found.TotalHits;
        }
    }

    /// <summary>How many of <see cref="TotalHits"/> the pages reach.</summary>
    public int BrowsableHits => Math.Min(TotalHits, NuGetFeed.MaxSkip + NuGetFeed.PageSize);

    public bool HasPreviousPage => Page > 0;

    public bool HasNextPage => Page + 1 < PageCount;

    /// <summary>The feeds the user's NuGet.Config lists, once the setup is read.</summary>
    public IReadOnlyList<NuGetSource> Sources
    {
        get
        {
            var setup = ReadySetup();
            if (setup == null)
                return [];
            return setup.Sources;
        }
    }

    public bool IsReadingSetup => _setup is not SetupState.Ready;

    /// <summary>Whether <see cref="FeedUrl"/> is a URL a feed could be at.</summary>
    public bool HasFeedUrl => NuGetSource.FromUrl(FeedUrl) != null;

    /// <summary>The query the packages shown were found by.</summary>
    public string ShownQuery
    {
        get
        {
            if (_shown == null)
                return "";
            return _shown.Search.Query;
        }
    }

    /// <summary>The versions of the selected package, newest first.</summary>
    public PackageVersion[] Versions { get; private set; } = [];

    /// <summary>How <see cref="Versions"/> are written, in their order.</summary>
    public string[] VersionTexts { get; private set; } = [];

    public bool IsSearching => _search is SearchRun.Running;

    /// <summary>Why the last search failed, or null.</summary>
    public string? SearchError
    {
        get
        {
            if (_search is not SearchRun.Failed failed)
                return null;
            return failed.Error;
        }
    }

    public bool IsDownloading => _download is DownloadState.Running;

    /// <summary>The package and version being downloaded; empty while none is.</summary>
    public string DownloadingName
    {
        get
        {
            if (_download is not DownloadState.Running running)
                return "";
            return running.Name;
        }
    }

    /// <summary>How many times the feed searched or whether prereleases count changed, for the
    /// settings to keep <see cref="Save"/> once it did.</summary>
    public int SearchChanges { get; private set; }

    /// <summary>Whether the packages shown are what the feed, query and prerelease choice now ask for:
    /// not while a changed query settles or another search runs. Another page loading over them, or
    /// failing to, leaves them so.</summary>
    public bool ShowsWhatWasAsked
    {
        get
        {
            if (_isSettling)
                return false;
            var searched = LastSearched();
            if (searched == null)
                return true;
            return IsSamePackages(searched, ShownSearch());
        }
    }

    /// <summary>Whether the selected package can be opened: it is one of what was asked for, a version
    /// is picked, and no download runs.</summary>
    public bool CanOpen
    {
        get
        {
            if (Selected == null)
                return false;
            if (IsDownloading)
                return false;
            if (!ShowsWhatWasAsked)
                return false;
            return Version != null;
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
        if (_setup is SetupState.Unread)
            ReadSetup();
    }

    /// <summary>Reads the user's NuGet.Config in the background; one that cannot be read at all is
    /// reported, and nuget.org searched in its place.</summary>
    private async void ReadSetup()
    {
        _setup = new SetupState.Reading();
        var read = await Background.Run(NuGetSetup.ReadUser);
        switch (read)
        {
            case Outcome<NuGetSetup>.Ok ok:
                UseSetup(ok.Value);
                break;
            case Outcome<NuGetSetup>.Failed failed:
                _reportError(new AppProblem.NuGetConfigUnreadable(failed.Error));
                UseSetup(NuGetSetup.CreateDefault());
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>Searches and downloads with <paramref name="setup"/> from now on, in place of the one
    /// read from the user's NuGet.Config; with no feed kept, its first feed is the one searched.</summary>
    internal void UseSetup(NuGetSetup setup)
    {
        _setup = new SetupState.Ready(setup);
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

    /// <summary>Closes the dialog, stopping the search, the download and the fetches it started.
    /// Returns the images of the icons shown, for the screen to free.</summary>
    public List<string> Close()
    {
        IsOpen = false;
        StopSearch();
        StopDownload();
        _fetches.Cancel();
        _fetches = new CancellationTokenSource();
        _details.Clear();
        return DropIcons();
    }

    private NuGetSetup? ReadySetup()
    {
        if (_setup is not SetupState.Ready ready)
            return null;
        return ready.Setup;
    }

    private void StopSearch()
    {
        if (_search is not SearchRun.Running running)
            return;
        running.Cancel.Cancel();
        _search = new SearchRun.None();
    }

    private void StopDownload()
    {
        if (_download is not DownloadState.Running running)
            return;
        running.Cancel.Cancel();
        _download = new DownloadState.None();
    }

    private List<string> DropIcons()
    {
        var images = _icons.Values
            .Select(i => i.ImageId)
            .ToList();
        _icons.Clear();
        return images;
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
        _requestedPage = Page - 1;
    }

    public void ShowNextPage()
    {
        if (!HasNextPage)
            return;
        _requestedPage = Page + 1;
    }

    /// <summary>Searches again for what the last search asked, after it failed.</summary>
    public void Retry()
    {
        StopSearch();
        _search = new SearchRun.None();
    }

    public void ClearQuery()
    {
        Query = "";
    }

    /// <summary>Opens the selected package at <see cref="Version"/>, unless the query typed is still to
    /// be searched: then it is searched at once instead, so Enter never opens what an older query found.</summary>
    public void Submit()
    {
        if (_isSettling)
        {
            SearchNow();
            return;
        }
        if (IsSearching)
            return;
        Download();
    }

    /// <summary>Searches what was typed with the next update, without waiting for it to settle.</summary>
    public void SearchNow()
    {
        _typedAt = double.NegativeInfinity;
    }

    /// <summary>Searches again once the typed feed or query settled, or at once when another page is
    /// asked for or whether prereleases count changed; a new search starts on its first page. Returns
    /// the seconds left before what was typed settles, or 0.</summary>
    public double Update(double now)
    {
        NoteTyping(now);
        _isSettling = false;
        var setup = ReadySetup();
        if (setup == null)
            return 0;
        var source = NuGetSource.FromUrl(_typedUrl);
        if (source == null)
        {
            DropResults();
            return 0;
        }
        var wanted = new FeedSearch(setup.FeedAt(source), _typedQuery, IncludePrerelease, _requestedPage);
        if (IsNewSearch(wanted))
        {
            _requestedPage = 0;
            wanted = wanted with { Page = 0 };
        }
        if (wanted == LastSearched())
            return 0;
        var remaining = DebounceSeconds - (now - _typedAt);
        if (remaining > 0 && IsOnlyTypingChanged(wanted))
        {
            _isSettling = true;
            return remaining;
        }
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
        _search = new SearchRun.None();
        ClearShown();
        _requestedPage = 0;
    }

    private void ClearShown()
    {
        _shown = null;
        Selection.Clear();
    }

    /// <summary>The search running or last run; null before any, or once it was stopped or dropped.</summary>
    private FeedSearch? LastSearched() =>
        _search switch
        {
            SearchRun.None => null,
            SearchRun.Running running => running.Search,
            SearchRun.Found found => found.Search,
            SearchRun.Failed failed => failed.Search,
            _ => throw new UnreachableException(),
        };

    private FeedSearch? ShownSearch()
    {
        if (_shown == null)
            return null;
        return _shown.Search;
    }

    /// <summary>The search the next one is weighed against: the one running or last run, else the one shown.</summary>
    private FeedSearch? LastAsked()
    {
        var searched = LastSearched();
        if (searched != null)
            return searched;
        return ShownSearch();
    }

    /// <summary>Whether <paramref name="wanted"/> asks for other packages than the last search, not
    /// just another page of them.</summary>
    private bool IsNewSearch(FeedSearch wanted)
    {
        var last = LastAsked();
        if (last == null)
            return false;
        return !IsSamePackages(wanted, last);
    }

    private static bool IsSamePackages(FeedSearch a, FeedSearch? b)
    {
        if (b == null)
            return false;
        if (a.Feed != b.Feed)
            return false;
        if (a.Query != b.Query)
            return false;
        return a.IncludePrerelease == b.IncludePrerelease;
    }

    private bool IsOnlyTypingChanged(FeedSearch wanted)
    {
        var last = LastAsked();
        if (last == null)
            return false;
        if (wanted.IncludePrerelease != last.IncludePrerelease)
            return false;
        return wanted.Page == last.Page;
    }

    private async void StartSearch(FeedSearch wanted)
    {
        StopSearch();
        KeepSearched(wanted);
        var running = new SearchRun.Running(wanted, new CancellationTokenSource());
        _search = running;
        var found = await Background.Wait(wanted.Feed.SearchAsync(wanted.Query, wanted.IncludePrerelease, wanted.Page, running.Cancel.Token));
        if (!ReferenceEquals(_search, running))
            return;
        _search = ShowFound(wanted, found);
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

    /// <summary>Shows the page found in place of the one shown. A failed search says why, and keeps
    /// the page shown when it only asked for another page of it.</summary>
    private SearchRun ShowFound(FeedSearch searched, Outcome<FeedPage> found)
    {
        switch (found)
        {
            case Outcome<FeedPage>.Ok ok:
                ShowFound(searched, ok.Value);
                return new SearchRun.Found(searched);
            case Outcome<FeedPage>.Failed failed:
                ReportSearchFailure(searched, failed.Error);
                return new SearchRun.Failed(searched, failed.Error.Message);
            default:
                throw new UnreachableException();
        }
    }

    private void ShowFound(FeedSearch searched, FeedPage page)
    {
        _shown = new ShownPage(searched, page);
        Selection.Clear();
        if (page.Packages.Count > 0)
            Selection.Select(page.Packages[0], page.Packages);
    }

    private void ReportSearchFailure(FeedSearch searched, Exception error)
    {
        if (!IsSamePackages(searched, ShownSearch()))
            ClearShown();
        _reportError(new AppProblem.NuGetSearchFailed(searched.Feed.Source.Name, error));
    }

    /// <summary>The icon of <paramref name="package"/>, fetched in the background the first time it is
    /// asked for; null when the package gives none.</summary>
    public PackageIcon? IconOf(FeedPackage package)
    {
        if (package.IconUrl.Length == 0)
            return null;
        var setup = ReadySetup();
        if (setup == null)
            return null;
        if (_icons.TryGetValue(package.IconUrl, out var icon))
            return icon;
        _iconCount++;
        icon = new PackageIcon(IconImagePrefix + _iconCount);
        _icons.Add(package.IconUrl, icon);
        FetchIcon(setup, package.IconUrl, icon);
        return icon;
    }

    /// <summary>Gives <paramref name="icon"/> its pixels once they are in; one that cannot be fetched
    /// keeps none, so its row keeps the stand-in.</summary>
    private async void FetchIcon(NuGetSetup setup, string url, PackageIcon icon)
    {
        var token = _fetches.Token;
        var fetched = await Background.Wait(setup.Icons.FetchAsync(url, IconPixels, token));
        if (token.IsCancellationRequested)
            return;
        if (fetched is Outcome<ResourceImage>.Ok ok)
            icon.Show(ok.Value);
    }

    /// <summary>What the feed says of <paramref name="version"/> of <paramref name="package"/>, read
    /// in the background the first time it is asked for; null while no page of packages is shown or
    /// the package lists no such version.</summary>
    public VersionDetails? DetailsOf(FeedPackage package, PackageVersion version)
    {
        if (_shown == null)
            return null;
        var listed = FindVersion(package, version);
        if (listed == null)
            return null;
        var feed = _shown.Search.Feed;
        var key = new DetailsKey(feed.Source.Url, package.Id, version);
        if (_details.TryGetValue(key, out var details))
            return details;
        var loading = new VersionDetails.Loading();
        _details.Add(key, loading);
        ReadDetails(key, feed, listed, loading);
        return loading;
    }

    /// <summary>Reads the details of <paramref name="version"/> of <paramref name="package"/> again, after they failed.</summary>
    public void RetryDetails(FeedPackage package, PackageVersion version)
    {
        if (_shown == null)
            return;
        _details.Remove(new DetailsKey(_shown.Search.Feed.Source.Url, package.Id, version));
    }

    private static FeedVersion? FindVersion(FeedPackage package, PackageVersion version) =>
        package.Versions.FirstOrDefault(v => v.Version == version);

    /// <summary>Puts the details read in place of <paramref name="loading"/>, unless they were dropped
    /// or asked for again meanwhile.</summary>
    private async void ReadDetails(DetailsKey key, NuGetFeed feed, FeedVersion version, VersionDetails.Loading loading)
    {
        var token = _fetches.Token;
        var read = await Background.Wait(feed.ReadDetailsAsync(key.Id, version, token));
        if (token.IsCancellationRequested)
            return;
        if (!_details.TryGetValue(key, out var current) || !ReferenceEquals(current, loading))
            return;
        _details[key] = DetailsRead(read);
    }

    private static VersionDetails DetailsRead(Outcome<PackageVersionDetails> read) =>
        read switch
        {
            Outcome<PackageVersionDetails>.Ok ok => new VersionDetails.Read(ok.Value),
            Outcome<PackageVersionDetails>.Failed failed => new VersionDetails.Failed(failed.Error.Message),
            _ => throw new UnreachableException(),
        };

    /// <summary>Offers the versions of the package selected, picking its latest, once another is selected.</summary>
    public void FollowSelection()
    {
        var package = Selected;
        if (package == _versionsOf)
            return;
        _versionsOf = package;
        if (package == null)
        {
            Version = null;
            Versions = [];
            VersionTexts = [];
            return;
        }
        Version = package.Version;
        Versions = package.Versions
            .Select(v => v.Version)
            .ToArray();
        VersionTexts = Versions
            .Select(v => v.Text)
            .ToArray();
    }

    /// <summary>How many times <see cref="Version"/> of the selected package was downloaded; 0 when the feed does not say.</summary>
    public long VersionDownloads
    {
        get
        {
            if (Selected == null || Version == null)
                return 0;
            var listed = FindVersion(Selected, Version);
            if (listed == null)
                return 0;
            return listed.Downloads;
        }
    }

    /// <summary>Fetches <see cref="Version"/> of the selected package, from the feed it was found in,
    /// into the cache, in the background, for <see cref="TakeDownloaded"/> to hand over.</summary>
    public async void Download()
    {
        var version = Version;
        var package = Selected;
        var setup = ReadySetup();
        var shown = _shown;
        if (!CanOpen || version == null || package == null || setup == null || shown == null)
            return;
        var name = $"{package.Id} {version}";
        var running = new DownloadState.Running(name, new CancellationTokenSource());
        _download = running;
        var fetched = await Background.Wait(setup.Cache.FetchAsync(shown.Search.Feed, package.Id, version, running.Cancel.Token));
        if (!ReferenceEquals(_download, running))
            return;
        _download = Downloaded(name, fetched);
    }

    private DownloadState Downloaded(string name, Outcome<string> fetched)
    {
        switch (fetched)
        {
            case Outcome<string>.Ok ok:
                return new DownloadState.Done(ok.Value);
            case Outcome<string>.Failed failed:
                _reportError(new AppProblem.PackageNotDownloaded(name, failed.Error));
                return new DownloadState.None();
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>The package file the last download fetched, once; null until it is in.</summary>
    public string? TakeDownloaded()
    {
        if (_download is not DownloadState.Done done)
            return null;
        _download = new DownloadState.None();
        return done.Path;
    }

    /// <summary>What a search asked of which feed.</summary>
    private sealed record FeedSearch(NuGetFeed Feed, string Query, bool IncludePrerelease, int Page);

    /// <summary>The page of packages shown, and the search that found it.</summary>
    private sealed record ShownPage(FeedSearch Search, FeedPage Found);

    /// <summary>Whether the user's NuGet.Config is read yet.</summary>
    private abstract record SetupState
    {
        private SetupState()
        {
        }

        public sealed record Unread : SetupState;

        public sealed record Reading : SetupState;

        public sealed record Ready(NuGetSetup Setup) : SetupState;
    }

    /// <summary>The last search: none since the dialog was made or the search was dropped, one
    /// running, or one that found a page or failed to.</summary>
    private abstract record SearchRun
    {
        private SearchRun()
        {
        }

        public sealed record None : SearchRun;

        public sealed record Running(FeedSearch Search, CancellationTokenSource Cancel) : SearchRun;

        public sealed record Found(FeedSearch Search) : SearchRun;

        public sealed record Failed(FeedSearch Search, string Error) : SearchRun;
    }

    /// <summary>A download: none, one running, or the package file one fetched, until it is taken.</summary>
    private abstract record DownloadState
    {
        private DownloadState()
        {
        }

        public sealed record None : DownloadState;

        public sealed record Running(string Name, CancellationTokenSource Cancel) : DownloadState;

        public sealed record Done(string Path) : DownloadState;
    }

    /// <summary>Which version of which package of which feed details are of.</summary>
    private readonly record struct DetailsKey(string Feed, PackageId Id, PackageVersion Version);
}

/// <summary>A package's icon: the image the screen draws it as, and its pixels, null until they are
/// in; with whether it is dark marks on a clear background, which a dark theme would hide.</summary>
public sealed class PackageIcon(string imageId)
{
    public string ImageId { get; } = imageId;

    public ResourceImage? Pixels { get; private set; }

    public bool IsDarkOnClear { get; private set; }

    public void Show(ResourceImage? pixels)
    {
        if (pixels == null)
            return;
        Pixels = pixels;
        IsDarkOnClear = pixels.IsDarkOnClear();
    }
}

/// <summary>What a feed says of one version of a package: loading, read, or why it could not be.</summary>
public abstract record VersionDetails
{
    private VersionDetails()
    {
    }

    public sealed record Loading : VersionDetails;

    public sealed record Read(PackageVersionDetails Details) : VersionDetails;

    public sealed record Failed(string Error) : VersionDetails;
}

/// <summary>What the settings keep of the "open from NuGet" dialog: the feed last searched and
/// whether prereleases counted.</summary>
public sealed record SavedNuGetSearch(string FeedUrl, bool IncludePrerelease);

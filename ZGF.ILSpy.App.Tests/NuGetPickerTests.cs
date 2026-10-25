using System.IO.Compression;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class NuGetPickerTests : IDisposable
{
    private const string FixturesPackage = """
        { "totalHits": 1, "data": [
            { "id": "Fixtures", "version": "2.0.0", "description": "The fixtures.", "authors": ["Zee"],
              "versions": [ { "version": "1.0.0" }, { "version": "2.0.0" } ] } ] }
        """;

    private const string NoPackages = """{ "totalHits": 0, "data": [] }""";

    private const string ThreePagesOfPackages = """
        { "totalHits": 60, "data": [ { "id": "Fixtures", "version": "2.0.0" } ] }
        """;

    private const string OtherIndexUrl = "https://other.test/v3/index.json";

    private readonly TempDir _temp = new();
    private readonly FakeFeed _fake = new();
    private readonly List<string> _reported = new();

    public NuGetPickerTests()
    {
        _fake.AnswerSearches(FixturesPackage);
    }

    public void Dispose() => _temp.Dispose();

    private NuGetSetup Setup()
    {
        var cache = new PackageCache(_temp.PathOf("global"), _temp.PathOf("downloads"));
        return new NuGetSetup([_fake.Source], cache, _fake);
    }

    private NuGetPicker OpenPicker(PostedActions posted, SavedNuGetSearch? saved = null)
    {
        var picker = new NuGetPicker(static () => Strings.En, (title, _) => _reported.Add(title));
        picker.Restore(saved);
        picker.UseSetup(Setup());
        picker.Open();
        picker.Update(now: 10);
        Poll.Until(() => !picker.IsSearching, posted.Run);
        picker.FollowSelection();
        return picker;
    }

    private int SearchCount() => _fake.Requested.Count(r => r.StartsWith(FakeFeed.SearchUrl));

    [Fact]
    public void Opening_searches_the_first_feed_and_picks_the_latest_version_of_the_first_package()
    {
        var posted = PostedActions.OfThisTest();

        var picker = OpenPicker(posted);

        Assert.Equal("Fixtures", Assert.Single(picker.Results).Id);
        Assert.Equal("Fixtures", picker.Selected!.Id);
        Assert.Equal("2.0.0", picker.Version);
        Assert.Equal(["2.0.0", "1.0.0"], picker.Versions);
        Assert.True(picker.CanOpen);
    }

    [Fact]
    public void A_typed_query_is_searched_once_it_settles()
    {
        var posted = PostedActions.OfThisTest();
        var picker = OpenPicker(posted);

        picker.Query = "fix";
        var wait = picker.Update(now: 20);
        var searchedWhileTyping = SearchCount();
        picker.Update(now: 21);
        Poll.Until(() => !picker.IsSearching, posted.Run);

        Assert.True(wait > 0);
        Assert.Equal(1, searchedWhileTyping);
        Assert.Equal(2, SearchCount());
        Assert.Contains("q=fix", _fake.Requested.Last());
    }

    [Fact]
    public void Counting_prereleases_searches_again_at_once()
    {
        var posted = PostedActions.OfThisTest();
        var picker = OpenPicker(posted);

        picker.IncludePrerelease = true;
        var wait = picker.Update(now: 10);
        Poll.Until(() => !picker.IsSearching, posted.Run);

        Assert.Equal(0, wait);
        Assert.Contains("prerelease=true", _fake.Requested.Last());
    }

    [Fact]
    public void The_next_page_is_searched_at_once_and_a_new_query_starts_on_the_first()
    {
        var posted = PostedActions.OfThisTest();
        _fake.AnswerSearches(ThreePagesOfPackages);
        var picker = OpenPicker(posted);

        picker.ShowNextPage();
        var wait = picker.Update(now: 10);
        Poll.Until(() => !picker.IsSearching, posted.Run);
        var nextPage = _fake.Requested.Last();
        picker.Query = "fix";
        picker.Update(now: 20);
        picker.Update(now: 21);
        Poll.Until(() => !picker.IsSearching, posted.Run);

        Assert.Equal(0, wait);
        Assert.Contains($"skip={NuGetFeed.PageSize}&", nextPage);
        Assert.Equal(3, picker.PageCount);
        Assert.Equal(0, picker.Page);
        Assert.Contains("skip=0&", _fake.Requested.Last());
    }

    [Fact]
    public void A_typed_feed_url_is_searched_once_it_settles_and_kept()
    {
        var posted = PostedActions.OfThisTest();
        _fake.AnswerIndexAt(OtherIndexUrl);
        var picker = OpenPicker(posted);

        picker.FeedUrl = OtherIndexUrl;
        var wait = picker.Update(now: 20);
        picker.Update(now: 21);
        Poll.Until(() => !picker.IsSearching, posted.Run);

        Assert.True(wait > 0);
        Assert.Contains(OtherIndexUrl, _fake.Requested);
        Assert.Equal(new SavedNuGetSearch(OtherIndexUrl, false), picker.Save());
    }

    [Fact]
    public void A_feed_that_is_no_url_shows_nothing_and_is_not_searched()
    {
        var posted = PostedActions.OfThisTest();
        var picker = OpenPicker(posted);
        var requests = _fake.Requested.Count;

        picker.FeedUrl = "api.nuget";
        picker.Update(now: 20);
        picker.Update(now: 21);

        Assert.False(picker.HasFeedUrl);
        Assert.Empty(picker.Results);
        Assert.Null(picker.Selected);
        Assert.Equal(requests, _fake.Requested.Count);
    }

    [Fact]
    public void The_kept_feed_and_prerelease_choice_are_searched_in_place_of_the_config_s_first()
    {
        var posted = PostedActions.OfThisTest();
        _fake.AnswerIndexAt(OtherIndexUrl);

        var picker = OpenPicker(posted, new SavedNuGetSearch(OtherIndexUrl, IncludePrerelease: true));

        Assert.Equal(OtherIndexUrl, picker.FeedUrl);
        Assert.DoesNotContain(FakeFeed.IndexUrl, _fake.Requested);
        Assert.Contains("prerelease=true", _fake.Requested.Last());
    }

    [Fact]
    public void A_search_that_finds_nothing_leaves_nothing_to_open()
    {
        var posted = PostedActions.OfThisTest();
        _fake.AnswerSearches(NoPackages);

        var picker = OpenPicker(posted);

        Assert.Empty(picker.Results);
        Assert.Null(picker.SearchError);
        Assert.False(picker.CanOpen);
    }

    [Fact]
    public void A_failed_search_says_why_and_reports_it()
    {
        var posted = PostedActions.OfThisTest();
        _fake.AnswerSearches("not json");

        var picker = OpenPicker(posted);

        Assert.NotNull(picker.SearchError);
        Assert.Equal(["Cannot search test"], _reported);
    }

    [Fact]
    public void Opening_the_picked_version_downloads_it_and_hands_its_file_over_once()
    {
        var posted = PostedActions.OfThisTest();
        _fake.Answer(FakeFeed.PackagesUrl + "fixtures/1.0.0/fixtures.1.0.0.nupkg", [1, 2, 3]);
        var picker = OpenPicker(posted);

        picker.Version = "1.0.0";
        picker.Download();
        Poll.Until(() => !picker.IsDownloading, posted.Run);

        var downloaded = picker.TakeDownloaded();
        Assert.Equal(_temp.PathOf(Path.Combine("downloads", "fixtures", "1.0.0", "fixtures.1.0.0.nupkg")), downloaded);
        Assert.Null(picker.TakeDownloaded());
    }

    [Fact]
    public void A_failed_download_hands_nothing_over_and_reports_it()
    {
        var posted = PostedActions.OfThisTest();
        var picker = OpenPicker(posted);

        picker.Download();
        Poll.Until(() => !picker.IsDownloading, posted.Run);

        Assert.Null(picker.TakeDownloaded());
        Assert.Equal(["Cannot download Fixtures 2.0.0"], _reported);
    }

    [Fact]
    public void Closing_while_downloading_drops_the_download()
    {
        var posted = PostedActions.OfThisTest();
        _fake.Answer(FakeFeed.PackagesUrl + "fixtures/2.0.0/fixtures.2.0.0.nupkg", [1, 2, 3]);
        var picker = OpenPicker(posted);
        _fake.Holds = true;

        picker.Download();
        picker.Close();
        Poll.Until(() => _fake.Cancelled > 0, posted.Run);

        Assert.False(picker.IsDownloading);
        Assert.Null(picker.TakeDownloaded());
        Assert.Empty(_reported);
    }

    [Fact]
    public void Enter_in_the_dialog_opens_the_package_and_closes_it()
    {
        var app = new AppDriver();
        _fake.Answer(FakeFeed.PackagesUrl + "fixtures/2.0.0/fixtures.2.0.0.nupkg", File.ReadAllBytes(WritePackage()));
        var picker = app.Ui.NuGetPicker;
        picker.UseSetup(Setup());

        picker.Open();
        app.RunUntil(() => picker.Results.Count > 0);
        app.Frames.Key(KeyboardKey.Enter);
        app.RunUntil(() => app.AppState.Assemblies.Listed.Count > 0);
        app.Settle(2);

        Assert.False(picker.IsOpen);
        Assert.Equal(NodeKind.Package, Assert.Single(app.Ui.Tree.Roots).Kind);
        Assert.True(app.Frames.Drew("fixtures.2.0.0.nupkg"));
    }

    [Fact]
    public void The_selected_row_offers_its_versions_and_Open_downloads_the_one_picked()
    {
        var app = new AppDriver();
        _fake.Answer(FakeFeed.PackagesUrl + "fixtures/1.0.0/fixtures.1.0.0.nupkg", File.ReadAllBytes(WritePackage()));
        var picker = app.Ui.NuGetPicker;
        picker.UseSetup(Setup());

        picker.Open();
        app.RunUntil(() => picker.Results.Count > 0);
        app.Run(2);
        app.Click("2.0.0").Frame();
        app.ClickMenuItem("1.0.0").Frame();
        var picked = picker.Version;
        app.ClickLast("Open").Frame();
        app.RunUntil(() => app.AppState.Assemblies.Listed.Count > 0);
        app.Settle(2);

        Assert.Equal("1.0.0", picked);
        Assert.False(picker.IsOpen);
        Assert.True(app.Frames.Drew("fixtures.1.0.0.nupkg"));
    }

    [Fact]
    public void The_dialog_lists_what_was_found_with_the_selected_package_s_description()
    {
        var app = new AppDriver();
        var picker = app.Ui.NuGetPicker;
        picker.UseSetup(Setup());

        picker.Open();
        app.RunUntil(() => picker.Results.Count > 0);
        app.Run(2);

        Assert.True(app.Frames.Drew("Open from NuGet"));
        Assert.True(app.Frames.Drew("Fixtures"));
        Assert.True(app.Frames.Drew("The fixtures."));
        Assert.True(app.Frames.Drew("by Zee"));
    }

    [Fact]
    public void Next_in_the_dialog_shows_the_next_page_of_how_many()
    {
        var app = new AppDriver();
        _fake.AnswerSearches(ThreePagesOfPackages);
        var picker = app.Ui.NuGetPicker;
        picker.UseSetup(Setup());

        picker.Open();
        app.RunUntil(() => picker.Results.Count > 0);
        app.Run(2);
        var first = app.Frames.Drew("Page 1 of 3");
        app.Click("Next").Frame();
        app.RunUntil(() => !picker.IsSearching);
        app.Run(2);

        Assert.True(first);
        Assert.True(app.Frames.Drew("60 packages"));
        Assert.True(app.Frames.Drew("Page 2 of 3"));
    }

    private string WritePackage()
    {
        var path = _temp.Write("source.nupkg", []);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        var name = Path.GetFileName(AppStateDriver.FixturePath);
        archive.CreateEntryFromFile(AppStateDriver.FixturePath, $"lib/net10.0/{name}");
        return path;
    }
}

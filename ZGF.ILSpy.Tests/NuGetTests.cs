using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class NuGetFeedTests
{
    private const string TwoPackages = """
        { "totalHits": 2, "data": [
            { "id": "Newtonsoft.Json", "version": "13.0.3", "description": "Json.NET", "authors": ["James Newton-King"],
              "versions": [ { "version": "12.0.1" }, { "version": "13.0.3" } ] },
            { "id": "Tiny", "version": "1.0.0", "description": "Small", "authors": "Ann, Bo" } ] }
        """;

    [Fact]
    public async Task A_search_reads_each_package_with_its_versions_newest_first()
    {
        var fake = new FakeFeed();
        fake.AnswerSearches(TwoPackages);
        var feed = new NuGetFeed(fake.Source, fake);

        var found = await feed.SearchAsync("json", includePrerelease: false);

        Assert.Equal(["Newtonsoft.Json", "Tiny"], found.Select(p => p.Id));
        Assert.Equal(["13.0.3", "12.0.1"], found[0].Versions);
        Assert.Equal("James Newton-King", found[0].Authors);
        Assert.Equal("Ann, Bo", found[1].Authors);
        Assert.Equal(["1.0.0"], found[1].Versions);
    }

    [Fact]
    public async Task A_search_asks_for_the_query_and_whether_prereleases_count()
    {
        var fake = new FakeFeed();
        fake.AnswerSearches(TwoPackages);
        var feed = new NuGetFeed(fake.Source, fake);

        await feed.SearchAsync(" a&b ", includePrerelease: true);

        var search = fake.Requested.Last();
        Assert.StartsWith(FakeFeed.SearchUrl + "?q=a%26b&", search);
        Assert.Contains("prerelease=true", search);
    }

    [Fact]
    public async Task The_service_index_is_read_once()
    {
        var fake = new FakeFeed();
        fake.AnswerSearches(TwoPackages);
        var feed = new NuGetFeed(fake.Source, fake);

        await feed.SearchAsync("a", includePrerelease: false);
        await feed.SearchAsync("b", includePrerelease: false);

        Assert.Single(fake.Requested, FakeFeed.IndexUrl);
    }

    [Fact]
    public async Task A_feed_whose_index_names_no_search_cannot_be_searched()
    {
        var fake = new FakeFeed();
        fake.Answer(FakeFeed.IndexUrl, """{ "version": "3.0.0", "resources": [] }""");
        var feed = new NuGetFeed(fake.Source, fake);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => feed.SearchAsync("a", includePrerelease: false));

        Assert.Contains("SearchQueryService", error.Message);
    }

    [Fact]
    public async Task A_feed_that_answers_with_no_json_says_so()
    {
        var fake = new FakeFeed();
        fake.AnswerSearches("<html>maintenance</html>");
        var feed = new NuGetFeed(fake.Source, fake);

        await Assert.ThrowsAsync<InvalidDataException>(() => feed.SearchAsync("a", includePrerelease: false));
    }
}

public sealed class PackageCacheTests
{
    private static readonly byte[] PackageBytes = [1, 2, 3];

    [Fact]
    public async Task A_package_not_on_disk_is_downloaded_from_its_lower_case_url_and_kept()
    {
        using var temp = new TempDir();
        var fake = new FakeFeed();
        fake.Answer(FakeFeed.PackagesUrl + "tiny/1.0.0-beta/tiny.1.0.0-beta.nupkg", PackageBytes);
        var cache = new PackageCache(temp.PathOf("global"), temp.PathOf("downloads"));

        var path = await cache.FetchAsync(new NuGetFeed(fake.Source, fake), "Tiny", "1.0.0-Beta+build.5");

        Assert.Equal(temp.PathOf(Path.Combine("downloads", "tiny", "1.0.0-beta", "tiny.1.0.0-beta.nupkg")), path);
        Assert.Equal(PackageBytes, File.ReadAllBytes(path));
        Assert.Equal(path, cache.Find("Tiny", "1.0.0-beta"));
    }

    [Fact]
    public async Task A_package_a_restore_put_in_the_global_folder_is_opened_from_there()
    {
        using var temp = new TempDir();
        var restored = temp.Write(Path.Combine("global", "tiny", "1.0.0", "tiny.1.0.0.nupkg"), PackageBytes);
        var fake = new FakeFeed();
        var cache = new PackageCache(temp.PathOf("global"), temp.PathOf("downloads"));

        var path = await cache.FetchAsync(new NuGetFeed(fake.Source, fake), "Tiny", "1.0.0");

        Assert.Equal(restored, path);
        Assert.Empty(fake.Requested);
    }

    [Fact]
    public async Task A_package_the_feed_does_not_have_leaves_nothing_behind()
    {
        using var temp = new TempDir();
        var fake = new FakeFeed();
        var cache = new PackageCache(temp.PathOf("global"), temp.PathOf("downloads"));

        await Assert.ThrowsAsync<HttpRequestException>(() => cache.FetchAsync(new NuGetFeed(fake.Source, fake), "Gone", "1.0.0"));

        Assert.Null(cache.Find("Gone", "1.0.0"));
        Assert.False(File.Exists(temp.PathOf(Path.Combine("downloads", "gone", "1.0.0", "gone.1.0.0.nupkg.download"))));
    }
}

public sealed class NuGetConfigTests
{
    [Fact]
    public void The_enabled_v3_sources_are_listed_in_order()
    {
        using var temp = new TempDir();
        var path = temp.Write("NuGet.Config", """
            <configuration>
              <packageSources>
                <add key="old" value="https://old.test/v3/index.json" />
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
                <add key="v2" value="https://v2.test/api/v2" />
                <add key="local" value="C:\packages" />
                <add key="off" value="https://off.test/v3/index.json" />
                <add key="company" value="https://company.test/nuget/v3/index.json" />
              </packageSources>
              <disabledPackageSources>
                <add key="off" value="true" />
              </disabledPackageSources>
            </configuration>
            """);

        var config = NuGetConfig.Read(path);

        Assert.Equal(["nuget.org", "company"], config.Sources.Select(s => s.Name));
        Assert.Equal("https://company.test/nuget/v3/index.json", config.Sources[1].Url);
    }

    [Fact]
    public void A_missing_config_offers_nuget_org()
    {
        using var temp = new TempDir();

        var config = NuGetConfig.Read(temp.PathOf("NuGet.Config"));

        Assert.Equal([NuGetSource.NuGetOrg], config.Sources);
    }

    [Fact]
    public void A_config_that_is_no_xml_offers_nuget_org()
    {
        using var temp = new TempDir();
        var path = temp.Write("NuGet.Config", "not xml");

        var config = NuGetConfig.Read(path);

        Assert.Equal([NuGetSource.NuGetOrg], config.Sources);
    }

    [Fact]
    public void The_global_packages_folder_the_config_names_is_found_from_the_config_s_folder()
    {
        if (Environment.GetEnvironmentVariable("NUGET_PACKAGES") != null)
            return;
        using var temp = new TempDir();
        var path = temp.Write("NuGet.Config", """
            <configuration>
              <config>
                <add key="globalPackagesFolder" value="restored" />
              </config>
            </configuration>
            """);

        var config = NuGetConfig.Read(path);

        Assert.Equal(temp.PathOf("restored"), config.GlobalPackagesFolder);
    }
}

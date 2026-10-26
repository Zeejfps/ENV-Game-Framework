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

        var packages = found.Packages;
        Assert.Equal(["Newtonsoft.Json", "Tiny"], packages.Select(p => p.Id));
        Assert.Equal(["13.0.3", "12.0.1"], packages[0].Versions.Select(v => v.Version));
        Assert.Equal("James Newton-King", packages[0].Authors);
        Assert.Equal("Ann, Bo", packages[1].Authors);
        Assert.Equal(["1.0.0"], packages[1].Versions.Select(v => v.Version));
        Assert.Equal(2, found.TotalHits);
    }

    [Fact]
    public async Task A_search_reads_what_each_package_says_of_itself()
    {
        var fake = new FakeFeed();
        fake.AnswerSearches("""
            { "totalHits": 1, "data": [
                { "id": "Serilog", "version": "4.4.0", "totalDownloads": 3477571230, "verified": true,
                  "owners": ["serilog"], "tags": ["logging", "structured"], "projectUrl": "https://serilog.net/",
                  "vulnerabilities": [ { "advisoryUrl": "https://advisory.test", "severity": "2" } ],
                  "versions": [ { "version": "4.4.0", "downloads": 5, "@id": "https://feed.test/reg/serilog/4.4.0.json" } ] } ] }
            """);
        var feed = new NuGetFeed(fake.Source, fake);

        var package = (await feed.SearchAsync("serilog", includePrerelease: false)).Packages[0];

        Assert.Equal(3477571230, package.TotalDownloads);
        Assert.True(package.Verified);
        Assert.Equal("serilog", package.Owners);
        Assert.Equal(["logging", "structured"], package.Tags);
        Assert.Equal("https://serilog.net/", package.ProjectUrl);
        Assert.Equal(1, package.Vulnerabilities);
        Assert.Equal(new FeedVersion("4.4.0", 5, "https://feed.test/reg/serilog/4.4.0.json"), package.Versions[0]);
    }

    [Fact]
    public async Task A_version_s_details_are_read_from_its_catalog_entry()
    {
        var fake = new FakeFeed();
        fake.Answer("https://feed.test/reg/tiny/1.0.0.json", """
            { "published": "2024-05-01T10:00:00Z", "catalogEntry": "https://feed.test/catalog/tiny.1.0.0.json" }
            """);
        fake.Answer("https://feed.test/catalog/tiny.1.0.0.json", """
            { "licenseExpression": "MIT", "packageSize": 2048,
              "dependencyGroups": [ { "targetFramework": "net8.0", "dependencies": [ { "id": "Dep", "range": "[1.0.0, )" } ] },
                                    { "targetFramework": "netstandard2.0" } ],
              "deprecation": { "reasons": ["Legacy"], "message": "Old.", "alternatePackage": { "id": "Tiny2" } },
              "vulnerabilities": [ { "advisoryUrl": "https://advisory.test", "severity": "3" } ] }
            """);
        var feed = new NuGetFeed(fake.Source, fake);

        var details = await feed.ReadDetailsAsync("Tiny", new FeedVersion("1.0.0", 0, "https://feed.test/reg/tiny/1.0.0.json"));

        Assert.Equal(new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.Zero), details.Published);
        Assert.Equal("MIT", details.License);
        Assert.Equal(2048, details.PackageSize);
        Assert.Equal(["net8.0", "netstandard2.0"], details.DependencyGroups.Select(g => g.TargetFramework));
        Assert.Equal(new PackageDependency("Dep", "[1.0.0, )"), Assert.Single(details.DependencyGroups[0].Dependencies));
        Assert.Equal("Tiny2", details.Deprecation!.AlternatePackage);
        Assert.Equal(3, Assert.Single(details.Vulnerabilities).Severity);
    }

    [Fact]
    public async Task A_version_the_search_gave_no_address_for_is_found_through_the_registrations()
    {
        var fake = new FakeFeed();
        fake.Answer(FakeFeed.IndexUrl, """
            { "version": "3.0.0", "resources": [
                { "@id": "https://feed.test/search", "@type": "SearchQueryService" },
                { "@id": "https://feed.test/flat/", "@type": "PackageBaseAddress/3.0.0" },
                { "@id": "https://feed.test/reg/", "@type": "RegistrationsBaseUrl/3.6.0" } ] }
            """);
        fake.Answer("https://feed.test/reg/tiny/1.0.0-beta.json", """{ "catalogEntry": { "licenseUrl": "https://license.test" } }""");
        var feed = new NuGetFeed(fake.Source, fake);

        var details = await feed.ReadDetailsAsync("Tiny", new FeedVersion("1.0.0-Beta", 0, ""));

        Assert.Equal("https://license.test", details.LicenseUrl);
        Assert.Null(details.Published);
    }

    [Theory]
    [InlineData("[8.0.1, )", "≥ 8.0.1")]
    [InlineData("8.0.1", "≥ 8.0.1")]
    [InlineData("[1.0]", "= 1.0")]
    [InlineData("[1.0, 2.0)", "≥ 1.0, < 2.0")]
    [InlineData("(, 2.0]", "≤ 2.0")]
    [InlineData("", "")]
    public void A_version_range_is_described_as_comparisons(string range, string described)
    {
        Assert.Equal(described, VersionRange.Describe(range));
    }

    [Fact]
    public async Task A_page_of_a_search_skips_the_pages_before_it()
    {
        var fake = new FakeFeed();
        fake.AnswerSearches(TwoPackages);
        var feed = new NuGetFeed(fake.Source, fake);

        await feed.SearchAsync("json", includePrerelease: false, page: 2);

        Assert.Contains($"skip={2 * NuGetFeed.PageSize}&take={NuGetFeed.PageSize}&", fake.Requested.Last());
    }

    [Fact]
    public async Task A_feed_that_gives_no_total_counts_what_is_on_the_page()
    {
        var fake = new FakeFeed();
        fake.AnswerSearches("""{ "data": [ { "id": "Tiny", "version": "1.0.0" } ] }""");
        var feed = new NuGetFeed(fake.Source, fake);

        var found = await feed.SearchAsync("", includePrerelease: false);

        Assert.Equal(1, found.TotalHits);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(400000, 121)]
    public void The_pages_are_counted_up_to_the_last_one_a_feed_lets_start(int totalHits, int pages)
    {
        Assert.Equal(pages, NuGetFeed.CountPages(totalHits));
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

public sealed class PackageIconsTests
{
    private const string IconUrl = "https://feed.test/icon";

    [Fact]
    public async Task An_icon_is_fetched_and_drawn_to_fit_the_side_asked_for()
    {
        var fake = new FakeFeed();
        fake.Answer(IconUrl, """<svg xmlns="http://www.w3.org/2000/svg" width="256" height="128"><rect width="256" height="128" fill="red"/></svg>""");
        var icons = new PackageIcons(fake);

        var icon = await icons.FetchAsync(IconUrl, 64);

        Assert.Equal(64, icon.Width);
        Assert.Equal(32, icon.Height);
    }

    [Fact]
    public async Task What_is_no_picture_cannot_be_an_icon()
    {
        var fake = new FakeFeed();
        fake.Answer(IconUrl, "<html>not found</html>");
        var icons = new PackageIcons(fake);

        await Assert.ThrowsAsync<InvalidDataException>(() => icons.FetchAsync(IconUrl, 64));
    }

    [Fact]
    public void A_picture_shrinks_by_averaging_what_each_pixel_covers_and_clear_pixels_lend_no_color()
    {
        byte[] rgba =
        [
            255, 0, 0, 255, 0, 0, 255, 255, 0, 255, 0, 0, 0, 255, 0, 0,
            255, 0, 0, 255, 0, 0, 255, 255, 0, 255, 0, 0, 0, 255, 0, 0,
        ];
        var image = new ZGF.ILSpy.Core.Resources.ResourceImage(4, 2, rgba, "PNG");

        var shrunk = image.FitInto(2);

        Assert.Equal(2, shrunk.Width);
        Assert.Equal(1, shrunk.Height);
        Assert.Equal([127, 0, 127, 255, 0, 0, 0, 0], shrunk.Rgba);
    }
}

public sealed class NuGetSetupTests
{
    [Fact]
    public void A_typed_url_is_a_source_named_after_its_host()
    {
        var source = NuGetSource.FromUrl(" https://pkgs.test/v3/index.json ");

        Assert.Equal(new NuGetSource("pkgs.test", "https://pkgs.test/v3/index.json"), source);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nuget.org")]
    [InlineData("C:\\packages")]
    [InlineData("ftp://pkgs.test/index.json")]
    public void Text_that_is_no_web_url_is_no_source(string typed)
    {
        Assert.Null(NuGetSource.FromUrl(typed));
    }

    [Fact]
    public void A_feed_is_made_once_per_url_and_named_as_the_config_names_it()
    {
        using var temp = new TempDir();
        var listed = new NuGetSource("company", "https://pkgs.test/v3/index.json");
        var setup = new NuGetSetup([listed], new PackageCache(temp.PathOf("global"), temp.PathOf("downloads")));

        var feed = setup.FeedAt(NuGetSource.FromUrl("https://PKGS.test/v3/index.json")!);

        Assert.Same(feed, setup.FeedAt(listed));
        Assert.Equal("company", feed.Source.Name);
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

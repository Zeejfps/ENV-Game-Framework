using System.Net;
using System.Text.Json;

namespace ZGF.ILSpy.Core;

/// <summary>
/// A NuGet feed spoken to over the v3 protocol: its service index, read the first time it is needed,
/// names the search service, the folder of package files the feed serves, and where it describes
/// each version. Every call may be made from any thread.
/// </summary>
public sealed class NuGetFeed
{
    private const string SearchType = "SearchQueryService";
    private const string PackageFolderType = "PackageBaseAddress/3.0.0";
    private const string RegistrationsType = "RegistrationsBaseUrl";

    /// <summary>How many packages a page of a search holds.</summary>
    public const int PageSize = 25;

    /// <summary>The furthest into a search nuget.org lets a page start; every feed is held to it.</summary>
    public const int MaxSkip = 3000;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private static readonly Lazy<HttpClient> Shared = new(() => CreateClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All }));

    private readonly HttpClient _client;
    private readonly SemaphoreSlim _indexGate = new(1, 1);
    private ServiceIndex? _index;

    /// <param name="handler">Sends the feed's requests; the app's shared connections by default.</param>
    public NuGetFeed(NuGetSource source, HttpMessageHandler? handler = null)
    {
        Source = source;
        _client = ClientFor(handler);
    }

    /// <summary>A client sending through <paramref name="handler"/>, or the app's shared one.</summary>
    internal static HttpClient ClientFor(HttpMessageHandler? handler)
    {
        if (handler == null)
            return Shared.Value;
        return CreateClient(handler);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { Timeout = Timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ZGF.ILSpy");
        return client;
    }

    public NuGetSource Source { get; }

    /// <summary>How many pages a search that matched <paramref name="totalHits"/> packages has, up to
    /// the last one <see cref="MaxSkip"/> lets start; at least one.</summary>
    public static int CountPages(int totalHits)
    {
        var reachable = Math.Min(totalHits, MaxSkip + PageSize);
        var pages = (reachable + PageSize - 1) / PageSize;
        return Math.Max(1, pages);
    }

    /// <summary>Page <paramref name="page"/>, from 0, of the packages matching <paramref name="query"/>;
    /// of every package when the query is empty.</summary>
    /// <exception cref="HttpRequestException">The feed could not be reached or refused the request.</exception>
    /// <exception cref="InvalidDataException">The feed answered with something that is no search result.</exception>
    public async Task<FeedPage> SearchAsync(string query, bool includePrerelease, int page = 0, CancellationToken cancellationToken = default)
    {
        var index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        var url = SearchUrl(index.Search, query, includePrerelease, page);
        using var document = await ReadJsonAsync(url, cancellationToken).ConfigureAwait(false);
        return FeedJson.ReadPage(document.RootElement);
    }

    private static string SearchUrl(string search, string query, bool includePrerelease, int page)
    {
        var q = Uri.EscapeDataString(query.Trim());
        var prerelease = includePrerelease ? "true" : "false";
        var skip = Math.Min(page * PageSize, MaxSkip);
        return $"{search}?q={q}&skip={skip}&take={PageSize}&prerelease={prerelease}&semVerLevel=2.0.0";
    }

    /// <summary>What the feed's catalog says of <paramref name="version"/> of the package <paramref name="id"/>:
    /// read from where the search said the feed describes it, else from the feed's registrations.</summary>
    /// <exception cref="HttpRequestException">The feed could not be reached, or has no such version.</exception>
    /// <exception cref="InvalidDataException">The feed describes no versions, or answered with something else.</exception>
    public async Task<PackageVersionDetails> ReadDetailsAsync(PackageId id, FeedVersion version, CancellationToken cancellationToken = default)
    {
        var url = await FindDetailsUrlAsync(id, version, cancellationToken).ConfigureAwait(false);
        using var leaf = await ReadJsonAsync(url, cancellationToken).ConfigureAwait(false);
        var listedAt = FeedJson.ReadDate(leaf.RootElement, "published");
        if (!leaf.RootElement.TryGetProperty("catalogEntry", out var entry))
            throw new InvalidDataException($"{url} names no catalog entry.");
        return await ReadCatalogEntryAsync(entry, listedAt, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> FindDetailsUrlAsync(PackageId id, FeedVersion version, CancellationToken cancellationToken)
    {
        if (version.DetailsUrl.Length > 0)
            return version.DetailsUrl;
        var index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        if (index.Registrations == null)
            throw new InvalidDataException($"The service index of {Source.Url} names no {RegistrationsType}.");
        return $"{index.Registrations.TrimEnd('/')}/{id.Lower}/{version.Version.Filed}.json";
    }

    /// <summary>The details in a catalog entry, which a registration leaf gives inline or as the URL to read it from.</summary>
    private async Task<PackageVersionDetails> ReadCatalogEntryAsync(JsonElement entry, DateTimeOffset? listedAt, CancellationToken cancellationToken)
    {
        if (entry.ValueKind == JsonValueKind.Object)
            return FeedJson.ReadDetails(entry, listedAt);
        if (entry.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("A catalog entry is neither inline nor a URL.");
        using var catalog = await ReadJsonAsync(entry.GetString()!, cancellationToken).ConfigureAwait(false);
        return FeedJson.ReadDetails(catalog.RootElement, listedAt);
    }

    /// <summary>Writes the package file of <paramref name="id"/> at <paramref name="version"/> to
    /// <paramref name="path"/>, which is replaced only once the whole file is in.</summary>
    /// <exception cref="HttpRequestException">The feed could not be reached, or has no such package.</exception>
    /// <exception cref="IOException">The file could not be written.</exception>
    public async Task DownloadAsync(PackageId id, PackageVersion version, string path, CancellationToken cancellationToken = default)
    {
        var index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        var url = PackageUrl(index.PackageFolder, id, version);
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await WriteWholeFileAsync(response, path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Where the feed serves a package file: <c>{folder}/{id}/{version}/{id}.{version}.nupkg</c>,
    /// in lower case, the version without its build metadata.</summary>
    private static string PackageUrl(string folder, PackageId id, PackageVersion version)
    {
        var lowerId = id.Lower;
        var lowerVersion = version.Filed;
        return $"{folder.TrimEnd('/')}/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";
    }

    private static async Task WriteWholeFileAsync(HttpResponseMessage response, string path, CancellationToken cancellationToken)
    {
        var temporary = path + ".download";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            await using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                await response.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private async Task<ServiceIndex> ReadIndexAsync(CancellationToken cancellationToken)
    {
        await _indexGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_index == null)
                _index = await FetchIndexAsync(cancellationToken).ConfigureAwait(false);
            return _index;
        }
        finally
        {
            _indexGate.Release();
        }
    }

    private async Task<ServiceIndex> FetchIndexAsync(CancellationToken cancellationToken)
    {
        using var document = await ReadJsonAsync(Source.Url, cancellationToken).ConfigureAwait(false);
        var resources = FeedJson.ReadArray(document.RootElement, "resources");
        var search = FindResource(resources, SearchType);
        var folder = FindResource(resources, PackageFolderType);
        var registrations = FindOptionalResource(resources, RegistrationsType);
        return new ServiceIndex(search, folder, registrations);
    }

    /// <summary>The URL of the first resource whose type is <paramref name="type"/>, or a version of it.</summary>
    private string FindResource(JsonElement resources, string type)
    {
        var url = FindOptionalResource(resources, type);
        if (url == null)
            throw new InvalidDataException($"The service index of {Source.Url} names no {type}.");
        return url;
    }

    private static string? FindOptionalResource(JsonElement resources, string type)
    {
        foreach (var resource in resources.EnumerateArray())
        {
            if (IsOfType(resource, type))
                return FeedJson.ReadString(resource, "@id");
        }
        return null;
    }

    private static bool IsOfType(JsonElement resource, string type)
    {
        var found = FeedJson.ReadString(resource, "@type");
        if (found == type)
            return true;
        return found.StartsWith(type + "/", StringComparison.Ordinal);
    }

    private async Task<JsonDocument> ReadJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{url} answered with something that is not JSON: {e.Message}", e);
        }
    }

    /// <summary>The resources of the feed's service index this reads; a feed may describe no versions.</summary>
    private sealed record ServiceIndex(string Search, string PackageFolder, string? Registrations);
}

using System.Net;
using System.Text.Json;

namespace ZGF.ILSpy.Core;

/// <summary>A package a feed's search found: its latest version and, newest first, every version
/// listed with it.</summary>
public sealed record FeedPackage(string Id, string Version, string Description, string Authors, IReadOnlyList<string> Versions);

/// <summary>
/// A NuGet feed spoken to over the v3 protocol: its service index, read the first time it is needed,
/// names the search service and the folder of package files the feed serves. Every call may be made
/// from any thread.
/// </summary>
public sealed class NuGetFeed
{
    private const string SearchType = "SearchQueryService";
    private const string PackageFolderType = "PackageBaseAddress/3.0.0";
    private const int SearchSize = 50;
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

    private static HttpClient ClientFor(HttpMessageHandler? handler)
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

    /// <summary>The packages matching <paramref name="query"/>, the closest first, as many as one
    /// page of the search holds; every package when the query is empty.</summary>
    /// <exception cref="HttpRequestException">The feed could not be reached or refused the request.</exception>
    /// <exception cref="InvalidDataException">The feed answered with something that is no search result.</exception>
    public async Task<IReadOnlyList<FeedPackage>> SearchAsync(string query, bool includePrerelease, CancellationToken cancellationToken = default)
    {
        var index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        var url = SearchUrl(index.Search, query, includePrerelease);
        using var document = await ReadJsonAsync(url, cancellationToken).ConfigureAwait(false);
        return ReadPackages(document.RootElement);
    }

    private static string SearchUrl(string search, string query, bool includePrerelease)
    {
        var q = Uri.EscapeDataString(query.Trim());
        var prerelease = includePrerelease ? "true" : "false";
        return $"{search}?q={q}&skip=0&take={SearchSize}&prerelease={prerelease}&semVerLevel=2.0.0";
    }

    /// <summary>Writes the package file of <paramref name="id"/> at <paramref name="version"/> to
    /// <paramref name="path"/>, which is replaced only once the whole file is in.</summary>
    /// <exception cref="HttpRequestException">The feed could not be reached, or has no such package.</exception>
    /// <exception cref="IOException">The file could not be written.</exception>
    public async Task DownloadAsync(string id, string version, string path, CancellationToken cancellationToken = default)
    {
        var index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        var url = PackageUrl(index.PackageFolder, id, version);
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await WriteWholeFileAsync(response, path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Where the feed serves a package file: <c>{folder}/{id}/{version}/{id}.{version}.nupkg</c>,
    /// in lower case, the version without its build metadata.</summary>
    private static string PackageUrl(string folder, string id, string version)
    {
        var lowerId = id.ToLowerInvariant();
        var lowerVersion = PackageCache.NormalizeVersion(version);
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
        var resources = ReadArray(document.RootElement, "resources");
        var search = FindResource(resources, SearchType);
        var folder = FindResource(resources, PackageFolderType);
        return new ServiceIndex(search, folder);
    }

    /// <summary>The URL of the first resource whose type is <paramref name="type"/>, or a version of it.</summary>
    private string FindResource(JsonElement resources, string type)
    {
        foreach (var resource in resources.EnumerateArray())
        {
            if (IsOfType(resource, type))
                return ReadString(resource, "@id");
        }
        throw new InvalidDataException($"The service index of {Source.Url} names no {type}.");
    }

    private static bool IsOfType(JsonElement resource, string type)
    {
        var found = ReadString(resource, "@type");
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

    private static List<FeedPackage> ReadPackages(JsonElement root)
    {
        var packages = new List<FeedPackage>();
        foreach (var package in ReadArray(root, "data").EnumerateArray())
            packages.Add(ReadPackage(package));
        return packages;
    }

    private static FeedPackage ReadPackage(JsonElement package)
    {
        var id = ReadString(package, "id");
        var version = ReadString(package, "version");
        var description = ReadString(package, "description");
        var authors = ReadAuthors(package);
        var versions = ReadVersions(package, version);
        return new FeedPackage(id, version, description, authors, versions);
    }

    /// <summary>The authors, which a feed may give as one string or as a list of them.</summary>
    private static string ReadAuthors(JsonElement package)
    {
        if (!package.TryGetProperty("authors", out var authors))
            return "";
        if (authors.ValueKind != JsonValueKind.Array)
            return ReadText(authors);
        var names = authors.EnumerateArray()
            .Select(ReadText)
            .Where(a => a.Length > 0);
        return string.Join(", ", names);
    }

    /// <summary>The versions listed with the package, newest first; just <paramref name="latest"/>
    /// when the feed lists none.</summary>
    private static List<string> ReadVersions(JsonElement package, string latest)
    {
        var versions = new List<string>();
        if (package.TryGetProperty("versions", out var listed) && listed.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in listed.EnumerateArray())
                versions.Add(ReadString(entry, "version"));
        }
        versions.Reverse();
        if (versions.Count == 0)
            versions.Add(latest);
        return versions;
    }

    private static JsonElement ReadArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Expected an object holding {name}.");
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Expected a list named {name}.");
        return array;
    }

    private static string ReadString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return "";
        if (!element.TryGetProperty(name, out var value))
            return "";
        return ReadText(value);
    }

    private static string ReadText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            return "";
        return value.GetString() ?? "";
    }

    /// <summary>The resources of the feed's service index this reads.</summary>
    private sealed record ServiceIndex(string Search, string PackageFolder);
}

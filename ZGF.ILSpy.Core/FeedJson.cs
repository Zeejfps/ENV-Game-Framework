using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ZGF.ILSpy.Core;

/// <summary>
/// Reads what a NuGet feed answers in JSON: a page of its search, a version's registration and catalog
/// entry, and the service index. What a feed leaves out reads as empty, 0 or null. A package or resource
/// of another shape is left out on its own; an answer of another shape cannot be read.
/// </summary>
internal static class FeedJson
{
    /// <summary>The year a feed dates a version it unlisted.</summary>
    private const int UnlistedYear = 1900;

    /// <summary>The page of packages <paramref name="root"/> holds, without those that cannot be read.</summary>
    /// <exception cref="InvalidDataException">It is no page of a search.</exception>
    public static FeedPage ReadPage(JsonElement root)
    {
        var page = Read(root, FeedJsonContext.Default.SearchPageJson, "a page of a search");
        if (page.Data == null)
            throw new InvalidDataException("A page of a search holds no list named data.");
        var packages = page.Data
            .Select(static p => TryRead(p, FeedJsonContext.Default.PackageJson)?.Parse())
            .OfType<FeedPackage>()
            .ToList();
        var total = (int)Math.Min(page.TotalHits ?? 0, int.MaxValue);
        return new FeedPage(packages, Math.Max(total, packages.Count));
    }

    /// <exception cref="InvalidDataException">It is no registration of a version.</exception>
    public static FeedRegistration ReadRegistration(JsonElement root)
    {
        var registration = Read(root, FeedJsonContext.Default.RegistrationJson, "a registration");
        return new FeedRegistration(ReadDate(registration.Published), registration.CatalogEntry);
    }

    /// <summary>The details in a version's catalog entry, published when <paramref name="listedAt"/>
    /// says unless the entry itself says.</summary>
    /// <exception cref="InvalidDataException">It is no catalog entry.</exception>
    public static PackageVersionDetails ReadDetails(JsonElement entry, DateTimeOffset? listedAt) =>
        Read(entry, FeedJsonContext.Default.CatalogEntryJson, "a catalog entry").Parse(listedAt);

    /// <summary>The resources the service index <paramref name="root"/> lists, without those that cannot be read.</summary>
    /// <exception cref="InvalidDataException">It is no service index.</exception>
    public static List<FeedResource> ReadResources(JsonElement root)
    {
        var index = Read(root, FeedJsonContext.Default.ServiceIndexJson, "a service index");
        if (index.Resources == null)
            throw new InvalidDataException("A service index holds no list named resources.");
        return index.Resources
            .Select(static r => TryRead(r, FeedJsonContext.Default.ResourceJson)?.Parse())
            .OfType<FeedResource>()
            .ToList();
    }

    /// <summary>When a version was published; null when the feed does not say, or dates it as unlisted.</summary>
    public static DateTimeOffset? ReadDate(string? text)
    {
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return null;
        if (date.Year <= UnlistedYear)
            return null;
        return date;
    }

    private static T Read<T>(JsonElement element, JsonTypeInfo<T> type, string what) where T : class
    {
        try
        {
            return element.Deserialize(type) ?? throw new InvalidDataException($"Expected {what}, found null.");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Expected {what}: {e.Message}", e);
        }
    }

    private static T? TryRead<T>(JsonElement element, JsonTypeInfo<T> type) where T : class
    {
        try
        {
            return element.Deserialize(type);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>A version's registration: when it was listed, and its catalog entry, inline or as the URL of one.</summary>
internal sealed record FeedRegistration(DateTimeOffset? Published, JsonElement? CatalogEntry);

/// <summary>A resource a service index lists: the types it is of, and its URL when it is a web URL.</summary>
internal sealed record FeedResource(IReadOnlyList<string> Types, WebUrl? Url);

internal sealed record SearchPageJson(long? TotalHits, List<JsonElement>? Data);

internal sealed record PackageJson(
    string? Id,
    string? Version,
    string? Description,
    TextListJson? Authors,
    string? IconUrl,
    long? TotalDownloads,
    bool? Verified,
    TextListJson? Owners,
    TextListJson? Tags,
    string? ProjectUrl,
    string? LicenseUrl,
    List<JsonElement>? Vulnerabilities,
    List<VersionJson?>? Versions)
{
    /// <summary>The package; null when its id or version is none NuGet allows.</summary>
    public FeedPackage? Parse()
    {
        var id = PackageId.Parse(Id ?? "");
        var version = PackageVersion.Parse(Version ?? "");
        if (id == null || version == null)
            return null;
        return new FeedPackage(id, version, Description ?? "", Joined(Authors), WebUrl.Parse(IconUrl), VersionsOf(version))
        {
            TotalDownloads = TotalDownloads ?? 0,
            Verified = Verified ?? false,
            Owners = Joined(Owners),
            Tags = Words(Tags),
            ProjectUrl = WebUrl.Parse(ProjectUrl),
            LicenseUrl = WebUrl.Parse(LicenseUrl),
            Vulnerabilities = Vulnerabilities?.Count ?? 0,
        };
    }

    /// <summary>The versions listed, newest first, without those NuGet allows no such version of; just
    /// <paramref name="latest"/> when the feed lists none.</summary>
    private List<FeedVersion> VersionsOf(PackageVersion latest)
    {
        var versions = (Versions ?? [])
            .Select(static v => v?.Parse())
            .OfType<FeedVersion>()
            .Reverse()
            .ToList();
        if (versions.Count == 0)
            versions.Add(new FeedVersion(latest, 0, null));
        return versions;
    }

    /// <summary>Names a feed gives as one string or as a list of them, joined by commas.</summary>
    private static string Joined(TextListJson? names) =>
        names == null ? "" : string.Join(", ", names.Items.Where(static n => n.Length > 0));

    /// <summary>Words a feed gives as a list of them or as one string, split on spaces.</summary>
    private static List<string> Words(TextListJson? words) =>
        words == null ? [] : words.Items.SelectMany(static w => w.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();
}

internal sealed record VersionJson(string? Version, long? Downloads, [property: JsonPropertyName("@id")] string? Id)
{
    public FeedVersion? Parse()
    {
        var version = PackageVersion.Parse(Version ?? "");
        if (version == null)
            return null;
        return new FeedVersion(version, Downloads ?? 0, WebUrl.Parse(Id));
    }
}

internal sealed record RegistrationJson(string? Published, JsonElement? CatalogEntry);

internal sealed record CatalogEntryJson(
    string? Published,
    string? LicenseExpression,
    string? LicenseUrl,
    string? ProjectUrl,
    long? PackageSize,
    List<DependencyGroupJson?>? DependencyGroups,
    DeprecationJson? Deprecation,
    List<VulnerabilityJson?>? Vulnerabilities)
{
    public PackageVersionDetails Parse(DateTimeOffset? listedAt) => new(
        FeedJson.ReadDate(Published) ?? listedAt,
        LicenseExpression ?? "",
        WebUrl.Parse(LicenseUrl),
        WebUrl.Parse(ProjectUrl),
        PackageSize ?? 0,
        (DependencyGroups ?? []).OfType<DependencyGroupJson>().Select(static g => g.Parse()).ToList(),
        Deprecation?.Parse(),
        (Vulnerabilities ?? []).OfType<VulnerabilityJson>().Select(static v => v.Parse()).ToList());
}

internal sealed record DependencyGroupJson(string? TargetFramework, List<DependencyJson?>? Dependencies)
{
    public DependencyGroup Parse() => new(
        TargetFramework ?? "",
        (Dependencies ?? []).OfType<DependencyJson>().Select(static d => new PackageDependency(d.Id ?? "", d.Range ?? "")).ToList());
}

internal sealed record DependencyJson(string? Id, string? Range);

internal sealed record DeprecationJson(TextListJson? Reasons, string? Message, AlternatePackageJson? AlternatePackage)
{
    public PackageDeprecation Parse() => new(
        Reasons == null ? [] : Reasons.Items.Where(static r => r.Length > 0).ToList(),
        Message ?? "",
        AlternatePackage?.Id ?? "");
}

internal sealed record AlternatePackageJson(string? Id);

/// <summary>A vulnerability, whose severity a feed may give as a number or as its digits.</summary>
internal sealed record VulnerabilityJson(string? AdvisoryUrl, JsonElement? Severity)
{
    public PackageVulnerability Parse() => new(SeverityOf(Severity), WebUrl.Parse(AdvisoryUrl));

    /// <summary>The severity; 0 for anything but a whole number that fits an int, or its digits.</summary>
    private static int SeverityOf(JsonElement? severity)
    {
        if (severity == null)
            return 0;
        var value = severity.Value;
        if (value.ValueKind == JsonValueKind.Number)
            return value.TryGetInt32(out var number) ? number : 0;
        if (value.ValueKind != JsonValueKind.String)
            return 0;
        int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed);
        return parsed;
    }
}

internal sealed record ServiceIndexJson(List<JsonElement>? Resources);

internal sealed record ResourceJson([property: JsonPropertyName("@id")] string? Id, [property: JsonPropertyName("@type")] TextListJson? Type)
{
    public FeedResource Parse() => new(Type == null ? [] : Type.Items, WebUrl.Parse(Id));
}

/// <summary>Text a feed may give as one string or as a list of strings; what else a list holds is left out.</summary>
[JsonConverter(typeof(TextListConverter))]
internal sealed record TextListJson(IReadOnlyList<string> Items);

internal sealed class TextListConverter : JsonConverter<TextListJson>
{
    public override TextListJson Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new TextListJson([reader.GetString() ?? ""]);
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected a string or a list of them.");
        var items = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
                items.Add(reader.GetString() ?? "");
            else
                reader.Skip();
        }
        return new TextListJson(items);
    }

    public override void Write(Utf8JsonWriter writer, TextListJson value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.Items)
            writer.WriteStringValue(item);
        writer.WriteEndArray();
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(SearchPageJson))]
[JsonSerializable(typeof(PackageJson))]
[JsonSerializable(typeof(RegistrationJson))]
[JsonSerializable(typeof(CatalogEntryJson))]
[JsonSerializable(typeof(ServiceIndexJson))]
[JsonSerializable(typeof(ResourceJson))]
internal sealed partial class FeedJsonContext : JsonSerializerContext;

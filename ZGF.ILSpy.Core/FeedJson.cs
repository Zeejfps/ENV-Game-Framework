using System.Globalization;
using System.Text.Json;

namespace ZGF.ILSpy.Core;

/// <summary>Reads what a NuGet feed answers in JSON: a page of its search, and a version's catalog
/// entry. What a feed leaves out reads as empty, 0 or null; only a missing list of results fails.</summary>
internal static class FeedJson
{
    /// <summary>The year a feed dates a version it unlisted.</summary>
    private const int UnlistedYear = 1900;

    public static FeedPage ReadPage(JsonElement root)
    {
        var packages = ReadPackages(root);
        var totalHits = ReadTotalHits(root, packages.Count);
        return new FeedPage(packages, totalHits);
    }

    private static List<FeedPackage> ReadPackages(JsonElement root)
    {
        var packages = new List<FeedPackage>();
        foreach (var package in ReadArray(root, "data").EnumerateArray())
            packages.Add(ReadPackage(package));
        return packages;
    }

    /// <summary>How many packages match in all; <paramref name="onPage"/> when the feed does not say.</summary>
    private static int ReadTotalHits(JsonElement root, int onPage)
    {
        var hits = ReadLong(root, "totalHits");
        var total = (int)Math.Min(hits, int.MaxValue);
        return Math.Max(total, onPage);
    }

    private static FeedPackage ReadPackage(JsonElement package)
    {
        var version = ReadString(package, "version");
        return new FeedPackage(
            ReadString(package, "id"),
            version,
            ReadString(package, "description"),
            ReadNames(package, "authors"),
            ReadString(package, "iconUrl"),
            ReadVersions(package, version))
        {
            TotalDownloads = ReadLong(package, "totalDownloads"),
            Verified = ReadBool(package, "verified"),
            Owners = ReadNames(package, "owners"),
            Tags = ReadWords(package, "tags"),
            ProjectUrl = ReadString(package, "projectUrl"),
            LicenseUrl = ReadString(package, "licenseUrl"),
            Vulnerabilities = CountItems(package, "vulnerabilities"),
        };
    }

    /// <summary>The versions listed with the package, newest first; just <paramref name="latest"/>
    /// when the feed lists none.</summary>
    private static List<FeedVersion> ReadVersions(JsonElement package, string latest)
    {
        var versions = new List<FeedVersion>();
        foreach (var entry in ReadOptionalArray(package, "versions"))
            versions.Add(ReadVersion(entry));
        versions.Reverse();
        if (versions.Count == 0)
            versions.Add(new FeedVersion(latest, 0, ""));
        return versions;
    }

    private static FeedVersion ReadVersion(JsonElement entry)
    {
        var version = ReadString(entry, "version");
        var downloads = ReadLong(entry, "downloads");
        var detailsUrl = ReadString(entry, "@id");
        return new FeedVersion(version, downloads, detailsUrl);
    }

    /// <summary>The details in a version's catalog entry, published when <paramref name="listedAt"/>
    /// says unless the entry itself says.</summary>
    public static PackageVersionDetails ReadDetails(JsonElement entry, DateTimeOffset? listedAt)
    {
        var published = ReadDate(entry, "published");
        if (published == null)
            published = listedAt;
        return new PackageVersionDetails(
            published,
            ReadString(entry, "licenseExpression"),
            ReadString(entry, "licenseUrl"),
            ReadString(entry, "projectUrl"),
            ReadLong(entry, "packageSize"),
            ReadDependencyGroups(entry),
            ReadDeprecation(entry),
            ReadVulnerabilities(entry));
    }

    private static List<DependencyGroup> ReadDependencyGroups(JsonElement entry)
    {
        var groups = new List<DependencyGroup>();
        foreach (var group in ReadOptionalArray(entry, "dependencyGroups"))
            groups.Add(ReadDependencyGroup(group));
        return groups;
    }

    private static DependencyGroup ReadDependencyGroup(JsonElement group)
    {
        var dependencies = ReadOptionalArray(group, "dependencies")
            .Select(d => new PackageDependency(ReadString(d, "id"), ReadString(d, "range")))
            .ToList();
        return new DependencyGroup(ReadString(group, "targetFramework"), dependencies);
    }

    private static PackageDeprecation? ReadDeprecation(JsonElement entry)
    {
        if (!entry.TryGetProperty("deprecation", out var deprecation))
            return null;
        if (deprecation.ValueKind != JsonValueKind.Object)
            return null;
        var reasons = ReadOptionalArray(deprecation, "reasons")
            .Select(ReadText)
            .Where(r => r.Length > 0)
            .ToList();
        var alternate = ReadAlternate(deprecation);
        return new PackageDeprecation(reasons, ReadString(deprecation, "message"), alternate);
    }

    private static string ReadAlternate(JsonElement deprecation)
    {
        if (!deprecation.TryGetProperty("alternatePackage", out var alternate))
            return "";
        return ReadString(alternate, "id");
    }

    private static List<PackageVulnerability> ReadVulnerabilities(JsonElement entry)
    {
        return ReadOptionalArray(entry, "vulnerabilities")
            .Select(v => new PackageVulnerability(ReadSeverity(v), ReadString(v, "advisoryUrl")))
            .ToList();
    }

    /// <summary>A vulnerability's severity, which a feed may give as a number or as its digits.</summary>
    private static int ReadSeverity(JsonElement vulnerability)
    {
        if (!vulnerability.TryGetProperty("severity", out var severity))
            return 0;
        if (severity.ValueKind == JsonValueKind.Number)
            return severity.GetInt32();
        int.TryParse(ReadText(severity), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed);
        return parsed;
    }

    /// <summary>When a version was published; null when the feed does not say, or dates it as unlisted.</summary>
    public static DateTimeOffset? ReadDate(JsonElement element, string name)
    {
        var text = ReadString(element, name);
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return null;
        if (date.Year <= UnlistedYear)
            return null;
        return date;
    }

    /// <summary>Names a feed may give as one string or as a list of them, joined by commas.</summary>
    private static string ReadNames(JsonElement package, string name)
    {
        if (!package.TryGetProperty(name, out var names))
            return "";
        if (names.ValueKind != JsonValueKind.Array)
            return ReadText(names);
        var joined = names.EnumerateArray()
            .Select(ReadText)
            .Where(n => n.Length > 0);
        return string.Join(", ", joined);
    }

    /// <summary>Words a feed may give as a list of them or as one string, split on spaces.</summary>
    private static List<string> ReadWords(JsonElement package, string name)
    {
        if (!package.TryGetProperty(name, out var words))
            return [];
        if (words.ValueKind != JsonValueKind.Array)
            return ReadText(words).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        return words.EnumerateArray()
            .Select(ReadText)
            .Where(w => w.Length > 0)
            .ToList();
    }

    private static int CountItems(JsonElement element, string name) => ReadOptionalArray(element, name).Count();

    public static JsonElement ReadArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Expected an object holding {name}.");
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Expected a list named {name}.");
        return array;
    }

    /// <summary>The items of the list <paramref name="name"/>; none when there is no such list.</summary>
    private static IEnumerable<JsonElement> ReadOptionalArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return [];
        if (!element.TryGetProperty(name, out var array))
            return [];
        if (array.ValueKind != JsonValueKind.Array)
            return [];
        return array.EnumerateArray();
    }

    public static string ReadString(JsonElement element, string name)
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

    private static long ReadLong(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return 0;
        if (!element.TryGetProperty(name, out var value))
            return 0;
        if (value.ValueKind != JsonValueKind.Number)
            return 0;
        if (!value.TryGetInt64(out var number))
            return 0;
        return number;
    }

    private static bool ReadBool(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return false;
        return value.ValueKind == JsonValueKind.True;
    }
}

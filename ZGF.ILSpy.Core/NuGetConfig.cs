using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace ZGF.ILSpy.Core;

/// <summary>A NuGet package source: the name the user gave it and the URL of its service index.</summary>
public sealed record NuGetSource
{
    private const string ServiceIndexName = "index.json";

    public static readonly NuGetSource NuGetOrg =
        new("nuget.org", WebUrl.Parse("https://api.nuget.org/v3/index.json") ?? throw new UnreachableException());

    private NuGetSource(string name, WebUrl url)
    {
        Name = name;
        Url = url;
    }

    public string Name { get; }

    public WebUrl Url { get; }

    /// <summary>The source a typed <paramref name="url"/> names, called after its host; null unless it
    /// is an absolute HTTP or HTTPS URL.</summary>
    public static NuGetSource? FromUrl(string url)
    {
        var web = WebUrl.Parse(url);
        if (web == null)
            return null;
        return new NuGetSource(web.Uri.Host, web);
    }

    /// <summary>The source a config adds as <paramref name="name"/>; null unless <paramref name="url"/>
    /// is the service index of a feed served over HTTP, which a v2 feed or a folder of packages is not.</summary>
    public static NuGetSource? FromConfig(string name, string url)
    {
        var web = WebUrl.Parse(url);
        if (web == null)
            return null;
        if (!web.Uri.AbsolutePath.EndsWith(ServiceIndexName, StringComparison.OrdinalIgnoreCase))
            return null;
        return new NuGetSource(name, web);
    }
}

/// <summary>
/// What the user's NuGet.Config says about where packages come from: the enabled sources that speak
/// the v3 protocol over HTTP, and the global packages folder that restores fill. A config that is
/// missing or cannot be read is as one that says nothing: nuget.org and the default folder.
/// </summary>
public sealed record NuGetConfig(IReadOnlyList<NuGetSource> Sources, string GlobalPackagesFolder)
{
    private const string PackagesFolderVariable = "NUGET_PACKAGES";

    /// <summary>The config of the user running the app.</summary>
    public static NuGetConfig ReadUser() => Read(UserConfigPath(), PackagesFolderFromEnvironment());

    /// <summary>What a config that says nothing means: nuget.org, and the default packages folder.</summary>
    public static NuGetConfig CreateDefault() =>
        new([NuGetSource.NuGetOrg], FindGlobalPackagesFolder(null, "", PackagesFolderFromEnvironment()));

    private static string? PackagesFolderFromEnvironment() => Environment.GetEnvironmentVariable(PackagesFolderVariable);

    private static string UserConfigPath()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet", "NuGet.Config");
        return Path.Combine(HomeFolder(), ".nuget", "NuGet", "NuGet.Config");
    }

    private static string HomeFolder() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>The config at <paramref name="path"/>, with <paramref name="packagesFolder"/> the value of
    /// <c>NUGET_PACKAGES</c>, or null where it is not set.</summary>
    public static NuGetConfig Read(string path, string? packagesFolder)
    {
        var document = TryLoad(path);
        var sources = ListSources(document);
        var folder = FindGlobalPackagesFolder(document, path, packagesFolder);
        return new NuGetConfig(sources, folder);
    }

    private static XDocument? TryLoad(string path)
    {
        try
        {
            return XDocument.Load(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or XmlException)
        {
            return null;
        }
    }

    /// <summary>The enabled v3 sources, in the order the config adds them; nuget.org when it adds none.</summary>
    private static List<NuGetSource> ListSources(XDocument? document)
    {
        var sources = new List<NuGetSource>();
        if (document == null)
        {
            sources.Add(NuGetSource.NuGetOrg);
            return sources;
        }
        var disabled = ListDisabled(document);
        foreach (var entry in Section(document, "packageSources"))
            ApplySourceEntry(entry, disabled, sources);
        if (sources.Count == 0)
            sources.Add(NuGetSource.NuGetOrg);
        return sources;
    }

    private static HashSet<string> ListDisabled(XDocument document)
    {
        var disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Section(document, "disabledPackageSources"))
        {
            if (IsTrue(entry))
                disabled.Add(KeyOf(entry));
        }
        return disabled;
    }

    private static bool IsTrue(XElement entry)
    {
        var value = (string?)entry.Attribute("value");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Applies one entry of the sources section: a <c>clear</c> drops those added before it,
    /// an <c>add</c> adds its source unless it is disabled, is no v3 feed or its name is taken.</summary>
    private static void ApplySourceEntry(XElement entry, HashSet<string> disabled, List<NuGetSource> sources)
    {
        if (entry.Name.LocalName == "clear")
        {
            sources.Clear();
            return;
        }
        if (entry.Name.LocalName != "add")
            return;
        var name = KeyOf(entry);
        if (disabled.Contains(name))
            return;
        var source = NuGetSource.FromConfig(name, (string?)entry.Attribute("value") ?? "");
        if (source == null)
            return;
        if (sources.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            return;
        sources.Add(source);
    }

    /// <summary>The folder restores put packages in: the one the environment names, else the config's,
    /// else <c>.nuget/packages</c> in the user's home folder. The environment's counts only as an absolute
    /// path, as NuGet takes no other, and is taken as it is, as NuGet expands no variables in it.</summary>
    private static string FindGlobalPackagesFolder(XDocument? document, string configPath, string? fromEnvironment)
    {
        if (!string.IsNullOrEmpty(fromEnvironment) && Path.IsPathRooted(fromEnvironment))
            return Path.GetFullPath(fromEnvironment);
        var fromConfig = FindConfigValue(document, "globalPackagesFolder");
        if (!string.IsNullOrEmpty(fromConfig))
            return ResolveFrom(configPath, fromConfig);
        return Path.Combine(HomeFolder(), ".nuget", "packages");
    }

    private static string ResolveFrom(string configPath, string folder)
    {
        var expanded = Environment.ExpandEnvironmentVariables(folder);
        var configFolder = Path.GetDirectoryName(configPath) ?? "";
        return Path.GetFullPath(Path.Combine(configFolder, expanded));
    }

    private static string? FindConfigValue(XDocument? document, string key)
    {
        if (document == null)
            return null;
        var entry = Section(document, "config")
            .FirstOrDefault(e => KeyOf(e) == key);
        return (string?)entry?.Attribute("value");
    }

    private static IEnumerable<XElement> Section(XDocument document, string name)
    {
        var section = document.Root?.Element(name);
        if (section == null)
            return [];
        return section.Elements();
    }

    private static string KeyOf(XElement entry) => (string?)entry.Attribute("key") ?? "";
}

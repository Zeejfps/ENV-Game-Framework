namespace ZGF.ILSpy.Core;

/// <summary>
/// A package a feed's search found: its latest version, what it says of itself, where its icon is,
/// and, newest first, every version listed with it. Text a feed leaves out is empty, and counts it
/// leaves out are 0.
/// </summary>
public sealed record FeedPackage(string Id, string Version, string Description, string Authors, string IconUrl, IReadOnlyList<FeedVersion> Versions)
{
    /// <summary>How many times every version was downloaded.</summary>
    public long TotalDownloads { get; init; }

    /// <summary>Whether the feed vouches that the package's id prefix belongs to its owners.</summary>
    public bool Verified { get; init; }

    /// <summary>The accounts that own the package on the feed, joined by commas.</summary>
    public string Owners { get; init; } = "";

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string ProjectUrl { get; init; } = "";

    public string LicenseUrl { get; init; } = "";

    /// <summary>How many vulnerabilities the feed knows of in the latest version.</summary>
    public int Vulnerabilities { get; init; }
}

/// <summary>A version a feed lists for a package, how many times it was downloaded, and where the feed
/// describes it in full; empty when the search does not say.</summary>
public sealed record FeedVersion(string Version, long Downloads, string DetailsUrl);

/// <summary>A page of a feed's search: the packages on it, the closest first, and how many match in all.</summary>
public sealed record FeedPage(IReadOnlyList<FeedPackage> Packages, int TotalHits);

/// <summary>
/// What a feed's catalog says of one version of a package: when it was published, null when the feed
/// does not say or it is unlisted; its license, as an SPDX expression or else a URL; its project
/// page; the size of its file; what it depends on for each framework; and why it is deprecated, or
/// the vulnerabilities known in it, when either is so.
/// </summary>
public sealed record PackageVersionDetails(
    DateTimeOffset? Published,
    string License,
    string LicenseUrl,
    string ProjectUrl,
    long PackageSize,
    IReadOnlyList<DependencyGroup> DependencyGroups,
    PackageDeprecation? Deprecation,
    IReadOnlyList<PackageVulnerability> Vulnerabilities);

/// <summary>The packages a version depends on when used from <see cref="TargetFramework"/>, empty for any framework.</summary>
public sealed record DependencyGroup(string TargetFramework, IReadOnlyList<PackageDependency> Dependencies);

/// <summary>A package depended on, and the versions of it that do, as NuGet writes a range.</summary>
public sealed record PackageDependency(string Id, string Range);

/// <summary>Why a version is deprecated (such as Legacy or CriticalBugs), what its owners say, and the
/// package to use instead, empty when they name none.</summary>
public sealed record PackageDeprecation(IReadOnlyList<string> Reasons, string Message, string AlternatePackage);

/// <summary>A vulnerability known in a version: how severe, from 0 (low) to 3 (critical), and where it is described.</summary>
public sealed record PackageVulnerability(int Severity, string AdvisoryUrl);

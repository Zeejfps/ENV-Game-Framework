namespace ZGF.ILSpy.Core;

/// <summary>
/// Where the packages taken from a feed are kept. One a restore already put in the global packages
/// folder is opened from there; any other is downloaded into a folder of the app's own, laid out the
/// same way, and kept there, so that a list holding it opens again with no feed to reach.
/// </summary>
public sealed class PackageCache(string globalPackagesFolder, string downloadFolder)
{
    /// <summary>The folder the app downloads packages into for the user running it.</summary>
    public static string UserDownloadFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZGF.ILSpy", "packages");

    /// <summary>The package file of <paramref name="id"/> at <paramref name="version"/> already on disk,
    /// or null.</summary>
    public string? Find(PackageId id, PackageVersion version)
    {
        var restored = PathIn(globalPackagesFolder, id, version);
        if (File.Exists(restored))
            return restored;
        var downloaded = PathIn(downloadFolder, id, version);
        if (File.Exists(downloaded))
            return downloaded;
        return null;
    }

    /// <summary>The package file of <paramref name="id"/> at <paramref name="version"/>: the one on
    /// disk, else one downloaded from <paramref name="feed"/> now.</summary>
    /// <exception cref="HttpRequestException">The feed could not be reached, or has no such package.</exception>
    /// <exception cref="IOException">The package could not be written.</exception>
    public async Task<string> FetchAsync(NuGetFeed feed, PackageId id, PackageVersion version, CancellationToken cancellationToken = default)
    {
        var found = Find(id, version);
        if (found != null)
            return found;
        var path = PathIn(downloadFolder, id, version);
        await feed.DownloadAsync(id, version, path, cancellationToken).ConfigureAwait(false);
        return path;
    }

    /// <summary>Where a package sits in a folder laid out as the global packages folder is:
    /// <c>{id}/{version}/{id}.{version}.nupkg</c>, in lower case.</summary>
    private static string PathIn(string folder, PackageId id, PackageVersion version)
    {
        var lowerId = id.Lower;
        var lowerVersion = version.Filed;
        return Path.Combine(folder, lowerId, lowerVersion, $"{lowerId}.{lowerVersion}.nupkg");
    }
}

namespace ZGF.ILSpy.Core;

/// <summary>The feeds packages are searched in, in the order the user's config lists them, and the
/// cache the packages picked are fetched into.</summary>
public sealed record NuGetSetup(IReadOnlyList<NuGetFeed> Feeds, PackageCache Cache)
{
    /// <summary>The setup the user's NuGet.Config describes, downloading into the app's own folder.</summary>
    public static NuGetSetup ReadUser()
    {
        var config = NuGetConfig.ReadUser();
        var feeds = config.Sources
            .Select(s => new NuGetFeed(s))
            .ToList();
        var cache = new PackageCache(config.GlobalPackagesFolder, PackageCache.UserDownloadFolder);
        return new NuGetSetup(feeds, cache);
    }
}

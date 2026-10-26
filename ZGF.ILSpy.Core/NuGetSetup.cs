namespace ZGF.ILSpy.Core;

/// <summary>The feeds the user's config lists, in its order, to search in or to pick from; one feed
/// per URL, so each service index is read once; where the packages' icons come from; and the cache
/// the packages picked are fetched into.</summary>
public sealed class NuGetSetup
{
    private readonly HttpMessageHandler? _handler;
    private readonly Dictionary<string, NuGetFeed> _feeds = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="handler">Sends the feeds' requests; the app's shared connections by default.</param>
    public NuGetSetup(IReadOnlyList<NuGetSource> sources, PackageCache cache, HttpMessageHandler? handler = null)
    {
        Sources = sources;
        Cache = cache;
        Icons = new PackageIcons(handler);
        _handler = handler;
    }

    public IReadOnlyList<NuGetSource> Sources { get; }

    public PackageCache Cache { get; }

    public PackageIcons Icons { get; }

    /// <summary>The setup the user's NuGet.Config describes, downloading into the app's own folder.</summary>
    public static NuGetSetup ReadUser() => FromConfig(NuGetConfig.ReadUser());

    /// <summary>The setup when the user's config cannot be read at all: nuget.org and the default folders.</summary>
    public static NuGetSetup CreateDefault() => FromConfig(NuGetConfig.CreateDefault());

    private static NuGetSetup FromConfig(NuGetConfig config)
    {
        var cache = new PackageCache(config.GlobalPackagesFolder, PackageCache.UserDownloadFolder);
        return new NuGetSetup(config.Sources, cache);
    }

    /// <summary>The feed at <paramref name="source"/>'s URL, made the first time it is asked for, named
    /// as the config names it when the config lists it. Call it from one thread only.</summary>
    public NuGetFeed FeedAt(NuGetSource source)
    {
        if (_feeds.TryGetValue(source.Url, out var feed))
            return feed;
        var named = NameAsListed(source);
        feed = new NuGetFeed(named, _handler);
        _feeds.Add(source.Url, feed);
        return feed;
    }

    private NuGetSource NameAsListed(NuGetSource source)
    {
        var listed = Sources.FirstOrDefault(s => string.Equals(s.Url, source.Url, StringComparison.OrdinalIgnoreCase));
        if (listed == null)
            return source;
        return listed;
    }
}

using ZGF.Svg;

namespace ZGF.Gui;

public sealed class SvgImageCacheOptions
{
    /// <summary>Byte budget for cached rasterizations; least-recently-used entries evict past it.</summary>
    public long MaxRasterBytes { get; init; } = 16 * 1024 * 1024;
}

/// <summary>
/// App-wide cache behind the <see cref="Widgets.Svg"/> widget: parsed documents keyed by
/// source, rasterizations keyed by (document, pixel size, currentColor) and uploaded as
/// dynamic canvas images under synthetic ids. Views sharing an icon at one size share one
/// texture, so their draws batch. Registered as a context service by the host
/// (<c>GuiApp</c> / test harness); the options parameter is required so a missed
/// registration fails loudly instead of silently constructing per-widget transients.
/// </summary>
public sealed class SvgImageCache
{
    /// <summary>Entries younger than this never evict, so a texture drawn by an in-flight frame stays alive.</summary>
    private const long MinIdleMsBeforeEvict = 500;

    private readonly long _maxBytes;
    private readonly Dictionary<string, SvgDocument> _documentsByPath = new();
    private readonly Dictionary<ulong, SvgDocument> _documentsByContentHash = new();
    private readonly Dictionary<SvgDocument, int> _documentIds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<RasterKey, Entry> _rasters = new();
    private readonly List<RasterKey> _evictionScratch = new();
    private readonly SvgRasterizer _rasterizer = new();
    private byte[] _pixelScratch = [];
    private int _nextDocumentId = 1;
    private long _totalBytes;

    private readonly record struct RasterKey(int DocumentId, int WidthPx, int HeightPx, uint ColorArgb);

    private sealed class Entry
    {
        public required string ImageId { get; init; }
        public required int Bytes { get; init; }
        public long LastUsedMs;
    }

    /// <summary>Total rasterizations performed — lets tests assert re-raster policy.</summary>
    public int RasterCount { get; private set; }

    public SvgImageCache(SvgImageCacheOptions options)
    {
        _maxBytes = options.MaxRasterBytes;
    }

    public SvgDocument GetOrParseFile(string resolvedPath)
    {
        if (!_documentsByPath.TryGetValue(resolvedPath, out var document))
        {
            document = SvgDocument.Parse(File.ReadAllText(resolvedPath));
            _documentsByPath.Add(resolvedPath, document);
        }
        return document;
    }

    public SvgDocument GetOrParse(byte[] svgData)
    {
        var hash = Fnv1A64(svgData);
        if (!_documentsByContentHash.TryGetValue(hash, out var document))
        {
            document = SvgDocument.Parse(svgData);
            _documentsByContentHash.Add(hash, document);
        }
        return document;
    }

    /// <summary>
    /// Returns the canvas image id for the document rasterized at the given pixel size and
    /// currentColor, rasterizing and uploading on miss. The entry is cached even when the
    /// canvas has no image support (upload returns false), so headless runs still exercise
    /// the same raster policy the GPU backends do.
    /// </summary>
    public string Acquire(ICanvas canvas, SvgDocument document, int widthPx, int heightPx, uint currentColorArgb)
    {
        // Documents that never reference currentColor rasterize identically for any color:
        // collapse the key so themed variants share one texture.
        var colorKey = document.UsesCurrentColor ? currentColorArgb : 0xFF000000;
        var key = new RasterKey(GetDocumentId(document), widthPx, heightPx, colorKey);
        var now = Environment.TickCount64;

        if (_rasters.TryGetValue(key, out var entry))
        {
            entry.LastUsedMs = now;
            return entry.ImageId;
        }

        var byteCount = widthPx * heightPx * 4;
        if (_pixelScratch.Length < byteCount)
            _pixelScratch = new byte[byteCount];
        var pixels = _pixelScratch.AsSpan(0, byteCount);
        _rasterizer.Rasterize(document, pixels, widthPx, heightPx, colorKey);
        RasterCount++;

        var imageId = $"svg:{key.DocumentId}@{widthPx}x{heightPx}#{colorKey:X8}";
        canvas.CreateOrUpdateRgbaImage(imageId, widthPx, heightPx, pixels);

        _rasters.Add(key, new Entry { ImageId = imageId, Bytes = byteCount, LastUsedMs = now });
        _totalBytes += byteCount;
        EvictIfOverBudget(canvas, now);
        return imageId;
    }

    private int GetDocumentId(SvgDocument document)
    {
        if (!_documentIds.TryGetValue(document, out var id))
        {
            id = _nextDocumentId++;
            _documentIds.Add(document, id);
        }
        return id;
    }

    private void EvictIfOverBudget(ICanvas canvas, long now)
    {
        if (_totalBytes <= _maxBytes)
            return;

        _evictionScratch.Clear();
        foreach (var (key, entry) in _rasters)
        {
            if (now - entry.LastUsedMs >= MinIdleMsBeforeEvict)
                _evictionScratch.Add(key);
        }
        _evictionScratch.Sort((a, b) => _rasters[a].LastUsedMs.CompareTo(_rasters[b].LastUsedMs));

        foreach (var key in _evictionScratch)
        {
            if (_totalBytes <= _maxBytes)
                break;
            if (_rasters.Remove(key, out var entry))
            {
                _totalBytes -= entry.Bytes;
                canvas.RemoveImage(entry.ImageId);
            }
        }
    }

    private static ulong Fnv1A64(ReadOnlySpan<byte> data)
    {
        var hash = 14695981039346656037ul;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= 1099511628211ul;
        }
        return hash;
    }
}

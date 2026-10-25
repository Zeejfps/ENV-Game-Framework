using ZGF.Svg;

namespace ZGF.Gui.Immediate;

/// <summary>The SVG icons drawn so far, each rasterized and uploaded once per picture, pixel size and
/// color under an image id of its own, so drawing one again allocates nothing. A picture that does not
/// use <c>currentColor</c> is kept once for every color. An image not drawn for
/// <see cref="LifetimeFrames"/> frames is freed, and every one a frame did not draw once
/// <see cref="ReleaseUndrawn"/> asked for it.</summary>
internal sealed class IconImages
{
    private const string ImageIdPrefix = "icon-svg:";
    private const uint OwnColors = 0u;
    private const int BytesPerPixel = 4;
    private const long LifetimeFrames = 600;
    private const long SweepInterval = 128;

    private readonly record struct Key(SvgDocument Svg, int Pixels, uint Color);

    private sealed class Entry(string imageId)
    {
        public string ImageId { get; } = imageId;
        public long LastFrame;
    }

    private readonly Dictionary<Key, Entry> _entries = new();
    private readonly List<Key> _expired = new();
    private readonly SvgRasterizer _rasterizer = new();
    private byte[] _pixels = [];
    private int _nextImage;
    private bool _isReleasePending;

    /// <summary>How many times a picture was rasterized, for tests to tell a reuse from a redraw.</summary>
    public int RasterCount { get; private set; }

    /// <summary>The id of the image of <paramref name="svg"/> at <paramref name="pixels"/> pixels square in
    /// <paramref name="color"/>, rasterizing and uploading it to <paramref name="canvas"/> the first time.</summary>
    public string Acquire(ICanvas canvas, SvgDocument svg, int pixels, uint color, long frame)
    {
        var key = new Key(svg, pixels, ColorKeyOf(svg, color));
        if (!_entries.TryGetValue(key, out var entry))
        {
            entry = new Entry(Rasterize(canvas, key));
            _entries[key] = entry;
        }
        entry.LastFrame = frame;
        return entry.ImageId;
    }

    private static uint ColorKeyOf(SvgDocument svg, uint color) => svg.UsesCurrentColor ? color : OwnColors;

    private string Rasterize(ICanvas canvas, Key key)
    {
        var byteCount = key.Pixels * key.Pixels * BytesPerPixel;
        if (_pixels.Length < byteCount)
            _pixels = new byte[byteCount];
        var pixels = _pixels.AsSpan(0, byteCount);
        _rasterizer.Rasterize(key.Svg, pixels, key.Pixels, key.Pixels, key.Color);
        RasterCount++;
        var imageId = ImageIdPrefix + _nextImage;
        _nextImage++;
        canvas.CreateOrUpdateRgbaImage(imageId, key.Pixels, key.Pixels, pixels);
        return imageId;
    }

    /// <summary>Frees, once this frame is drawn, every image it did not draw.</summary>
    public void ReleaseUndrawn()
    {
        _isReleasePending = true;
    }

    /// <summary>Frees the images that are no longer drawn, as of the end of <paramref name="frame"/>.</summary>
    public void EndFrame(ICanvas canvas, long frame)
    {
        if (_isReleasePending)
        {
            _isReleasePending = false;
            Release(canvas, frame);
            return;
        }
        if (frame % SweepInterval == 0)
            Release(canvas, frame - LifetimeFrames);
    }

    /// <summary>Frees the images last drawn before <paramref name="since"/>.</summary>
    private void Release(ICanvas canvas, long since)
    {
        _expired.Clear();
        foreach (var (key, entry) in _entries)
        {
            if (entry.LastFrame < since)
                _expired.Add(key);
        }
        foreach (var key in _expired)
            Remove(canvas, key);
    }

    private void Remove(ICanvas canvas, Key key)
    {
        _entries.Remove(key, out var entry);
        canvas.RemoveImage(entry!.ImageId);
    }
}

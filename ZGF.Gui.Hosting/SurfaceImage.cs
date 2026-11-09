using PngSharp.Api;

namespace ZGF.Gui.Hosting;

/// <summary>Pixels read back from a window's offscreen target: RGBA8, rows top-down, premultiplied alpha.</summary>
public sealed class SurfaceImage
{
    private readonly byte[] _premultiplied;

    internal SurfaceImage(int width, int height, byte[] premultiplied)
    {
        if (premultiplied.Length != width * height * 4)
            throw new ArgumentException($"Expected {width * height * 4} bytes for {width}x{height}, got {premultiplied.Length}.", nameof(premultiplied));
        Width = width;
        Height = height;
        _premultiplied = premultiplied;
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>All pixels, premultiplied, rows top-down.</summary>
    public ReadOnlySpan<byte> Premultiplied => _premultiplied;

    /// <summary>The premultiplied pixel at column <paramref name="x"/> of row <paramref name="y"/>, counted from the top.</summary>
    public (byte R, byte G, byte B, byte A) this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
            var i = (y * Width + x) * 4;
            return (_premultiplied[i], _premultiplied[i + 1], _premultiplied[i + 2], _premultiplied[i + 3]);
        }
    }

    /// <summary>The pixels with alpha divided back out, as image files store them.</summary>
    public byte[] ToStraightAlpha()
    {
        var straight = new byte[_premultiplied.Length];
        for (var i = 0; i < straight.Length; i += 4)
        {
            var a = _premultiplied[i + 3];
            if (a == 0) continue;
            straight[i] = Unpremultiply(_premultiplied[i], a);
            straight[i + 1] = Unpremultiply(_premultiplied[i + 1], a);
            straight[i + 2] = Unpremultiply(_premultiplied[i + 2], a);
            straight[i + 3] = a;
        }
        return straight;
    }

    /// <summary>Writes a straight-alpha PNG, creating the directory if needed.</summary>
    public void SavePng(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        Png.EncodeToFile(Png.CreateRgba(Width, Height, ToStraightAlpha()), path);
    }

    /// <summary>A straight-alpha PNG, encoded for speed rather than size, for a capture read once.</summary>
    public byte[] ToPng() => FastPng.Encode(Width, Height, ToStraightAlpha());

    private static byte Unpremultiply(byte channel, byte alpha) =>
        (byte)Math.Min(255, (channel * 255 + alpha / 2) / alpha);
}

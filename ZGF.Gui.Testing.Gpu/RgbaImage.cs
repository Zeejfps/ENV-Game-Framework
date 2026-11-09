using PngSharp.Api;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>One RGBA8 pixel.</summary>
public readonly record struct Rgba8(byte R, byte G, byte B, byte A);

/// <summary>Pixels read back from a target: RGBA8, rows top-down, premultiplied alpha.</summary>
public sealed class RgbaImage
{
    private readonly byte[] _premultiplied;

    internal RgbaImage(int width, int height, byte[] premultiplied)
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
    public Rgba8 this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
            var i = (y * Width + x) * 4;
            return new Rgba8(_premultiplied[i], _premultiplied[i + 1], _premultiplied[i + 2], _premultiplied[i + 3]);
        }
    }

    /// <summary>The <paramref name="width"/> by <paramref name="height"/> pixels from column <paramref name="x"/> of
    /// row <paramref name="y"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The region is empty or not inside the image.</exception>
    public RgbaImage Crop(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(x + width, Width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(y + height, Height, nameof(height));
        var pixels = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
            Array.Copy(_premultiplied, ((y + row) * Width + x) * 4, pixels, row * width * 4, width * 4);
        return new RgbaImage(width, height, pixels);
    }

    /// <summary>Each pixel as a <paramref name="factor"/> by <paramref name="factor"/> block.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The factor is not positive, or the result would not fit in one array.</exception>
    public RgbaImage Magnify(int factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);
        if ((long)Width * Height * 4 * factor * factor > Array.MaxLength)
            throw new ArgumentOutOfRangeException(nameof(factor), $"{Width}x{Height} magnified {factor} times does not fit in one array.");
        var width = Width * factor;
        var pixels = new byte[width * Height * factor * 4];
        for (var y = 0; y < Height * factor; y++)
        for (var x = 0; x < width; x++)
            Array.Copy(_premultiplied, ((y / factor) * Width + x / factor) * 4, pixels, (y * width + x) * 4, 4);
        return new RgbaImage(width, Height * factor, pixels);
    }

    /// <summary>This image with <paramref name="top"/> drawn over it source-over, its top-left at column
    /// <paramref name="x"/> of row <paramref name="y"/>; what falls outside is cut off.</summary>
    public RgbaImage Overlaid(RgbaImage top, int x, int y)
    {
        var pixels = _premultiplied.ToArray();
        var left = Math.Max(0, x);
        var right = Math.Min(Width, x + top.Width);
        var upper = Math.Max(0, y);
        var lower = Math.Min(Height, y + top.Height);
        for (var row = upper; row < lower; row++)
        for (var column = left; column < right; column++)
        {
            var d = (row * Width + column) * 4;
            var s = ((row - y) * top.Width + column - x) * 4;
            var keep = 255 - top._premultiplied[s + 3];
            for (var c = 0; c < 4; c++)
                pixels[d + c] = (byte)Math.Min(255, top._premultiplied[s + c] + (pixels[d + c] * keep + 127) / 255);
        }
        return new RgbaImage(Width, Height, pixels);
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

    private static byte Unpremultiply(byte channel, byte alpha) =>
        (byte)Math.Min(255, (channel * 255 + alpha / 2) / alpha);
}

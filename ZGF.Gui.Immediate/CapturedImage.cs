using ZGF.Gui.Hosting;

namespace ZGF.Gui.Immediate;

/// <summary>Captured pixels being put together for a capture: RGBA8, rows top-down, premultiplied alpha.</summary>
internal sealed class CapturedImage
{
    private readonly byte[] _pixels;

    private CapturedImage(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }

    public static CapturedImage Of(SurfaceImage image) => new(image.Width, image.Height, image.Premultiplied.ToArray());

    /// <summary>This image with <paramref name="top"/> drawn over it, its top-left at (<paramref name="left"/>,
    /// <paramref name="topEdge"/>), cut to this image.</summary>
    public CapturedImage Overlaid(CapturedImage top, int left, int topEdge)
    {
        var pixels = (byte[])_pixels.Clone();
        for (var y = Math.Max(0, topEdge); y < Math.Min(Height, topEdge + top.Height); y++)
        {
            for (var x = Math.Max(0, left); x < Math.Min(Width, left + top.Width); x++)
            {
                var s = ((y - topEdge) * top.Width + (x - left)) * 4;
                var d = (y * Width + x) * 4;
                var keep = 255 - top._pixels[s + 3];
                for (var c = 0; c < 4; c++)
                    pixels[d + c] = (byte)Math.Min(255, top._pixels[s + c] + (pixels[d + c] * keep + 127) / 255);
            }
        }
        return new CapturedImage(Width, Height, pixels);
    }

    public CapturedImage Crop(int left, int top, int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
            Array.Copy(_pixels, ((top + y) * Width + left) * 4, pixels, y * width * 4, width * 4);
        return new CapturedImage(width, height, pixels);
    }

    /// <summary>Each pixel <paramref name="factor"/> times as wide and high.</summary>
    public CapturedImage Magnify(int factor)
    {
        if (factor == 1)
            return this;
        var width = Width * factor;
        var height = Height * factor;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                Array.Copy(_pixels, ((y / factor) * Width + x / factor) * 4, pixels, (y * width + x) * 4, 4);
        }
        return new CapturedImage(width, height, pixels);
    }

    public byte[] ToPng() => new SurfaceImage(Width, Height, _pixels).ToPng();

    /// <summary>The width and height a PNG's header gives.</summary>
    /// <exception cref="FormatException">It is no PNG.</exception>
    public static (int Width, int Height) PngSize(byte[] png)
    {
        ReadOnlySpan<byte> signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        if (png.Length < 24 || !png.AsSpan(0, 8).SequenceEqual(signature))
            throw new FormatException("Not a PNG.");
        return (System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)),
            System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
    }
}

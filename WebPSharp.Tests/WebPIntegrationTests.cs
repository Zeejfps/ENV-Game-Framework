using WebPSharp.Api;

namespace WebPSharp.Tests;

// Integration tests over realistic (non-random) image content that exercises the predictor,
// palette, and LZ77 machinery the way real photographs and graphics do.
public class WebPIntegrationTests
{
    private const int Size = 128;

    private static byte Clamp(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

    private static WebPImage Photographic()
    {
        // A smooth plasma-like field: strong spatial correlation, ideal for the predictor transform.
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var i = (y * Size + x) * 4;
            var r = 128 + 96 * Sin(x * 0.10) * Cos(y * 0.07);
            var g = 128 + 96 * Sin((x + y) * 0.06);
            var b = 128 + 96 * Cos(x * 0.05) * Sin(y * 0.09);
            pixels[i] = Clamp((int)r);
            pixels[i + 1] = Clamp((int)g);
            pixels[i + 2] = Clamp((int)b);
            pixels[i + 3] = 255;
        }
        return WebPImage.CreateRgba(Size, Size, pixels);

        static double Sin(double v) => Approx(v, true);
        static double Cos(double v) => Approx(v, false);
        // Deterministic, allocation-free trig approximation (avoids Math for full determinism).
        static double Approx(double v, bool sin)
        {
            if (!sin) v += Math.PI / 2;
            v %= 2 * Math.PI;
            if (v < 0) v += 2 * Math.PI;
            v -= Math.PI;
            var v2 = v * v;
            var s = v * (1 - v2 / 6 * (1 - v2 / 20 * (1 - v2 / 42)));
            return -s;
        }
    }

    private static WebPImage Graphic()
    {
        // A UI-like mockup: flat rectangular regions with borders — few colors, large runs.
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var i = (y * Size + x) * 4;
            var block = ((x / 16) + (y / 16)) % 3;
            var (r, g, b) = block switch
            {
                0 => (240, 240, 245),
                1 => (60, 120, 200),
                _ => (30, 30, 40),
            };
            var border = x % 16 == 0 || y % 16 == 0;
            if (border) { r = g = b = 0; }
            pixels[i] = (byte)r; pixels[i + 1] = (byte)g; pixels[i + 2] = (byte)b; pixels[i + 3] = 255;
        }
        return WebPImage.CreateRgba(Size, Size, pixels);
    }

    private static WebPImage Texture()
    {
        // A repeating tile: strong LZ77 back-references.
        var tile = new byte[8 * 8 * 4];
        new Random(7).NextBytes(tile);
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var src = ((y % 8) * 8 + (x % 8)) * 4;
            var dst = (y * Size + x) * 4;
            Array.Copy(tile, src, pixels, dst, 4);
        }
        return WebPImage.CreateRgba(Size, Size, pixels);
    }

    public static IEnumerable<object[]> Cases()
    {
        yield return new object[] { "photographic" };
        yield return new object[] { "graphic" };
        yield return new object[] { "texture" };
    }

    private static WebPImage Build(string kind) => kind switch
    {
        "photographic" => Photographic(),
        "graphic" => Graphic(),
        _ => Texture(),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void StructuredContent_RoundTripsExactly(string kind)
    {
        var image = Build(kind);
        foreach (var effort in new[] { 0, 4, 9 })
        {
            var encoded = WebP.Encode(image, new WebPEncoderOptions { Effort = effort });
            var decoded = WebP.Decode(encoded);
            Assert.Equal(image.PixelData, decoded.PixelData);
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void StructuredContent_CompressesWell(string kind)
    {
        var image = Build(kind);
        var raw = image.PixelData.Length;
        var encoded = WebP.Encode(image, new WebPEncoderOptions { Effort = 9 });
        // Structured content should compress to well under half its raw RGBA size.
        Assert.True(encoded.Length < raw / 2,
            $"{kind}: encoded {encoded.Length} should be < half of raw {raw}.");
    }

    [Fact]
    public void GraphicContent_CompressesDramatically()
    {
        // Few-color UI content should shrink to a small fraction via palette/LZ77.
        var image = Graphic();
        var encoded = WebP.Encode(image, new WebPEncoderOptions { Effort = 9 });
        Assert.True(encoded.Length < image.PixelData.Length / 10,
            $"graphic encoded {encoded.Length} should be < 10% of raw {image.PixelData.Length}.");
    }
}

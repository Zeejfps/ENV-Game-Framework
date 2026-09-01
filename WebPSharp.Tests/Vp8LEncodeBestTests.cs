using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LEncodeBestTests
{
    private static WebPImage Gradient(int w, int h)
    {
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            pixels[i] = (byte)(x * 2);
            pixels[i + 1] = (byte)(y * 2);
            pixels[i + 2] = (byte)(x + y);
            pixels[i + 3] = 255;
        }
        return WebPImage.CreateRgba(w, h, pixels);
    }

    private static WebPImage FewColors(int w, int h, int colors, int seed)
    {
        var rng = new Random(seed);
        var palette = new uint[colors];
        for (var i = 0; i < colors; i++)
            palette[i] = (uint)rng.NextInt64(0, 1L << 32);
        var pixels = new byte[w * h * 4];
        for (var p = 0; p < w * h; p++)
        {
            var c = palette[rng.Next(colors)];
            pixels[p * 4] = (byte)(c >> 16);
            pixels[p * 4 + 1] = (byte)(c >> 8);
            pixels[p * 4 + 2] = (byte)c;
            pixels[p * 4 + 3] = (byte)(c >> 24);
        }
        return WebPImage.CreateRgba(w, h, pixels);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(9)]
    public void EncodeBest_RoundTrips(int effort)
    {
        var image = Gradient(40, 30);
        var payload = Vp8LEncoder.EncodeBest(image, effort);
        var decoded = Vp8LDecoder.Decode(payload);
        Assert.Equal(image.PixelData, decoded.PixelData);
    }

    [Fact]
    public void EncodeBest_HigherEffort_NotLargerThanBaseline()
    {
        var image = Gradient(64, 64);
        var baseline = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        var best = Vp8LEncoder.EncodeBest(image, 9);
        Assert.True(best.Length <= baseline.Length,
            $"best ({best.Length}) should not exceed the LZ77-only baseline ({baseline.Length}).");
    }

    [Fact]
    public void EncodeBest_Gradient_BeatsBaseline()
    {
        // A smooth gradient is well-predicted, so the predictor transform should shrink it.
        var image = Gradient(96, 96);
        var baseline = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        var best = Vp8LEncoder.EncodeBest(image, 6);
        Assert.True(best.Length < baseline.Length,
            $"gradient best ({best.Length}) should beat baseline ({baseline.Length}).");
    }

    [Fact]
    public void EncodeBest_FewColors_RoundTripsAndNeverWorseThanBaseline()
    {
        var image = FewColors(48, 48, colors: 8, seed: 3);
        var baseline = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        var best = Vp8LEncoder.EncodeBest(image, 4);
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(best).PixelData);
        Assert.True(best.Length <= baseline.Length,
            $"best ({best.Length}) must never exceed baseline ({baseline.Length}).");
    }

    [Fact]
    public void EncodeBest_LargeFlatPalette_BeatsBaseline()
    {
        // A large image tiled from a tiny palette: the color-index transform's bundling wins big.
        var image = FewColors(128, 128, colors: 4, seed: 5);
        var baseline = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        var best = Vp8LEncoder.EncodeBest(image, 4);
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(best).PixelData);
        Assert.True(best.Length < baseline.Length,
            $"palette best ({best.Length}) should beat baseline ({baseline.Length}).");
    }

    [Fact]
    public void EncodeBest_ManyColors_StillRoundTrips()
    {
        // Random noise has >256 colors so the palette candidate is skipped without error.
        var rng = new Random(1);
        var pixels = new byte[40 * 40 * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(40, 40, pixels);
        var best = Vp8LEncoder.EncodeBest(image, 9);
        Assert.Equal(pixels, Vp8LDecoder.Decode(best).PixelData);
    }

    [Fact]
    public void EncodeBest_IsDeterministic()
    {
        var image = Gradient(50, 50);
        Assert.Equal(Vp8LEncoder.EncodeBest(image, 6), Vp8LEncoder.EncodeBest(image, 6));
    }

    [Fact]
    public void PublicEncode_UsesEffort_AndRoundTrips()
    {
        var image = Gradient(80, 60);
        var small = WebP.Encode(image, new WebPEncoderOptions { Effort = 9 });
        Assert.Equal(image.PixelData, WebP.Decode(small).PixelData);
    }
}

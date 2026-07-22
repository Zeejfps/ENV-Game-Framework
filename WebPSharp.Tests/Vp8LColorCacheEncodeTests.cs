using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LColorCacheEncodeTests
{
    private static WebPImage Noise(int w, int h, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(w, h, pixels);
    }

    private static WebPImage Recurring(int w, int h, int colors, int seed)
    {
        var rng = new Random(seed);
        var palette = new uint[colors];
        for (var i = 0; i < colors; i++) palette[i] = (uint)rng.NextInt64(0, 1L << 32);
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
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(11)]
    public void ColorCache_RoundTripsNoise(int cacheBits)
    {
        var image = Noise(20, 16, cacheBits);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = cacheBits });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void ColorCache_WithoutLz77_RoundTrips()
    {
        var image = Recurring(24, 24, 12, 3);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = false, ColorCacheBits = 6 });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void ColorCache_SolidColor_RoundTrips()
    {
        var pixels = new byte[16 * 16 * 4];
        for (var i = 0; i < pixels.Length; i += 4) { pixels[i] = 5; pixels[i + 1] = 9; pixels[i + 2] = 13; pixels[i + 3] = 255; }
        var image = WebPImage.CreateRgba(16, 16, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = 8 });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void ColorCache_FullyTransparent_RoundTrips()
    {
        var image = WebPImage.CreateRgba(12, 12, new byte[12 * 12 * 4]); // all zero
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = 4 });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void ColorCache_WithTransform_RoundTrips()
    {
        var image = Recurring(32, 20, 20, 7);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = 8, Predictor = true, PredictorMode = 2 });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void ColorCache_RecurringColors_HelpsOrEqualsBaseline()
    {
        var image = Recurring(64, 64, 24, 11);
        var baseline = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        var cached = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = 8 });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(cached).PixelData);
        // Cache should not make recurring-color content larger by much; typically it helps.
        Assert.True(cached.Length <= baseline.Length + 8);
    }

    [Fact]
    public void ColorCache_Deterministic()
    {
        var image = Noise(30, 30, 5);
        var a = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = 8 });
        var b = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = 8 });
        Assert.Equal(a, b);
    }
}

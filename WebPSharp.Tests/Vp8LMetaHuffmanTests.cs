using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LMetaHuffmanTests
{
    private static WebPImage RandomRgba(int width, int height, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[width * height * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(width, height, pixels);
    }

    [Theory]
    [InlineData(32, 32, 2)]
    [InlineData(32, 32, 4)]
    [InlineData(40, 24, 3)]
    [InlineData(17, 19, 4)] // odd sizes, partial tiles
    public void MetaHuffman_RoundTripsNoise(int width, int height, int groups)
    {
        var image = RandomRgba(width, height, width * 13 + height + groups);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings
        {
            MetaHuffman = true,
            MetaHuffmanBits = 3,
            MetaHuffmanGroups = groups,
        });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void MetaHuffman_RoundTripsGradient()
    {
        const int w = 48, h = 40;
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            pixels[i] = (byte)(x * 5);
            pixels[i + 1] = (byte)(y * 6);
            pixels[i + 2] = (byte)(x * y);
            pixels[i + 3] = 255;
        }
        var image = WebPImage.CreateRgba(w, h, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { MetaHuffman = true, MetaHuffmanGroups = 4 });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void MetaHuffman_WithPredictor_RoundTrips()
    {
        const int w = 40, h = 40;
        var image = RandomRgba(w, h, 999);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings
        {
            MetaHuffman = true,
            MetaHuffmanGroups = 3,
            Predictor = true,
            PredictorMode = 2,
        });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void MetaHuffman_SingleTile_DegradesToOneGroup()
    {
        // Small image with bits large enough that only one tile exists.
        var image = RandomRgba(4, 4, 3);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings
        {
            MetaHuffman = true,
            MetaHuffmanBits = 9,
            MetaHuffmanGroups = 4,
        });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }
}

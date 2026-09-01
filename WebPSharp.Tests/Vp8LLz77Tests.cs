using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LLz77Tests
{
    private static WebPImage RandomRgba(int width, int height, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[width * height * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(width, height, pixels);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(1, 33)]
    [InlineData(33, 1)]
    [InlineData(19, 13)]
    [InlineData(64, 40)]
    public void Lz77_RoundTripsNoise(int width, int height)
    {
        var image = RandomRgba(width, height, width * 191 + height);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        Assert.Equal(image.PixelData, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void Lz77_RoundTripsSolidColor()
    {
        var pixels = new byte[40 * 30 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 111; pixels[i + 1] = 22; pixels[i + 2] = 200; pixels[i + 3] = 255;
        }
        var image = WebPImage.CreateRgba(40, 30, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void Lz77_RoundTripsRepeatingPattern()
    {
        const int w = 64, h = 64;
        var pattern = new byte[] { 10, 20, 30, 255, 40, 50, 60, 255, 70, 80, 90, 255 };
        var pixels = new byte[w * h * 4];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = pattern[i % pattern.Length];
        var image = WebPImage.CreateRgba(w, h, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void Lz77_CompressesRepetitiveDataBetterThanLiteral()
    {
        // A high-entropy 256-pixel block tiled across the image: literals cost ~1 byte/channel,
        // while LZ77 replaces every repeat with a single copy.
        const int w = 128, h = 128;
        var rng = new Random(7);
        var block = new byte[256 * 4];
        rng.NextBytes(block);
        var pixels = new byte[w * h * 4];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = block[i % block.Length];
        var image = WebPImage.CreateRgba(w, h, pixels);

        var literal = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = false });
        var lz77 = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });

        Assert.True(lz77.Length < literal.Length,
            $"LZ77 ({lz77.Length}) should beat literal ({literal.Length}) on repetitive data.");
    }

    [Fact]
    public void Lz77_WithPredictor_RoundTrips()
    {
        const int w = 48, h = 32;
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            pixels[i] = (byte)(x * 4);
            pixels[i + 1] = (byte)(y * 4);
            pixels[i + 2] = (byte)(x + y);
            pixels[i + 3] = 255;
        }
        var image = WebPImage.CreateRgba(w, h, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = 2 });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void Lz77_IsDeterministic()
    {
        var image = RandomRgba(50, 50, 12345);
        var a = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        var b = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true });
        Assert.Equal(a, b);
    }
}

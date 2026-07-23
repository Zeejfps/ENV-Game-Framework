using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LTransformRoundTripTests
{
    private static void AssertRoundTripsWithSubtractGreen(WebPImage image)
    {
        var payload = Vp8LEncoder.Encode(image, subtractGreen: true);
        var decoded = Vp8LDecoder.Decode(payload);

        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
        Assert.Equal(ToRgba(image), decoded.PixelData);
    }

    private static byte[] ToRgba(WebPImage image)
    {
        if (image.Format == WebPColorFormat.Rgba)
            return image.PixelData;
        var rgba = new byte[image.Width * image.Height * 4];
        var src = image.PixelData;
        for (int i = 0, j = 0; i < src.Length; i += 3, j += 4)
        {
            rgba[j] = src[i];
            rgba[j + 1] = src[i + 1];
            rgba[j + 2] = src[i + 2];
            rgba[j + 3] = 255;
        }
        return rgba;
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(16, 16)]
    [InlineData(31, 7)]
    public void SubtractGreen_RoundTripsNoise(int width, int height)
    {
        var rng = new Random(width * 131 + height);
        var pixels = new byte[width * height * 4];
        rng.NextBytes(pixels);
        AssertRoundTripsWithSubtractGreen(WebPImage.CreateRgba(width, height, pixels));
    }

    [Fact]
    public void SubtractGreen_RoundTripsGradient()
    {
        const int w = 24, h = 24;
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            pixels[i] = (byte)(x * 10);
            pixels[i + 1] = (byte)(y * 10);
            pixels[i + 2] = (byte)((x + y) * 5);
            pixels[i + 3] = 255;
        }
        AssertRoundTripsWithSubtractGreen(WebPImage.CreateRgba(w, h, pixels));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public void Predictor_RoundTripsNoise(int mode)
    {
        const int w = 20, h = 15;
        var rng = new Random(300 + mode);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(w, h, pixels);

        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Predictor = true, PredictorMode = mode });
        var decoded = Vp8LDecoder.Decode(payload);

        Assert.Equal(pixels, decoded.PixelData);
    }

    [Fact]
    public void Predictor_RoundTripsGradient()
    {
        const int w = 48, h = 32;
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            pixels[i] = (byte)(x * 5);
            pixels[i + 1] = (byte)(y * 7);
            pixels[i + 2] = (byte)(x + y);
            pixels[i + 3] = 255;
        }
        var image = WebPImage.CreateRgba(w, h, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Predictor = true, PredictorMode = 12 });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 9)]
    [InlineData(9, 1)]
    public void Predictor_RoundTripsTinyAndOddShapes(int w, int h)
    {
        var rng = new Random(w * 17 + h);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(w, h, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Predictor = true, PredictorMode = 11, PredictorBits = 2 });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CombinedTransforms_RoundTrip(int seed)
    {
        const int w = 33, h = 21;
        var rng = new Random(seed);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(w, h, pixels);

        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings
        {
            Predictor = true,
            PredictorMode = 12,
            CrossColor = true,
            CrossColorGreenToRed = 20,
            CrossColorGreenToBlue = 10,
            CrossColorRedToBlue = 30,
            SubtractGreen = true,
        });

        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Theory]
    [InlineData(2, 16, 16)]   // bundling bits = 3 (8 px/byte)
    [InlineData(4, 15, 9)]    // bundling bits = 2 (4 px/byte), odd width
    [InlineData(16, 23, 7)]   // bundling bits = 1 (2 px/byte), odd width
    [InlineData(200, 20, 20)] // bundling bits = 0 (1 px/byte)
    public void Palette_RoundTrips(int numColors, int width, int height)
    {
        var rng = new Random(numColors * 100 + width);
        var colors = new uint[numColors];
        for (var i = 0; i < numColors; i++)
            colors[i] = (uint)rng.NextInt64(0, 1L << 32);

        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            var c = colors[rng.Next(numColors)];
            pixels[i * 4] = (byte)(c >> 16);
            pixels[i * 4 + 1] = (byte)(c >> 8);
            pixels[i * 4 + 2] = (byte)c;
            pixels[i * 4 + 3] = (byte)(c >> 24);
        }

        var image = WebPImage.CreateRgba(width, height, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Palette = true });
        var decoded = Vp8LDecoder.Decode(payload);

        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.Equal(pixels, decoded.PixelData);
    }

    [Fact]
    public void Palette_SingleColor_RoundTrips()
    {
        const int w = 9, h = 5;
        var pixels = new byte[w * h * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 30; pixels[i + 1] = 60; pixels[i + 2] = 90; pixels[i + 3] = 255;
        }
        var image = WebPImage.CreateRgba(w, h, pixels);
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Palette = true });
        Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
    }

    [Fact]
    public void Palette_TooManyColors_Throws()
    {
        // 300 distinct colors exceeds the 256-entry palette limit.
        var pixels = new byte[300 * 4];
        for (var i = 0; i < 300; i++)
        {
            pixels[i * 4] = (byte)i;        // low 8 bits
            pixels[i * 4 + 1] = (byte)(i >> 8); // high bits -> guarantees distinctness
            pixels[i * 4 + 2] = 0;
            pixels[i * 4 + 3] = 255;
        }
        var image = WebPImage.CreateRgba(300, 1, pixels);
        Assert.Throws<InvalidOperationException>(
            () => Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Palette = true }));
    }

    [Fact]
    public void SubtractGreen_ProducesIdentifiableContainer()
    {
        var rng = new Random(9);
        var pixels = new byte[8 * 8 * 4];
        rng.NextBytes(pixels);
        var payload = Vp8LEncoder.Encode(WebPImage.CreateRgba(8, 8, pixels), subtractGreen: true);
        var (width, height, _) = WebPSharp.Container.WebPHeaderReader.ReadVp8LDimensions(payload);
        Assert.Equal(8, width);
        Assert.Equal(8, height);
    }
}

using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class PackedPixelTests
{
    [Fact]
    public void Rgb_PacksChannelsPerFormat()
    {
        // Single pixel R=10, G=20, B=30.
        var image = JpegImage.CreateRgb(1, 1, [10, 20, 30]);

        Assert.Equal((10 << 24) | (20 << 16) | (30 << 8) | 255, image.ToPackedPixels(PackedPixelFormat.Rgba8888)[0]);
        Assert.Equal((255 << 24) | (10 << 16) | (20 << 8) | 30, image.ToPackedPixels(PackedPixelFormat.Argb8888)[0]);
        Assert.Equal((30 << 24) | (20 << 16) | (10 << 8) | 255, image.ToPackedPixels(PackedPixelFormat.Bgra8888)[0]);
        Assert.Equal((255 << 24) | (30 << 16) | (20 << 8) | 10, image.ToPackedPixels(PackedPixelFormat.Abgr8888)[0]);
    }

    [Fact]
    public void NamedExtensions_MatchExplicitFormat()
    {
        var image = JpegImage.CreateRgb(1, 1, [10, 20, 30]);

        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Rgba8888), image.ToRgba8888());
        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Argb8888), image.ToArgb8888());
        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Bgra8888), image.ToBgra8888());
        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Abgr8888), image.ToAbgr8888());
    }

    [Fact]
    public void NamedExtension_SpanOverload_MatchesArray()
    {
        var image = JpegImage.CreateRgb(2, 1, [10, 20, 30, 40, 50, 60]);
        var destination = new int[2];
        image.ToArgb8888(destination);
        Assert.Equal(image.ToArgb8888(), destination);
    }

    [Fact]
    public void Grayscale_ReplicatesLuminanceAndIsOpaque()
    {
        var image = JpegImage.CreateGrayscale(1, 1, [128]);
        Assert.Equal((128 << 24) | (128 << 16) | (128 << 8) | 255, image.ToRgba8888()[0]);
    }

    [Fact]
    public void ProducesOnePixelPerSampleInRowMajorOrder()
    {
        var image = JpegImage.CreateRgb(2, 2, [
            0, 0, 0,        1, 1, 1,
            2, 2, 2,        3, 3, 3,
        ]);

        var packed = image.ToRgba8888();
        Assert.Equal(4, packed.Length);
        Assert.Equal((3 << 24) | (3 << 16) | (3 << 8) | 255, packed[3]);
    }

    [Fact]
    public void SpanOverload_MatchesArrayOverload_AndLeavesExtraUntouched()
    {
        var image = JpegImage.CreateRgb(2, 1, [10, 20, 30, 40, 50, 60]);
        var expected = image.ToPackedPixels(PackedPixelFormat.Bgra8888);

        var destination = new int[3];
        destination[2] = unchecked((int)0xDEADBEEF);
        image.ToPackedPixels(destination, PackedPixelFormat.Bgra8888);

        Assert.Equal(expected[0], destination[0]);
        Assert.Equal(expected[1], destination[1]);
        Assert.Equal(unchecked((int)0xDEADBEEF), destination[2]);
    }

    [Fact]
    public void SpanOverload_ThrowsWhenDestinationTooSmall()
    {
        var image = JpegImage.CreateRgb(2, 2, new byte[2 * 2 * 3]);
        Assert.Throws<ArgumentException>(() => image.ToPackedPixels(new int[3], PackedPixelFormat.Rgba8888));
    }

    [Fact]
    public void Cmyk_NoInk_IsWhite()
    {
        var image = JpegImage.CreateCmyk(1, 1, [0, 0, 0, 0]);
        Assert.Equal((255 << 24) | (255 << 16) | (255 << 8) | 255, image.ToRgba8888()[0]);
    }

    [Fact]
    public void Cmyk_FullBlack_IsBlack()
    {
        var image = JpegImage.CreateCmyk(1, 1, [0, 0, 0, 255]);
        Assert.Equal((0 << 24) | (0 << 16) | (0 << 8) | 255, image.ToRgba8888()[0]);
    }

    [Fact]
    public void Cmyk_UsesMultiplicativeModel()
    {
        const byte c = 51, m = 102, y = 153, k = 20;
        var image = JpegImage.CreateCmyk(1, 1, [c, m, y, k]);

        // Standard model: channel = (255 - ink) * (255 - k) / 255.
        int r = (255 - c) * (255 - k) / 255;
        int g = (255 - m) * (255 - k) / 255;
        int b = (255 - y) * (255 - k) / 255;

        Assert.Equal((r << 24) | (g << 16) | (b << 8) | 255, image.ToRgba8888()[0]);
    }

    [Fact]
    public void Cmyk_MatchesColorConverter()
    {
        const byte c = 200, m = 60, y = 120, k = 30;
        var image = JpegImage.CreateCmyk(1, 1, [c, m, y, k]);

        JpegSharp.Color.ColorConverter.CmykToRgb(c, m, y, k, out var r, out var g, out var b);
        Assert.Equal((r << 24) | (g << 16) | (b << 8) | 255, image.ToRgba8888()[0]);
    }

    [Fact]
    public void Cmyk_RoundTripsThroughCodec_ThenPacksToExpectedRgb()
    {
        // A flat CMYK color survives encode/decode near-losslessly, so packing the decoded image
        // should match a direct conversion of the original CMYK. This exercises the real path:
        // the encoder writes Adobe-inverted CMYK and the decoder normalizes it back before we pack.
        const byte c = 40, m = 80, y = 120, k = 30;
        const int w = 16, h = 16;
        var samples = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            samples[i * 4] = c;
            samples[i * 4 + 1] = m;
            samples[i * 4 + 2] = y;
            samples[i * 4 + 3] = k;
        }

        var decoded = Jpeg.Decode(Jpeg.Encode(JpegImage.CreateCmyk(w, h, samples), new JpegEncoderOptions { Quality = 100 }));
        Assert.Equal(JpegColorSpace.Cmyk, decoded.ColorSpace);

        JpegSharp.Color.ColorConverter.CmykToRgb(c, m, y, k, out var er, out var eg, out var eb);
        var packed = decoded.ToRgba8888();
        foreach (var px in packed)
        {
            Assert.InRange((px >> 24) & 0xFF, er - 4, er + 4);
            Assert.InRange((px >> 16) & 0xFF, eg - 4, eg + 4);
            Assert.InRange((px >> 8) & 0xFF, eb - 4, eb + 4);
            Assert.Equal(255, px & 0xFF);
        }
    }
}

using WebPSharp.Api;

namespace WebPSharp.Tests;

public class WebPEdgeCaseTests
{
    private static byte[] Noise(int count, int seed)
    {
        var rng = new Random(seed);
        var b = new byte[count];
        rng.NextBytes(b);
        return b;
    }

    [Theory]
    [InlineData(1, 1024)]
    [InlineData(1024, 1)]
    [InlineData(2, 777)]
    [InlineData(777, 2)]
    [InlineData(13, 17)]
    [InlineData(251, 3)]
    [InlineData(1, 1)]
    public void ExtremeShapes_RoundTrip(int width, int height)
    {
        var pixels = Noise(width * height * 4, width * 31 + height);
        var image = WebPImage.CreateRgba(width, height, pixels);
        var decoded = WebP.Decode(WebP.Encode(image));
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.Equal(pixels, decoded.PixelData);
    }

    [Fact]
    public void ModeratelyLargeImage_RoundTrips()
    {
        const int w = 400, h = 300;
        var pixels = Noise(w * h * 4, 5);
        var image = WebPImage.CreateRgba(w, h, pixels);
        var decoded = WebP.Decode(WebP.Encode(image, new WebPEncoderOptions { Effort = 2 }));
        Assert.Equal(pixels, decoded.PixelData);
    }

    [Theory]
    [InlineData(256)] // bundling bits = 0
    [InlineData(17)]  // bundling bits = 0 (just over 16)
    [InlineData(16)]  // bundling bits = 1
    [InlineData(2)]   // bundling bits = 3
    public void PaletteBoundaries_RoundTrip(int colors)
    {
        var rng = new Random(colors);
        var palette = new uint[colors];
        for (var i = 0; i < colors; i++) palette[i] = (uint)rng.NextInt64(0, 1L << 32);

        const int w = 40, h = 40;
        var pixels = new byte[w * h * 4];
        // Ensure every palette entry appears so the distinct count is exactly `colors`.
        for (var p = 0; p < w * h; p++)
        {
            var c = palette[p < colors ? p : rng.Next(colors)];
            pixels[p * 4] = (byte)(c >> 16);
            pixels[p * 4 + 1] = (byte)(c >> 8);
            pixels[p * 4 + 2] = (byte)c;
            pixels[p * 4 + 3] = (byte)(c >> 24);
        }
        var image = WebPImage.CreateRgba(w, h, pixels);
        var decoded = WebP.Decode(WebP.Encode(image, new WebPEncoderOptions { Effort = 4 }));
        Assert.Equal(pixels, decoded.PixelData);
    }

    [Fact]
    public void RgbSource_RoundTripsToOpaqueRgba()
    {
        const int w = 33, h = 21;
        var pixels = Noise(w * h * 3, 9);
        var image = WebPImage.CreateRgb(w, h, pixels);
        var decoded = WebP.Decode(WebP.Encode(image));
        Assert.Equal(WebPColorFormat.Rgba, decoded.Format);
        for (var p = 0; p < w * h; p++)
        {
            Assert.Equal(pixels[p * 3], decoded.PixelData[p * 4]);
            Assert.Equal(pixels[p * 3 + 1], decoded.PixelData[p * 4 + 1]);
            Assert.Equal(pixels[p * 3 + 2], decoded.PixelData[p * 4 + 2]);
            Assert.Equal(255, decoded.PixelData[p * 4 + 3]);
        }
    }

    [Fact]
    public void ManyFrameAnimation_RoundTrips()
    {
        var anim = new WebPAnimation(16, 16) { LoopCount = 2 };
        var rng = new Random(3);
        for (var f = 0; f < 30; f++)
        {
            var pixels = Noise(16 * 16 * 4, f);
            anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(16, 16, pixels), durationMs: 10 + f));
        }

        var decoded = WebP.DecodeAnimation(WebP.EncodeAnimation(anim));
        Assert.Equal(30, decoded.Frames.Count);
        for (var f = 0; f < 30; f++)
        {
            Assert.Equal(anim.Frames[f].DurationMs, decoded.Frames[f].DurationMs);
            Assert.Equal(anim.Frames[f].Image.PixelData, decoded.Frames[f].Image.PixelData);
        }
    }

    [Fact]
    public void LargeMetadataPayloads_RoundTrip()
    {
        var image = WebPImage.CreateRgba(4, 4, new byte[64]);
        image.Metadata = new WebPMetadata
        {
            IccProfile = Noise(50_000, 1),
            Exif = Noise(20_000, 2),
            Xmp = Noise(10_001, 3), // odd length exercises chunk padding
        };
        var decoded = WebP.Decode(WebP.Encode(image));
        Assert.Equal(image.Metadata.IccProfile, decoded.Metadata!.IccProfile);
        Assert.Equal(image.Metadata.Exif, decoded.Metadata.Exif);
        Assert.Equal(image.Metadata.Xmp, decoded.Metadata.Xmp);
    }
}

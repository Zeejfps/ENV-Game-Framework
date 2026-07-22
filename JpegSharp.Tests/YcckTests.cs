using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class YcckTests
{
    [Fact]
    public void Ycck_RoundTrips_AtHighQuality()
    {
        var pixels = CmykGradient(32, 32);
        var image = JpegImage.CreateCmyk(32, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 95, CmykAsYcck = true });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(JpegColorSpace.Cmyk, decoded.ColorSpace);
        Assert.Equal(4, decoded.ComponentCount);
        AssertClose(pixels, decoded.PixelData, 6.0, 55);
    }

    [Fact]
    public void Ycck_WritesAdobeTransform2()
    {
        var image = JpegImage.CreateCmyk(16, 16, CmykGradient(16, 16));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, CmykAsYcck = true });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(2, decoded.Metadata!.AdobeColorTransform);
    }

    [Fact]
    public void Cmyk_StillWritesAdobeTransform0()
    {
        var image = JpegImage.CreateCmyk(16, 16, CmykGradient(16, 16));
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { CmykAsYcck = false }));
        Assert.Equal(0, decoded.Metadata!.AdobeColorTransform);
    }

    [Fact]
    public void FlatYcck_RoundTripsNearlyExact()
    {
        var pixels = new byte[24 * 24 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 40;
            pixels[i + 1] = 90;
            pixels[i + 2] = 160;
            pixels[i + 3] = 25;
        }

        var image = JpegImage.CreateCmyk(24, 24, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 100, CmykAsYcck = true }));
        AssertClose(pixels, decoded.PixelData, 3.0, 12);
    }

    private static void AssertClose(byte[] expected, byte[] actual, double meanTol, int maxTol)
    {
        Assert.Equal(expected.Length, actual.Length);
        long total = 0;
        var max = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            var d = Math.Abs(expected[i] - actual[i]);
            total += d;
            if (d > max)
                max = d;
        }

        Assert.True((double)total / expected.Length <= meanTol, $"mean {(double)total / expected.Length:F2} > {meanTol}");
        Assert.True(max <= maxTol, $"max {max} > {maxTol}");
    }

    private static byte[] CmykGradient(int w, int h)
    {
        var data = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                data[i] = (byte)(x * 255 / Math.Max(1, w - 1));
                data[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                data[i + 2] = (byte)((x + y) * 255 / Math.Max(1, w + h - 2));
                data[i + 3] = (byte)(255 - x * 255 / Math.Max(1, w - 1));
            }
        return data;
    }
}

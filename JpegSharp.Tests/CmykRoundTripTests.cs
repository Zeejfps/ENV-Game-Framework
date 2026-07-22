using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class CmykRoundTripTests
{
    [Fact]
    public void Cmyk_RoundTrips_AtHighQuality()
    {
        var pixels = CmykGradient(32, 32);
        var image = JpegImage.CreateCmyk(32, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 95 });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(JpegColorSpace.Cmyk, decoded.ColorSpace);
        Assert.Equal(4, decoded.ComponentCount);
        Assert.Equal(32, decoded.Width);
        Assert.Equal(32, decoded.Height);
        AssertClose(pixels, decoded.PixelData, 5.0, 45);
    }

    [Fact]
    public void Cmyk_Output_ContainsAdobeMarker()
    {
        var image = JpegImage.CreateCmyk(16, 16, CmykGradient(16, 16));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 });

        // APP14 marker present...
        var hasApp14 = false;
        for (var i = 0; i < bytes.Length - 1; i++)
            if (bytes[i] == 0xFF && bytes[i + 1] == 0xEE)
                hasApp14 = true;
        Assert.True(hasApp14, "expected an APP14 (Adobe) marker");

        // ...and the "Adobe" identifier appears in the payload.
        var text = System.Text.Encoding.ASCII.GetString(bytes);
        Assert.Contains("Adobe", text);
    }

    [Fact]
    public void FlatCmyk_RoundTripsNearlyExact()
    {
        var pixels = new byte[20 * 20 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 30;
            pixels[i + 1] = 60;
            pixels[i + 2] = 200;
            pixels[i + 3] = 15;
        }

        var image = JpegImage.CreateCmyk(20, 20, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 100 }));
        AssertClose(pixels, decoded.PixelData, 2.0, 10);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 5)]
    [InlineData(17, 13)]
    public void Cmyk_OddDimensions_Decode(int width, int height)
    {
        var pixels = CmykGradient(width, height);
        var image = JpegImage.CreateCmyk(width, height, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 }));
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.Equal(width * height * 4, decoded.PixelData.Length);
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

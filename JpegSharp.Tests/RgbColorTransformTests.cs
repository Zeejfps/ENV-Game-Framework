using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class RgbColorTransformTests
{
    [Fact]
    public void RgbDirect_RoundTripsNearLossless()
    {
        var pixels = ColorGradient(32, 32);
        var image = JpegImage.CreateRgb(32, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 98, RgbEncoding = JpegRgbEncoding.Rgb });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);
        Assert.Equal(32, decoded.Width);
        // No color transform and no subsampling -> only DCT/quant rounding remains.
        AssertClose(pixels, decoded.PixelData, 2.0, 12);
    }

    [Fact]
    public void RgbDirect_WritesAdobeTransform0()
    {
        var image = JpegImage.CreateRgb(16, 16, ColorGradient(16, 16));
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { RgbEncoding = JpegRgbEncoding.Rgb }));
        Assert.Equal(0, decoded.Metadata!.AdobeColorTransform);
    }

    [Fact]
    public void RgbDirect_DoesNotApplyYCbCrTransform()
    {
        // Pure saturated colors would be badly distorted if YCbCr were wrongly applied
        // together with the direct-RGB reconstruction.
        var pixels = new byte[8 * 8 * 3];
        for (var i = 0; i < pixels.Length; i += 3)
        {
            pixels[i] = 255;     // R
            pixels[i + 1] = 0;   // G
            pixels[i + 2] = 128; // B
        }

        var image = JpegImage.CreateRgb(8, 8, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 100, RgbEncoding = JpegRgbEncoding.Rgb }));

        AssertClose(pixels, decoded.PixelData, 3.0, 12);
    }

    [Fact]
    public void YCbCrEncoding_RemainsDefault()
    {
        var image = JpegImage.CreateRgb(16, 16, ColorGradient(16, 16));
        var decoded = Jpeg.Decode(Jpeg.Encode(image)); // default
        // JFIF/YCbCr path: no Adobe marker, so AdobeColorTransform stays null.
        Assert.Null(decoded.Metadata!.AdobeColorTransform);
        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);
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

    private static byte[] ColorGradient(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var idx = (y * w + x) * 3;
                d[idx] = (byte)(x * 255 / Math.Max(1, w - 1));
                d[idx + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                d[idx + 2] = (byte)((x + y) % 256);
            }
        return d;
    }
}

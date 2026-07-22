using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ProgressiveTests
{
    [Fact]
    public void Progressive_UsesSof2Marker_AndIsIdentifiedAsProgressive()
    {
        var image = JpegImage.CreateGrayscale(32, 32, Gradient(32, 32));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true });

        Assert.True(ContainsMarker(bytes, 0xC2), "expected SOF2 marker");
        Assert.True(Jpeg.Identify(bytes).IsProgressive);
    }

    [Fact]
    public void Progressive_Grayscale_DecodesIdenticallyToBaseline()
    {
        // Spectral-selection-only progressive splits the same coefficients across scans,
        // so it must reconstruct bit-for-bit identically to the baseline encoding.
        var pixels = Gradient(40, 40);
        var image = JpegImage.CreateGrayscale(40, 40, pixels);
        var options = new JpegEncoderOptions { Quality = 80 };
        var baseline = Jpeg.Decode(Jpeg.Encode(image, options));

        var progressive = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 80, Progressive = true }));

        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Theory]
    [InlineData(ChromaSubsampling.Samp444)]
    [InlineData(ChromaSubsampling.Samp420)]
    [InlineData(ChromaSubsampling.Samp422)]
    public void Progressive_Rgb_DecodesIdenticallyToBaseline(ChromaSubsampling subsampling)
    {
        var pixels = ColorGradient(48, 40);
        var image = JpegImage.CreateRgb(48, 40, pixels);

        var baseline = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 85, Subsampling = subsampling }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 85, Subsampling = subsampling, Progressive = true }));

        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void Progressive_RoundTrips_CloseToOriginal()
    {
        var pixels = ColorGradient(64, 64);
        var image = JpegImage.CreateRgb(64, 64, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 92, Progressive = true }));
        AssertClose(pixels, decoded.PixelData, 10.0, 90);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 3)]
    [InlineData(17, 15)]
    public void Progressive_OddDimensions_Decode(int width, int height)
    {
        var pixels = Gradient(width, height);
        var image = JpegImage.CreateGrayscale(width, height, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 88, Progressive = true }));
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
    }

    [Fact]
    public void Progressive_WithRestartInterval_RoundTrips()
    {
        var pixels = Gradient(40, 40);
        var image = JpegImage.CreateGrayscale(40, 40, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 85, Progressive = true, RestartInterval = 3 }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    private static bool ContainsMarker(byte[] data, byte code)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] == code)
                return true;
        return false;
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

        Assert.True((double)total / expected.Length <= meanTol);
        Assert.True(max <= maxTol);
    }

    private static byte[] Gradient(int w, int h)
    {
        var data = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                data[y * w + x] = (byte)((x * 255 / Math.Max(1, w - 1) + y * 255 / Math.Max(1, h - 1)) / 2);
        return data;
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var data = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                data[i] = (byte)(x * 255 / Math.Max(1, w - 1));
                data[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                data[i + 2] = (byte)((x + y) * 255 / Math.Max(1, w + h - 2));
            }
        return data;
    }
}

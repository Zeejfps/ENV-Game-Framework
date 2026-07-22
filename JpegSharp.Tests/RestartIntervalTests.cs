using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class RestartIntervalTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Grayscale_WithRestartInterval_RoundTrips(int interval)
    {
        var pixels = Gradient(40, 40);
        var image = JpegImage.CreateGrayscale(40, 40, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RestartInterval = interval });

        var decoded = Jpeg.Decode(bytes);
        AssertClose(pixels, decoded.PixelData, 4.0, 40);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void Rgb420_WithRestartInterval_RoundTrips(int interval)
    {
        var pixels = ColorGradient(48, 32);
        var image = JpegImage.CreateRgb(48, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions
        {
            Quality = 90,
            Subsampling = ChromaSubsampling.Samp420,
            RestartInterval = interval,
        });

        var decoded = Jpeg.Decode(bytes);
        AssertClose(pixels, decoded.PixelData, 10.0, 90);
    }

    [Fact]
    public void Output_ContainsDriAndRestartMarkers()
    {
        var image = JpegImage.CreateGrayscale(64, 64, Gradient(64, 64));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RestartInterval = 2 });

        Assert.True(ContainsMarker(bytes, 0xDD), "expected a DRI marker");
        Assert.True(ContainsRestartMarker(bytes), "expected at least one RSTn marker");
    }

    [Fact]
    public void RestartInterval_LargerThanMcuCount_StillDecodes()
    {
        var pixels = Gradient(16, 16);
        var image = JpegImage.CreateGrayscale(16, 16, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RestartInterval = 10_000 });
        var decoded = Jpeg.Decode(bytes);
        AssertClose(pixels, decoded.PixelData, 4.0, 40);
    }

    [Fact]
    public void RestartEncoding_IsDeterministic()
    {
        var image = JpegImage.CreateRgb(32, 32, ColorGradient(32, 32));
        var options = new JpegEncoderOptions { Quality = 85, RestartInterval = 3 };
        Assert.Equal(Jpeg.Encode(image, options), Jpeg.Encode(image, options));
    }

    private static bool ContainsMarker(byte[] data, byte code)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] == code)
                return true;
        return false;
    }

    private static bool ContainsRestartMarker(byte[] data)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] is >= 0xD0 and <= 0xD7)
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
                data[i + 2] = 128;
            }
        return data;
    }
}

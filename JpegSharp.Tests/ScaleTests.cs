using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ScaleTests
{
    [Fact]
    public void LargeImage_Baseline_RoundTrips()
    {
        const int w = 512, h = 384;
        var pixels = Photo(w, h);
        var image = JpegImage.CreateRgb(w, h, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 }));

        Assert.Equal(w, decoded.Width);
        Assert.Equal(h, decoded.Height);
        AssertMean(pixels, decoded.PixelData, 12.0);
    }

    [Fact]
    public void LargeImage_Progressive_MatchesBaseline()
    {
        const int w = 480, h = 320;
        var pixels = Photo(w, h);
        var image = JpegImage.CreateRgb(w, h, pixels);

        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 82, Subsampling = ChromaSubsampling.Samp422 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 82, Subsampling = ChromaSubsampling.Samp422, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void LargeGrayscale_WithRestart_RoundTrips()
    {
        const int w = 400, h = 400;
        var pixels = new byte[w * h];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)((i * 7) % 256);
        var image = JpegImage.CreateGrayscale(w, h, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 88, RestartInterval = 17 }));
        Assert.Equal(w, decoded.Width);
        Assert.Equal(h, decoded.Height);
        AssertMean(pixels, decoded.PixelData, 6.0);
    }

    private static void AssertMean(byte[] expected, byte[] actual, double tol)
    {
        Assert.Equal(expected.Length, actual.Length);
        long total = 0;
        for (var i = 0; i < expected.Length; i++)
            total += Math.Abs(expected[i] - actual[i]);
        var mean = (double)total / expected.Length;
        Assert.True(mean <= tol, $"mean abs error {mean:F2} exceeded {tol}");
    }

    private static byte[] Photo(int w, int h)
    {
        // Smooth gradients with mild texture, approximating photographic content.
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                d[i] = (byte)((x * 255 / w + (y & 15) * 2) & 0xFF);
                d[i + 1] = (byte)((y * 255 / h + (x & 15) * 2) & 0xFF);
                d[i + 2] = (byte)(((x + y) * 255 / (w + h)) & 0xFF);
            }
        return d;
    }
}

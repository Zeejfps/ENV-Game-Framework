using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ProgressiveRefinementTests
{
    [Fact]
    public void Progressive_Grayscale_MatchesBaseline_WithSuccessiveApproximation()
    {
        // With successive-approximation refinement scans, the reconstructed coefficients are
        // identical to baseline, so the pixels must match exactly.
        var pixels = Gradient(48, 48);
        var image = JpegImage.CreateGrayscale(48, 48, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Theory]
    [InlineData(ChromaSubsampling.Samp444)]
    [InlineData(ChromaSubsampling.Samp420)]
    [InlineData(ChromaSubsampling.Samp422)]
    public void Progressive_Rgb_MatchesBaseline_WithSuccessiveApproximation(ChromaSubsampling subsampling)
    {
        var pixels = ColorGradient(56, 48);
        var image = JpegImage.CreateRgb(56, 48, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = subsampling }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = subsampling, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void Progressive_NoisyImage_MatchesBaseline()
    {
        // Random content exercises many nonzero AC coefficients and correction bits.
        var rng = new Random(1234);
        var pixels = new byte[64 * 64 * 3];
        rng.NextBytes(pixels);
        var image = JpegImage.CreateRgb(64, 64, pixels);

        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 70 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 70, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(95)]
    [InlineData(100)]
    public void Progressive_MatchesBaseline_AcrossQualities(int quality)
    {
        var pixels = ColorGradient(40, 40);
        var image = JpegImage.CreateRgb(40, 40, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = quality }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = quality, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void Progressive_WithRestart_MatchesBaseline_WithSuccessiveApproximation()
    {
        var pixels = Gradient(40, 40);
        var image = JpegImage.CreateGrayscale(40, 40, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true, RestartInterval = 4 }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
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
                data[i + 2] = (byte)((x * y) % 256);
            }
        return data;
    }
}

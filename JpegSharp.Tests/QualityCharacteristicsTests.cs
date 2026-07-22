using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class QualityCharacteristicsTests
{
    [Fact]
    public void HigherQuality_ReducesReconstructionError()
    {
        var pixels = Photo(64, 64);
        var image = JpegImage.CreateRgb(64, 64, pixels);

        var e20 = MeanError(pixels, Decode(image, 20));
        var e50 = MeanError(pixels, Decode(image, 50));
        var e80 = MeanError(pixels, Decode(image, 80));
        var e95 = MeanError(pixels, Decode(image, 95));

        Assert.True(e20 > e50, $"q20 {e20:F2} should exceed q50 {e50:F2}");
        Assert.True(e50 > e80, $"q50 {e50:F2} should exceed q80 {e80:F2}");
        Assert.True(e80 > e95, $"q80 {e80:F2} should exceed q95 {e95:F2}");
    }

    [Fact]
    public void HigherQuality_ProducesLargerFiles()
    {
        var image = JpegImage.CreateRgb(64, 64, Photo(64, 64));
        var s20 = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 20 }).Length;
        var s50 = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 50 }).Length;
        var s90 = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 }).Length;
        Assert.True(s20 < s50);
        Assert.True(s50 < s90);
    }

    [Fact]
    public void Quality90_444_AchievesReasonablePsnr()
    {
        var pixels = Photo(64, 64);
        var image = JpegImage.CreateRgb(64, 64, pixels);
        var decoded = Decode(image, 90, ChromaSubsampling.Samp444);

        var psnr = Psnr(pixels, decoded);
        Assert.True(psnr > 35.0, $"PSNR {psnr:F1} dB below 35 dB at q90 4:4:4");
    }

    [Fact]
    public void Quality100_444_IsNearLossless()
    {
        var pixels = Photo(48, 48);
        var image = JpegImage.CreateRgb(48, 48, pixels);
        var decoded = Decode(image, 100, ChromaSubsampling.Samp444);
        var psnr = Psnr(pixels, decoded);
        Assert.True(psnr > 45.0, $"PSNR {psnr:F1} dB below 45 dB at q100 4:4:4");
    }

    private static byte[] Decode(JpegImage image, int quality, ChromaSubsampling sub = ChromaSubsampling.Samp420)
        => Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = quality, Subsampling = sub })).PixelData;

    private static double MeanError(byte[] a, byte[] b)
    {
        long total = 0;
        for (var i = 0; i < a.Length; i++)
            total += Math.Abs(a[i] - b[i]);
        return (double)total / a.Length;
    }

    private static double Psnr(byte[] a, byte[] b)
    {
        double mse = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var d = a[i] - b[i];
            mse += d * d;
        }

        mse /= a.Length;
        if (mse <= 0)
            return double.PositiveInfinity;
        return 10.0 * Math.Log10(255.0 * 255.0 / mse);
    }

    private static byte[] Photo(int w, int h)
    {
        // Smooth low-frequency content, where quality differences are clearly ordered.
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                d[i] = (byte)(128 + 100 * Math.Sin(x * 0.15));
                d[i + 1] = (byte)(128 + 100 * Math.Sin(y * 0.15));
                d[i + 2] = (byte)(128 + 100 * Math.Sin((x + y) * 0.1));
            }
        return d;
    }
}

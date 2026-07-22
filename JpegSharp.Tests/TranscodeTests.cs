using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class TranscodeTests
{
    [Fact]
    public void ReEncoding_AtSameQuality_IsNearlyIdempotent()
    {
        var original = Photo(64, 64);
        var image = JpegImage.CreateRgb(64, 64, original);
        var options = new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp444 };

        var gen1 = Jpeg.Decode(Jpeg.Encode(image, options)).PixelData;
        var gen2 = Jpeg.Decode(Jpeg.Encode(JpegImage.CreateRgb(64, 64, gen1), options)).PixelData;
        var gen3 = Jpeg.Decode(Jpeg.Encode(JpegImage.CreateRgb(64, 64, gen2), options)).PixelData;

        // Generation loss is front-loaded: after the first save, subsequent re-saves at the
        // same quality drift far less than the original -> gen1 step.
        var firstLoss = MeanError(original, gen1);
        var secondLoss = MeanError(gen1, gen2);
        var thirdLoss = MeanError(gen2, gen3);

        Assert.True(secondLoss < firstLoss, $"gen1->gen2 {secondLoss:F2} should be < original->gen1 {firstLoss:F2}");
        Assert.True(secondLoss < 2.0, $"gen1->gen2 drift {secondLoss:F2} too high");
        Assert.True(thirdLoss <= secondLoss + 0.5, "generation drift should not accelerate");
    }

    [Fact]
    public void GrayscaleReEncoding_Stabilizes()
    {
        var original = new byte[48 * 48];
        for (var i = 0; i < original.Length; i++)
            original[i] = (byte)(128 + 60 * Math.Sin(i * 0.05));

        var options = new JpegEncoderOptions { Quality = 85 };
        var gen1 = Jpeg.Decode(Jpeg.Encode(JpegImage.CreateGrayscale(48, 48, original), options)).PixelData;
        var gen2 = Jpeg.Decode(Jpeg.Encode(JpegImage.CreateGrayscale(48, 48, gen1), options)).PixelData;

        Assert.True(MeanError(gen1, gen2) < MeanError(original, gen1));
    }

    private static double MeanError(byte[] a, byte[] b)
    {
        long total = 0;
        for (var i = 0; i < a.Length; i++)
            total += Math.Abs(a[i] - b[i]);
        return (double)total / a.Length;
    }

    private static byte[] Photo(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                d[i] = (byte)(128 + 90 * Math.Sin(x * 0.12));
                d[i + 1] = (byte)(128 + 90 * Math.Sin(y * 0.12));
                d[i + 2] = (byte)(128 + 90 * Math.Sin((x + y) * 0.09));
            }
        return d;
    }
}

using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class ProgressiveRobustnessTests
{
    [Fact]
    public void TruncatedProgressiveStream_NeverThrowsNonJpegException()
    {
        var image = JpegImage.CreateRgb(40, 32, ColorGradient(40, 32));
        var full = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, Progressive = true });

        for (var length = 0; length < full.Length; length += 2)
        {
            try
            {
                Jpeg.Decode(full[..length]);
            }
            catch (JpegException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Truncation at {length} threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [Fact]
    public void CorruptedProgressiveEntropy_NeverThrowsNonJpegException()
    {
        var image = JpegImage.CreateGrayscale(48, 48, Gradient(48, 48));
        var full = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, Progressive = true });
        var rng = new Random(31);

        for (var trial = 0; trial < 100; trial++)
        {
            var copy = (byte[])full.Clone();
            var idx = full.Length / 3 + rng.Next(full.Length / 3);
            copy[idx] ^= (byte)(1 << rng.Next(8));
            try
            {
                Jpeg.Decode(copy);
            }
            catch (JpegException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Corruption at {idx} threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [Fact]
    public void CorruptedProgressiveDecode_Terminates()
    {
        // A regression guard against unbounded loops in the multi-scan / EOB-run logic:
        // decoding heavily corrupted progressive data must return or throw promptly.
        var image = JpegImage.CreateRgb(64, 64, ColorGradient(64, 64));
        var full = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 75, Progressive = true, Subsampling = ChromaSubsampling.Samp420 });

        var scanStart = FindFirstScanStart(full);
        for (var i = scanStart; i < full.Length - 2; i++)
            full[i] = 0xA5;

        var ex = Record.Exception(() => Jpeg.Decode(full));
        if (ex is not null)
            Assert.IsAssignableFrom<JpegException>(ex);
    }

    [Fact]
    public void ProgressiveAcScanWithMultipleComponents_Throws()
    {
        // Hand-corrupt a progressive stream so an AC scan header declares 2 components,
        // which is illegal (AC scans must be non-interleaved).
        var image = JpegImage.CreateRgb(16, 16, ColorGradient(16, 16));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Progressive = true });

        // Find an AC SOS (Ss != 0) and bump its component count to 2.
        for (var i = 0; i < bytes.Length - 6; i++)
        {
            if (bytes[i] == 0xFF && bytes[i + 1] == 0xDA)
            {
                var ns = bytes[i + 4];
                var ssOffset = i + 4 + 1 + ns * 2;
                if (ssOffset < bytes.Length && bytes[ssOffset] != 0 && ns == 1)
                {
                    // This is a single-component AC scan; forge Ns = 2 without adding data.
                    bytes[i + 4] = 2;
                    break;
                }
            }
        }

        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes));
    }

    private static int FindFirstScanStart(byte[] data)
    {
        for (var i = 0; i < data.Length - 3; i++)
            if (data[i] == 0xFF && data[i + 1] == 0xDA)
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                return i + 2 + len;
            }
        return data.Length;
    }

    private static byte[] Gradient(int w, int h)
    {
        var d = new byte[w * h];
        for (var i = 0; i < d.Length; i++)
            d[i] = (byte)(i % 256);
        return d;
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                d[i] = (byte)(x * 255 / Math.Max(1, w - 1));
                d[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                d[i + 2] = (byte)((x + y) % 256);
            }
        return d;
    }
}

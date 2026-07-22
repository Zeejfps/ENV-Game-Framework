using System.Collections.Concurrent;
using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ThreadSafetyTests
{
    [Fact]
    public void ParallelEncode_ProducesIdenticalDeterministicOutput()
    {
        var image = JpegImage.CreateRgb(48, 48, ColorGradient(48, 48));
        var options = new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 };
        var reference = Jpeg.Encode(image, options);

        var results = new ConcurrentBag<byte[]>();
        Parallel.For(0, 200, _ => results.Add(Jpeg.Encode(image, options)));

        Assert.All(results, r => Assert.Equal(reference, r));
    }

    [Fact]
    public void ParallelDecode_ProducesIdenticalOutput()
    {
        var image = JpegImage.CreateRgb(40, 40, ColorGradient(40, 40));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 });
        var reference = Jpeg.Decode(bytes).PixelData;

        var results = new ConcurrentBag<byte[]>();
        Parallel.For(0, 200, _ => results.Add(Jpeg.Decode(bytes).PixelData));

        Assert.All(results, r => Assert.Equal(reference, r));
    }

    [Fact]
    public void MixedParallelWorkload_ProducesCorrectResults()
    {
        var images = new[]
        {
            JpegImage.CreateGrayscale(32, 32, Gray(32, 32)),
            JpegImage.CreateRgb(32, 24, ColorGradient(32, 24)),
            JpegImage.CreateCmyk(24, 24, Cmyk(24, 24)),
        };

        // Reference outputs computed sequentially.
        var refBytes = images.Select(im => Jpeg.Encode(im, new JpegEncoderOptions { Quality = 80 })).ToArray();
        var refPixels = refBytes.Select(b => Jpeg.Decode(b).PixelData).ToArray();

        Parallel.For(0, 300, i =>
        {
            var idx = i % images.Length;
            var encoded = Jpeg.Encode(images[idx], new JpegEncoderOptions { Quality = 80 });
            Assert.Equal(refBytes[idx], encoded);
            var pixels = Jpeg.Decode(encoded).PixelData;
            Assert.Equal(refPixels[idx], pixels);
        });
    }

    [Fact]
    public void ParallelProgressive_MatchesBaseline()
    {
        var image = JpegImage.CreateRgb(40, 40, ColorGradient(40, 40));
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 })).PixelData;

        Parallel.For(0, 100, _ =>
        {
            var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true })).PixelData;
            Assert.Equal(baseline, decoded);
        });
    }

    private static byte[] Gray(int w, int h)
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
                var idx = (y * w + x) * 3;
                d[idx] = (byte)(x * 255 / Math.Max(1, w - 1));
                d[idx + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                d[idx + 2] = (byte)((x + y) % 256);
            }
        return d;
    }

    private static byte[] Cmyk(int w, int h)
    {
        var d = new byte[w * h * 4];
        for (var i = 0; i < d.Length; i++)
            d[i] = (byte)((i * 13) % 256);
        return d;
    }
}

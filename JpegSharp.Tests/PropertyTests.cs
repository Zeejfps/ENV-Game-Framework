using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class PropertyTests
{
    [Fact]
    public void RandomGrayscale_RoundTrips_AtMaxQuality()
    {
        var rng = new Random(101);
        for (var trial = 0; trial < 25; trial++)
        {
            var w = rng.Next(1, 40);
            var h = rng.Next(1, 40);
            var pixels = new byte[w * h];
            rng.NextBytes(pixels);

            var image = JpegImage.CreateGrayscale(w, h, pixels);
            var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 100 }));

            Assert.Equal(w, decoded.Width);
            Assert.Equal(h, decoded.Height);
            AssertMeanError(pixels, decoded.PixelData, 3.0);
        }
    }

    [Fact]
    public void RandomRgb444_RoundTrips_AtHighQuality()
    {
        var rng = new Random(202);
        for (var trial = 0; trial < 20; trial++)
        {
            var w = rng.Next(1, 48);
            var h = rng.Next(1, 48);
            var pixels = new byte[w * h * 3];
            rng.NextBytes(pixels);

            var image = JpegImage.CreateRgb(w, h, pixels);
            var decoded = Jpeg.Decode(Jpeg.Encode(image,
                new JpegEncoderOptions { Quality = 98, Subsampling = ChromaSubsampling.Samp444 }));

            Assert.Equal(w, decoded.Width);
            Assert.Equal(h, decoded.Height);
            AssertMeanError(pixels, decoded.PixelData, 6.0);
        }
    }

    [Fact]
    public void Encoding_IsDeterministic_ForRandomInputs()
    {
        var rng = new Random(303);
        for (var trial = 0; trial < 10; trial++)
        {
            var w = rng.Next(1, 32);
            var h = rng.Next(1, 32);
            var pixels = new byte[w * h * 3];
            rng.NextBytes(pixels);
            var image = JpegImage.CreateRgb(w, h, pixels);

            var options = new JpegEncoderOptions { Quality = 77, Subsampling = ChromaSubsampling.Samp420 };
            Assert.Equal(Jpeg.Encode(image, options), Jpeg.Encode(image, options));
        }
    }

    [Fact]
    public void Progressive_MatchesBaseline_ForRandomInputs()
    {
        var rng = new Random(404);
        for (var trial = 0; trial < 15; trial++)
        {
            var w = rng.Next(1, 40);
            var h = rng.Next(1, 40);
            var pixels = new byte[w * h * 3];
            rng.NextBytes(pixels);
            var image = JpegImage.CreateRgb(w, h, pixels);

            foreach (var subsampling in new[] { ChromaSubsampling.Samp444, ChromaSubsampling.Samp420, ChromaSubsampling.Samp422, ChromaSubsampling.Samp411 })
            {
                var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 68, Subsampling = subsampling }));
                var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 68, Subsampling = subsampling, Progressive = true }));
                Assert.Equal(baseline.PixelData, progressive.PixelData);
            }
        }
    }

    [Fact]
    public void OptimizedHuffman_MatchesStandard_Reconstruction()
    {
        var rng = new Random(505);
        for (var trial = 0; trial < 10; trial++)
        {
            var w = rng.Next(8, 40);
            var h = rng.Next(8, 40);
            var pixels = new byte[w * h * 3];
            rng.NextBytes(pixels);
            var image = JpegImage.CreateRgb(w, h, pixels);

            var standard = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 }));
            var optimized = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, OptimizeHuffman = true }));
            Assert.Equal(standard.PixelData, optimized.PixelData);
        }
    }

    private static void AssertMeanError(byte[] expected, byte[] actual, double meanTol)
    {
        Assert.Equal(expected.Length, actual.Length);
        long total = 0;
        for (var i = 0; i < expected.Length; i++)
            total += Math.Abs(expected[i] - actual[i]);
        var mean = (double)total / expected.Length;
        Assert.True(mean <= meanTol, $"mean abs error {mean:F2} exceeded {meanTol}");
    }
}

using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class BaselineRoundTripTests
{
    [Fact]
    public void EncodedStream_StartsWithSoi_EndsWithEoi()
    {
        var image = JpegImage.CreateGrayscale(16, 16, Gradient(16, 16));
        var bytes = Jpeg.Encode(image);

        Assert.True(bytes.Length > 4);
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xD8, bytes[1]);
        Assert.Equal(0xFF, bytes[^2]);
        Assert.Equal(0xD9, bytes[^1]);
    }

    [Fact]
    public void Grayscale_RoundTrips_AtHighQuality()
    {
        var pixels = Gradient(32, 24);
        var image = JpegImage.CreateGrayscale(32, 24, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 95 });

        var decoded = Jpeg.Decode(bytes);

        Assert.Equal(32, decoded.Width);
        Assert.Equal(24, decoded.Height);
        Assert.Equal(JpegColorSpace.Grayscale, decoded.ColorSpace);
        AssertCloseEnough(pixels, decoded.PixelData, meanTolerance: 3.0, maxTolerance: 30);
    }

    [Theory]
    [InlineData(ChromaSubsampling.Samp444)]
    [InlineData(ChromaSubsampling.Samp422)]
    [InlineData(ChromaSubsampling.Samp420)]
    [InlineData(ChromaSubsampling.Samp411)]
    public void Rgb_RoundTrips_ForEachSubsampling(ChromaSubsampling subsampling)
    {
        var pixels = ColorGradient(40, 40);
        var image = JpegImage.CreateRgb(40, 40, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 92, Subsampling = subsampling });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(40, decoded.Width);
        Assert.Equal(40, decoded.Height);
        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);
        // Chroma subsampling loses color detail; keep tolerance generous.
        AssertCloseEnough(pixels, decoded.PixelData, meanTolerance: 10.0, maxTolerance: 90);
    }

    [Fact]
    public void OptimizedHuffman_RoundTrips_AndIsSmaller()
    {
        var pixels = Gradient(48, 48);
        var image = JpegImage.CreateGrayscale(48, 48, pixels);

        var standard = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, OptimizeHuffman = false });
        var optimized = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, OptimizeHuffman = true });

        var decoded = Jpeg.Decode(optimized);
        AssertCloseEnough(pixels, decoded.PixelData, meanTolerance: 5.0, maxTolerance: 40);
        Assert.True(optimized.Length <= standard.Length);
    }

    [Fact]
    public void SinglePixel_RoundTrips()
    {
        var image = JpegImage.CreateGrayscale(1, 1, [123]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 100 }));
        Assert.Equal(1, decoded.Width);
        Assert.Equal(1, decoded.Height);
        Assert.InRange(decoded.PixelData[0], 118, 128);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 3)]
    [InlineData(17, 9)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    public void OddDimensions_PreserveSizeAndDecode(int width, int height)
    {
        var pixels = Gradient(width, height);
        var image = JpegImage.CreateGrayscale(width, height, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 }));
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.Equal(width * height, decoded.PixelData.Length);
    }

    [Fact]
    public void LowerQuality_ProducesSmallerFiles()
    {
        var image = JpegImage.CreateRgb(64, 64, ColorGradient(64, 64));
        var high = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 95 });
        var low = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 20 });
        Assert.True(low.Length < high.Length);
    }

    [Fact]
    public void FlatColor_RoundTripsNearlyExact()
    {
        var pixels = new byte[24 * 24 * 3];
        for (var i = 0; i < pixels.Length; i += 3)
        {
            pixels[i] = 200;
            pixels[i + 1] = 100;
            pixels[i + 2] = 50;
        }

        var image = JpegImage.CreateRgb(24, 24, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 100, Subsampling = ChromaSubsampling.Samp444 }));
        AssertCloseEnough(pixels, decoded.PixelData, meanTolerance: 2.0, maxTolerance: 8);
    }

    private static void AssertCloseEnough(byte[] expected, byte[] actual, double meanTolerance, int maxTolerance)
    {
        Assert.Equal(expected.Length, actual.Length);
        long total = 0;
        var max = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            var diff = Math.Abs(expected[i] - actual[i]);
            total += diff;
            if (diff > max)
                max = diff;
        }

        var mean = (double)total / expected.Length;
        Assert.True(mean <= meanTolerance, $"mean abs error {mean:F2} exceeded {meanTolerance}");
        Assert.True(max <= maxTolerance, $"max abs error {max} exceeded {maxTolerance}");
    }

    private static byte[] Gradient(int width, int height)
    {
        var data = new byte[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                data[y * width + x] = (byte)((x * 255 / Math.Max(1, width - 1) + y * 255 / Math.Max(1, height - 1)) / 2);
        return data;
    }

    private static byte[] ColorGradient(int width, int height)
    {
        var data = new byte[width * height * 3];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 3;
                data[i] = (byte)(x * 255 / Math.Max(1, width - 1));
                data[i + 1] = (byte)(y * 255 / Math.Max(1, height - 1));
                data[i + 2] = (byte)((x + y) * 255 / Math.Max(1, width + height - 2));
            }
        return data;
    }
}

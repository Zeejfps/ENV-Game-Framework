using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class EdgeCaseTests
{
    public static IEnumerable<object[]> Dimensions =>
    [
        [1, 1], [1, 17], [17, 1], [2, 2], [3, 3], [7, 5], [8, 8], [9, 9],
        [16, 16], [23, 41], [64, 1], [1, 64], [33, 33], [8, 64], [64, 8],
    ];

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void Grayscale_AllDimensions_PreserveSizeAndDecode(int w, int h)
    {
        var pixels = Ramp(w * h);
        var image = JpegImage.CreateGrayscale(w, h, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 }));
        Assert.Equal(w, decoded.Width);
        Assert.Equal(h, decoded.Height);
        Assert.Equal(w * h, decoded.PixelData.Length);
    }

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void Rgb420_AllDimensions_PreserveSizeAndDecode(int w, int h)
    {
        var pixels = Ramp(w * h * 3);
        var image = JpegImage.CreateRgb(w, h, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp420 }));
        Assert.Equal(w, decoded.Width);
        Assert.Equal(h, decoded.Height);
        Assert.Equal(w * h * 3, decoded.PixelData.Length);
    }

    [Theory]
    [InlineData(ChromaSubsampling.Samp444)]
    [InlineData(ChromaSubsampling.Samp422)]
    [InlineData(ChromaSubsampling.Samp420)]
    [InlineData(ChromaSubsampling.Samp411)]
    public void Progressive_TinyImages_AllSubsampling(ChromaSubsampling subsampling)
    {
        foreach (var (w, h) in new[] { (1, 1), (2, 3), (5, 5), (8, 8), (13, 7) })
        {
            var pixels = Ramp(w * h * 3);
            var image = JpegImage.CreateRgb(w, h, pixels);
            var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = subsampling }));
            var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = subsampling, Progressive = true }));
            Assert.Equal(baseline.PixelData, progressive.PixelData);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void QualityExtremes_RoundTripWithoutError(int quality)
    {
        var image = JpegImage.CreateRgb(32, 32, Ramp(32 * 32 * 3));
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = quality }));
        Assert.Equal(32, decoded.Width);
        Assert.Equal(32, decoded.Height);
    }

    [Fact]
    public void SinglePixel_EachColorSpace()
    {
        Assert.Equal(1, Jpeg.Decode(Jpeg.Encode(JpegImage.CreateGrayscale(1, 1, [200]))).Width);
        Assert.Equal(1, Jpeg.Decode(Jpeg.Encode(JpegImage.CreateRgb(1, 1, [10, 20, 30]))).Width);
        Assert.Equal(1, Jpeg.Decode(Jpeg.Encode(JpegImage.CreateCmyk(1, 1, [10, 20, 30, 40]))).Width);
    }

    [Fact]
    public void SolidColor_RoundTripsNearlyExact()
    {
        var pixels = new byte[40 * 40 * 3];
        for (var i = 0; i < pixels.Length; i += 3)
        {
            pixels[i] = 173;
            pixels[i + 1] = 92;
            pixels[i + 2] = 214;
        }

        var image = JpegImage.CreateRgb(40, 40, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 100, Subsampling = ChromaSubsampling.Samp444 }));

        long total = 0;
        for (var i = 0; i < pixels.Length; i++)
            total += Math.Abs(pixels[i] - decoded.PixelData[i]);
        Assert.True((double)total / pixels.Length <= 2.0);
    }

    [Fact]
    public void LargeImage_RoundTrips()
    {
        var pixels = Ramp(200 * 150 * 3);
        var image = JpegImage.CreateRgb(200, 150, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 }));
        Assert.Equal(200, decoded.Width);
        Assert.Equal(150, decoded.Height);
    }

    private static byte[] Ramp(int length)
    {
        var data = new byte[length];
        for (var i = 0; i < length; i++)
            data[i] = (byte)((i * 17 + 40) % 256);
        return data;
    }
}

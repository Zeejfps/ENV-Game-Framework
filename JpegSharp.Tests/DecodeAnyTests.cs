using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class DecodeAnyTests
{
    [Fact]
    public void DecodeAnyPrecision_ReturnsJpegImage_ForEightBitSource()
    {
        var bytes = Jpeg.Encode(JpegImage.CreateRgb(8, 8, new byte[8 * 8 * 3]));

        IJpegImage image = Jpeg.DecodeAnyPrecision(bytes);
        var concrete = Assert.IsType<JpegImage>(image);
        Assert.Equal(8, image.Precision);
        Assert.Equal(8, concrete.Width);
    }

    [Fact]
    public void DecodeAnyPrecisionFromStream_MatchesByteArrayOverload()
    {
        var bytes = Jpeg.Encode(JpegImage.CreateRgb(8, 8, new byte[8 * 8 * 3]));

        using var ms = new MemoryStream(bytes);
        var image = Jpeg.DecodeAnyPrecisionFromStream(ms);
        Assert.IsType<JpegImage>(image);
        Assert.Equal(8, image.Precision);
    }

    [Fact]
    public async Task DecodeAnyPrecisionFromStreamAsync_ReturnsHighPrecisionForTwelveBit()
    {
        var bytes = Jpeg.Encode16(JpegImage16.CreateGrayscale(8, 8, 12, new ushort[64]));

        using var ms = new MemoryStream(bytes);
        var image = await Jpeg.DecodeAnyPrecisionFromStreamAsync(ms);
        Assert.IsType<JpegImage16>(image);
        Assert.Equal(12, image.Precision);
    }

    [Fact]
    public void IJpegImage_ToRgba8888_MatchesExtension()
    {
        var image = JpegImage.CreateRgb(1, 1, [10, 20, 30]);
        IJpegImage asInterface = image;

        Assert.Equal(image.ToRgba8888(), asInterface.ToRgba8888());
    }

    [Fact]
    public void JpegImage_PrecisionIsEight()
    {
        Assert.Equal(8, JpegImage.CreateGrayscale(1, 1, [0]).Precision);
    }
}

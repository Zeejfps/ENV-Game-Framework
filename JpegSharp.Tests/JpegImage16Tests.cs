using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class JpegImage16Tests
{
    [Fact]
    public void Constructor_SetsPropertiesAndMaxSampleValue()
    {
        var image = JpegImage16.CreateGrayscale(2, 3, 12, new ushort[6]);

        Assert.Equal(2, image.Width);
        Assert.Equal(3, image.Height);
        Assert.Equal(JpegColorSpace.Grayscale, image.ColorSpace);
        Assert.Equal(1, image.ComponentCount);
        Assert.Equal(12, image.Precision);
        Assert.Equal(4095, image.MaxSampleValue);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(17)]
    public void Constructor_RejectsPrecisionOutsideRange(int precision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => JpegImage16.CreateGrayscale(1, 1, precision, new ushort[1]));
    }

    [Fact]
    public void Constructor_RejectsMismatchedLength()
    {
        Assert.Throws<ArgumentException>(() => JpegImage16.CreateRgb(2, 2, 12, new ushort[10]));
    }

    [Fact]
    public void ImplementsIJpegImage()
    {
        IJpegImage image = JpegImage16.CreateRgb(1, 1, 12, [4095, 0, 2048]);
        Assert.Equal(12, image.Precision);
        Assert.Equal(3, image.ComponentCount);
    }

    [Fact]
    public void To8Bit_RightShiftsByPrecisionMinusEight()
    {
        // 12-bit sample 0xABC (2748) >> 4 == 0xAB (171).
        var image = JpegImage16.CreateGrayscale(1, 1, 12, [0xABC]);
        var eight = image.To8Bit();

        Assert.Equal(8, eight.Precision);
        Assert.Equal(JpegColorSpace.Grayscale, eight.ColorSpace);
        Assert.Equal(0xAB, eight.PixelData[0]);
    }

    [Fact]
    public void To8Bit_CarriesMetadata()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add("hi");
        var image = JpegImage16.CreateGrayscale(1, 1, 12, [0]);
        image.Metadata = metadata;

        Assert.Same(metadata, image.To8Bit().Metadata);
    }

    [Fact]
    public void ToRgba8888_MatchesTo8BitThenPack()
    {
        var image = JpegImage16.CreateRgb(2, 1, 12, [4095, 2048, 0, 100, 200, 300]);
        Assert.Equal(image.To8Bit().ToRgba8888(), image.ToRgba8888());
    }

    [Fact]
    public void Precision16_MaxSampleValueIs65535()
    {
        var image = JpegImage16.CreateGrayscale(1, 1, 16, [65535]);
        Assert.Equal(65535, image.MaxSampleValue);
        Assert.Equal(0xFF, image.To8Bit().PixelData[0]);
    }
}

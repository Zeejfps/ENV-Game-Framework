using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ToRgbTests
{
    [Fact]
    public void Rgb_ProducesIndependentCopy()
    {
        var image = JpegImage.CreateRgb(2, 1, [10, 20, 30, 40, 50, 60]);
        var rgb = image.ToRgb();

        Assert.Equal(JpegColorSpace.Rgb, rgb.ColorSpace);
        Assert.Equal(image.PixelData, rgb.PixelData);
        Assert.NotSame(image.PixelData, rgb.PixelData);

        // Mutating the copy must not affect the original.
        rgb.PixelData[0] = 99;
        Assert.Equal(10, image.PixelData[0]);
    }

    [Fact]
    public void Grayscale_ExpandsLuminanceAcrossChannels()
    {
        var image = JpegImage.CreateGrayscale(2, 1, [50, 200]);
        var rgb = image.ToRgb();

        Assert.Equal(JpegColorSpace.Rgb, rgb.ColorSpace);
        Assert.Equal(3, rgb.ComponentCount);
        Assert.Equal([50, 50, 50, 200, 200, 200], rgb.PixelData);
    }

    [Fact]
    public void Cmyk_ConvertsWithColorConverter()
    {
        const byte c = 40, m = 80, y = 120, k = 30;
        var image = JpegImage.CreateCmyk(1, 1, [c, m, y, k]);
        var rgb = image.ToRgb();

        JpegSharp.Color.ColorConverter.CmykToRgb(c, m, y, k, out var er, out var eg, out var eb);
        Assert.Equal([er, eg, eb], rgb.PixelData);
    }

    [Fact]
    public void PreservesDimensions()
    {
        var image = JpegImage.CreateCmyk(7, 3, new byte[7 * 3 * 4]);
        var rgb = image.ToRgb();

        Assert.Equal(7, rgb.Width);
        Assert.Equal(3, rgb.Height);
        Assert.Equal(7 * 3 * 3, rgb.PixelData.Length);
    }

    [Fact]
    public void CarriesMetadataOver()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add("hello");
        var image = JpegImage.CreateGrayscale(1, 1, [128]);
        image.Metadata = metadata;

        var rgb = image.ToRgb();
        Assert.Same(metadata, rgb.Metadata);
    }

    [Fact]
    public void MatchesPackedRgbaChannels()
    {
        // ToRgb and the packing path should agree channel-for-channel on a CMYK source.
        var image = JpegImage.CreateCmyk(1, 1, [200, 60, 120, 30]);
        var rgb = image.ToRgb();
        var packed = image.ToRgba8888()[0];

        Assert.Equal((packed >> 24) & 0xFF, rgb.PixelData[0]);
        Assert.Equal((packed >> 16) & 0xFF, rgb.PixelData[1]);
        Assert.Equal((packed >> 8) & 0xFF, rgb.PixelData[2]);
    }
}

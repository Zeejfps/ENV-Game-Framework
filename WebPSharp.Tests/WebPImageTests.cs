using WebPSharp.Api;

namespace WebPSharp.Tests;

public class WebPImageTests
{
    [Fact]
    public void CreateRgb_SetsProperties()
    {
        var img = WebPImage.CreateRgb(2, 3, new byte[2 * 3 * 3]);
        Assert.Equal(2, img.Width);
        Assert.Equal(3, img.Height);
        Assert.Equal(WebPColorFormat.Rgb, img.Format);
        Assert.Equal(3, img.ComponentCount);
        Assert.False(img.HasAlpha);
        Assert.Equal(6, img.Stride);
    }

    [Fact]
    public void CreateRgba_SetsProperties()
    {
        var img = WebPImage.CreateRgba(4, 4, new byte[4 * 4 * 4]);
        Assert.Equal(WebPColorFormat.Rgba, img.Format);
        Assert.Equal(4, img.ComponentCount);
        Assert.True(img.HasAlpha);
        Assert.Equal(16, img.Stride);
    }

    [Fact]
    public void Constructor_NullPixels_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WebPImage(1, 1, WebPColorFormat.Rgba, null!));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 5)]
    public void Constructor_NonPositiveDimension_Throws(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WebPImage(width, height, WebPColorFormat.Rgba, new byte[16]));
    }

    [Fact]
    public void Constructor_WrongBufferLength_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new WebPImage(2, 2, WebPColorFormat.Rgba, new byte[10]));
    }

    [Fact]
    public void ComponentsFor_MatchesFormat()
    {
        Assert.Equal(3, WebPImage.ComponentsFor(WebPColorFormat.Rgb));
        Assert.Equal(4, WebPImage.ComponentsFor(WebPColorFormat.Rgba));
    }
}

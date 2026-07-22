using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class DecodeAnyTests
{
    [Fact]
    public void DecodeAny_ReturnsJpegImage_ForEightBitSource()
    {
        var bytes = Jpeg.Encode(JpegImage.CreateRgb(8, 8, new byte[8 * 8 * 3]));

        IJpegImage image = Jpeg.DecodeAny(bytes);
        var concrete = Assert.IsType<JpegImage>(image);
        Assert.Equal(8, image.Precision);
        Assert.Equal(8, concrete.Width);
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

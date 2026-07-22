using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class DimensionLimitTests
{
    [Fact]
    public void WidthAboveJpegMaximum_ThrowsRatherThanCorrupting()
    {
        var image = JpegImage.CreateGrayscale(65536, 1, new byte[65536]);
        Assert.Throws<ArgumentException>(() => Jpeg.Encode(image));
    }

    [Fact]
    public void HeightAboveJpegMaximum_ThrowsRatherThanCorrupting()
    {
        var image = JpegImage.CreateGrayscale(1, 70000, new byte[70000]);
        Assert.Throws<ArgumentException>(() => Jpeg.Encode(image));
    }

    [Fact]
    public void MaximumAllowedDimension_Encodes()
    {
        // 65535 x 1 is exactly the JPEG limit and must encode + decode with valid dimensions.
        var image = JpegImage.CreateGrayscale(65535, 1, new byte[65535]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 50 });

        var info = Jpeg.Identify(bytes);
        Assert.Equal(65535, info.Width);
        Assert.Equal(1, info.Height);

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(65535, decoded.Width);
        Assert.Equal(1, decoded.Height);
    }
}

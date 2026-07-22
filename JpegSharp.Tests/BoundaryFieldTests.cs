using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class BoundaryFieldTests
{
    [Fact]
    public void RestartInterval_Above16Bit_Throws()
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        Assert.Throws<ArgumentException>(() => Jpeg.Encode(image, new JpegEncoderOptions { RestartInterval = 70000 }));
    }

    [Fact]
    public void RestartInterval_AtMax_EncodesAndDecodes()
    {
        // 65535 is the DRI field maximum; on a small image no restart marker is emitted, but
        // the file must still round-trip (the previous truncation bug would desync).
        var pixels = new byte[32 * 32];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(i % 256);
        var image = JpegImage.CreateGrayscale(32, 32, pixels);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, RestartInterval = 65535 }));
        Assert.Equal(32, decoded.Width);
    }

    [Fact]
    public void JfifDensity_AboveMax_IsClampedNotTruncated()
    {
        // 70000 & 0xFFFF = 4464 (truncation). Clamping keeps a valid, large value instead.
        var metadata = new JpegMetadata { Density = new JfifDensity(JpegDensityUnit.DotsPerInch, 70000, 90000) };
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));

        var density = decoded.Metadata!.Density!.Value;
        Assert.Equal(65535, density.X);
        Assert.Equal(65535, density.Y);
        Assert.NotEqual(4464, density.X); // guard against the old truncation
    }

    [Fact]
    public void NormalDensity_RoundTripsExactly()
    {
        var metadata = new JpegMetadata { Density = new JfifDensity(JpegDensityUnit.DotsPerCentimeter, 300, 300) };
        var image = JpegImage.CreateGrayscale(8, 8, new byte[64]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));
        Assert.Equal(new JfifDensity(JpegDensityUnit.DotsPerCentimeter, 300, 300), decoded.Metadata!.Density);
    }
}

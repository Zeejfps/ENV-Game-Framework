using JpegSharp.Color;
using Xunit;

namespace JpegSharp.Tests;

public class HighPrecisionColorTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(4095, 4095, 4095)]
    [InlineData(4095, 0, 0)]
    [InlineData(2048, 1000, 3000)]
    public void YCbCr12_RoundTripsWithinTolerance(int r, int g, int b)
    {
        const int max = 4095;
        ColorConverter.RgbToYCbCr(r, g, b, max, out var y, out var cb, out var cr);
        ColorConverter.YCbCrToRgb(y, cb, cr, max, out var r2, out var g2, out var b2);

        Assert.True(Math.Abs(r - r2) <= 3, $"R {r} -> {r2}");
        Assert.True(Math.Abs(g - g2) <= 3, $"G {g} -> {g2}");
        Assert.True(Math.Abs(b - b2) <= 3, $"B {b} -> {b2}");
    }

    [Fact]
    public void YCbCr12_GrayIsAchromatic()
    {
        // A neutral gray must map to Y=gray, Cb=Cr=center (2048).
        ColorConverter.RgbToYCbCr(2048, 2048, 2048, 4095, out var y, out var cb, out var cr);
        Assert.InRange(y, 2047, 2049);
        Assert.Equal(2048, cb);
        Assert.Equal(2048, cr);
    }

    [Fact]
    public void Upsample16_IsExactWhenDimensionsMatch()
    {
        ushort[] src = [10, 4000, 2048, 0];
        var dst = new ushort[4];
        ChromaSampler.UpsampleLinear(src, 2, 2, dst, 2, 2, 4095);
        Assert.Equal(src, dst);
    }

    [Fact]
    public void Upsample16_DoublesResolutionAndStaysInRange()
    {
        ushort[] src = [0, 4095];
        var dst = new ushort[4];
        ChromaSampler.UpsampleLinear(src, 2, 1, dst, 4, 1, 4095);

        // Endpoints preserved; interior interpolated and clamped within [0, 4095].
        Assert.Equal(0, dst[0]);
        Assert.Equal(4095, dst[3]);
        foreach (var v in dst)
            Assert.InRange(v, 0, 4095);
    }
}

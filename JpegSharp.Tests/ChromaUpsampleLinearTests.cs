using JpegSharp.Color;
using Xunit;

namespace JpegSharp.Tests;

public class ChromaUpsampleLinearTests
{
    [Fact]
    public void Identity_WhenDimensionsMatch()
    {
        byte[] src = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        var dst = new byte[9];
        ChromaSampler.UpsampleLinear(src, 3, 3, dst, 3, 3);
        Assert.Equal(src, dst);
    }

    [Fact]
    public void ConstantPlane_UpsamplesToConstant()
    {
        var src = new byte[16];
        Array.Fill(src, (byte)137);
        var dst = new byte[64];
        ChromaSampler.UpsampleLinear(src, 4, 4, dst, 8, 8);
        Assert.All(dst, v => Assert.Equal(137, v));
    }

    [Fact]
    public void LinearRamp_InterpolatesSmoothly_NotBlocky()
    {
        // A 2-pixel row [0, 255] upsampled to 4 pixels should produce intermediate values,
        // unlike nearest-neighbour replication which would give [0, 0, 255, 255].
        byte[] src = [0, 255];
        var dst = new byte[4];
        ChromaSampler.UpsampleLinear(src, 2, 1, dst, 4, 1);

        Assert.True(dst[1] > dst[0], "expected interpolation, not replication");
        Assert.True(dst[2] > dst[1]);
        Assert.True(dst[3] >= dst[2]);
        Assert.InRange(dst[1], 1, 128);
        Assert.InRange(dst[2], 128, 254);
    }

    [Fact]
    public void MidpointOfTwoValues_IsAveraged()
    {
        // 2x1 -> 3x1: the centre output sample sits exactly between the two inputs.
        byte[] src = [40, 120];
        var dst = new byte[3];
        ChromaSampler.UpsampleLinear(src, 2, 1, dst, 3, 1);
        Assert.Equal(80, dst[1]); // (40 + 120) / 2
    }

    [Fact]
    public void OutputStaysInByteRange()
    {
        var rng = new Random(9);
        var src = new byte[64];
        rng.NextBytes(src);
        var dst = new byte[16 * 16];
        ChromaSampler.UpsampleLinear(src, 8, 8, dst, 16, 16);
        Assert.All(dst, v => Assert.InRange(v, 0, 255));
    }
}

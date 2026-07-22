using JpegSharp.Color;
using Xunit;

namespace JpegSharp.Tests;

public class ChromaSamplerTests
{
    [Fact]
    public void SubsampledSize_RoundsUp()
    {
        Assert.Equal(4, ChromaSampler.SubsampledSize(8, 2));
        Assert.Equal(2, ChromaSampler.SubsampledSize(3, 2)); // ceil(3/2)
        Assert.Equal(2, ChromaSampler.SubsampledSize(5, 4)); // ceil(5/4)
        Assert.Equal(8, ChromaSampler.SubsampledSize(8, 1)); // identity
    }

    [Fact]
    public void Downsample_444_IsIdentity()
    {
        byte[] src = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        var dst = new byte[9];
        ChromaSampler.Downsample(src, 3, 3, 1, 1, dst, 3, 3);
        Assert.Equal(src, dst);
    }

    [Fact]
    public void Downsample_420_AveragesTwoByTwoBlocks()
    {
        // 4x4 plane
        byte[] src =
        [
            10, 20, 100, 100,
            30, 40, 100, 100,
            0, 0, 200, 220,
            0, 0, 240, 252,
        ];

        var dst = new byte[4];
        ChromaSampler.Downsample(src, 4, 4, 2, 2, dst, 2, 2);

        // Block (0,0): (10+20+30+40)/4 = 25
        Assert.Equal(25, dst[0]);
        // Block (0,1): (100+100+100+100)/4 = 100
        Assert.Equal(100, dst[1]);
        // Block (1,0): all zero -> 0
        Assert.Equal(0, dst[2]);
        // Block (1,1): (200+220+240+252)/4 = 228
        Assert.Equal(228, dst[3]);
    }

    [Fact]
    public void Upsample_420_ReplicatesPixels()
    {
        byte[] src = [10, 20, 30, 40]; // 2x2
        var dst = new byte[16];         // 4x4
        ChromaSampler.Upsample(src, 2, 2, 2, 2, dst, 4, 4);

        byte[] expected =
        [
            10, 10, 20, 20,
            10, 10, 20, 20,
            30, 30, 40, 40,
            30, 30, 40, 40,
        ];
        Assert.Equal(expected, dst);
    }

    [Fact]
    public void Upsample_422_ReplicatesHorizontallyOnly()
    {
        byte[] src = [10, 20, 30, 40]; // 2x2 stored, full is 4x2
        var dst = new byte[8];
        ChromaSampler.Upsample(src, 2, 2, 2, 1, dst, 4, 2);

        byte[] expected =
        [
            10, 10, 20, 20,
            30, 30, 40, 40,
        ];
        Assert.Equal(expected, dst);
    }

    [Fact]
    public void ConstantPlane_DownsampleThenUpsample_IsExact()
    {
        var full = new byte[8 * 8];
        Array.Fill(full, (byte)137);

        var sub = new byte[ChromaSampler.SubsampledSize(8, 2) * ChromaSampler.SubsampledSize(8, 2)];
        ChromaSampler.Downsample(full, 8, 8, 2, 2, sub, 4, 4);

        var restored = new byte[64];
        ChromaSampler.Upsample(sub, 4, 4, 2, 2, restored, 8, 8);

        Assert.All(restored, v => Assert.Equal(137, v));
    }

    [Fact]
    public void Downsample_OddDimensions_ClampsBlocksAtEdges()
    {
        // 3x3 with 2x2 factor -> 2x2 output.
        byte[] src =
        [
            10, 20, 30,
            40, 50, 60,
            70, 80, 90,
        ];
        var dst = new byte[4];
        ChromaSampler.Downsample(src, 3, 3, 2, 2, dst, 2, 2);

        Assert.Equal((10 + 20 + 40 + 50) / 4, dst[0]); // 30
        Assert.Equal((30 + 60 + 1) / 2, dst[1]);        // edge col: (30+60)/2 = 45
        Assert.Equal((70 + 80 + 1) / 2, dst[2]);        // edge row: (70+80)/2 = 75
        Assert.Equal(90, dst[3]);                       // single corner pixel
    }

    [Fact]
    public void Upsample_OddDimensions_ClampsToSourceBounds()
    {
        // 2x2 source upsampled to 3x3 target (factor 2).
        byte[] src = [10, 20, 30, 40];
        var dst = new byte[9];
        ChromaSampler.Upsample(src, 2, 2, 2, 2, dst, 3, 3);

        byte[] expected =
        [
            10, 10, 20,
            10, 10, 20,
            30, 30, 40,
        ];
        Assert.Equal(expected, dst);
    }

    [Fact]
    public void Downsample_411_AveragesFourWide()
    {
        byte[] src = [40, 60, 80, 100, 4, 8, 12, 16]; // 8x1
        var dst = new byte[2];
        ChromaSampler.Downsample(src, 8, 1, 4, 1, dst, 2, 1);
        Assert.Equal((40 + 60 + 80 + 100) / 4, dst[0]); // 70
        Assert.Equal((4 + 8 + 12 + 16) / 4, dst[1]);     // 10
    }

    [Fact]
    public void InvalidFactor_Throws()
    {
        var dst = new byte[4];
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ChromaSampler.Downsample(new byte[4], 2, 2, 0, 1, dst, 2, 2));
    }

    [Fact]
    public void Downsample_OversizedDst_ThrowsValidationNotDivideByZero()
    {
        // 4x4 source with 2x2 factor -> subsampled size is 2x2; a 3x3 dst is oversized
        // and would divide by count==0 for out-of-range blocks without validation.
        var src = new byte[16];
        var dst = new byte[9];
        Assert.Throws<ArgumentException>(
            () => ChromaSampler.Downsample(src, 4, 4, 2, 2, dst, 3, 3));

        var srcU = new ushort[16];
        var dstU = new ushort[9];
        Assert.Throws<ArgumentException>(
            () => ChromaSampler.Downsample(srcU, 4, 4, 2, 2, dstU, 3, 3));
    }
}

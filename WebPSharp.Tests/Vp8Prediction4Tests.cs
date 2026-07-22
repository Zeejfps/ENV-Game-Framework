using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8Prediction4Tests
{
    private static byte[] Predict(int mode, byte[] top, byte[] left, byte corner)
    {
        var dst = new byte[16];
        Vp8Prediction4.Predict(mode, dst, 4, top, left, corner);
        return dst;
    }

    private static int Avg2(int a, int b) => (a + b + 1) >> 1;
    private static int Avg3(int a, int b, int c) => (a + 2 * b + c + 2) >> 2;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void UniformNeighbors_ProduceUniformBlock(int mode)
    {
        const byte k = 137;
        var top = new byte[8];
        var left = new byte[4];
        Array.Fill(top, k);
        Array.Fill(left, k);

        var dst = Predict(mode, top, left, k);
        Assert.All(dst, v => Assert.Equal(k, v));
    }

    [Fact]
    public void Dc_AveragesTopAndLeftPlusRounding()
    {
        var top = new byte[] { 10, 20, 30, 40, 0, 0, 0, 0 };
        var left = new byte[] { 50, 60, 70, 80 };
        var expected = (10 + 20 + 30 + 40 + 50 + 60 + 70 + 80 + 4) >> 3;
        var dst = Predict(0, top, left, 0);
        Assert.All(dst, v => Assert.Equal(expected, v));
    }

    [Fact]
    public void TrueMotion_IsLeftPlusTopMinusCorner()
    {
        var top = new byte[] { 100, 110, 120, 130, 0, 0, 0, 0 };
        var left = new byte[] { 50, 60, 70, 80 };
        byte corner = 90;
        var dst = Predict(1, top, left, corner);
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
        {
            var e = left[y] + top[x] - corner;
            e = e < 0 ? 0 : e > 255 ? 255 : e;
            Assert.Equal(e, dst[y * 4 + x]);
        }
    }

    [Fact]
    public void Vertical_UsesSmoothedTopColumns()
    {
        var top = new byte[] { 10, 20, 30, 40, 50, 0, 0, 0 };
        byte x = 5;
        var dst = Predict(2, top, new byte[4], x);
        var cols = new[]
        {
            Avg3(x, top[0], top[1]),
            Avg3(top[0], top[1], top[2]),
            Avg3(top[1], top[2], top[3]),
            Avg3(top[2], top[3], top[4]),
        };
        for (var row = 0; row < 4; row++)
        for (var col = 0; col < 4; col++)
            Assert.Equal(cols[col], dst[row * 4 + col]);
    }

    [Fact]
    public void Horizontal_UsesSmoothedLeftRows()
    {
        var left = new byte[] { 10, 20, 30, 40 };
        byte corner = 5;
        var dst = Predict(3, new byte[8], left, corner);
        var rows = new[]
        {
            Avg3(corner, left[0], left[1]),
            Avg3(left[0], left[1], left[2]),
            Avg3(left[1], left[2], left[3]),
            Avg3(left[2], left[3], left[3]),
        };
        for (var row = 0; row < 4; row++)
        for (var col = 0; col < 4; col++)
            Assert.Equal(rows[row], dst[row * 4 + col]);
    }

    [Fact]
    public void DownLeft_MatchesHandComputedCorners()
    {
        var top = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 };
        var dst = Predict(4, top, new byte[4], 0);
        // DST(0,0) = avg3(A,B,C)
        Assert.Equal(Avg3(10, 20, 30), dst[0 * 4 + 0]);
        // DST(3,3) = avg3(G,H,H)
        Assert.Equal(Avg3(70, 80, 80), dst[3 * 4 + 3]);
        // DST(3,0)=DST(2,1)=DST(1,2)=DST(0,3) = avg3(D,E,F)
        var v = Avg3(40, 50, 60);
        Assert.Equal(v, dst[0 * 4 + 3]);
        Assert.Equal(v, dst[1 * 4 + 2]);
        Assert.Equal(v, dst[2 * 4 + 1]);
        Assert.Equal(v, dst[3 * 4 + 0]);
    }

    [Fact]
    public void DownRight_MatchesHandComputedDiagonal()
    {
        var top = new byte[] { 10, 20, 30, 40, 0, 0, 0, 0 };
        var left = new byte[] { 60, 70, 80, 90 };
        byte x = 50;
        var dst = Predict(5, top, left, x);
        // Main diagonal DST(0,0)=DST(1,1)=DST(2,2)=DST(3,3) = avg3(A,X,I)
        var diag = Avg3(10, 50, 60);
        Assert.Equal(diag, dst[0 * 4 + 0]);
        Assert.Equal(diag, dst[1 * 4 + 1]);
        Assert.Equal(diag, dst[2 * 4 + 2]);
        Assert.Equal(diag, dst[3 * 4 + 3]);
    }

    [Fact]
    public void InvalidMode_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Predict(10, new byte[8], new byte[4], 0));
    }
}

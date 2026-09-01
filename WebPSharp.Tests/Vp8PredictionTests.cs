using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8PredictionTests
{
    private static (byte[] dst, int stride) NewBlock(int size)
    {
        var stride = size;
        return (new byte[size * size], stride);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(8)]
    public void Dc_BothAvailable_AveragesTopAndLeft(int size)
    {
        var top = new byte[size];
        var left = new byte[size];
        Array.Fill(top, (byte)100);
        Array.Fill(left, (byte)100);
        var (dst, stride) = NewBlock(size);

        Vp8Prediction.FillDc(dst, stride, size, top, left, hasTop: true, hasLeft: true);
        Assert.All(dst, v => Assert.Equal(100, v));
    }

    [Theory]
    [InlineData(16, 40)]
    [InlineData(8, 24)]
    public void Dc_TopOnly_AveragesTop(int size, byte value)
    {
        var top = new byte[size];
        Array.Fill(top, value);
        var (dst, stride) = NewBlock(size);

        Vp8Prediction.FillDc(dst, stride, size, top, ReadOnlySpan<byte>.Empty, hasTop: true, hasLeft: false);
        Assert.All(dst, v => Assert.Equal(value, v));
    }

    [Theory]
    [InlineData(16, 77)]
    [InlineData(8, 12)]
    public void Dc_LeftOnly_AveragesLeft(int size, byte value)
    {
        var left = new byte[size];
        Array.Fill(left, value);
        var (dst, stride) = NewBlock(size);

        Vp8Prediction.FillDc(dst, stride, size, ReadOnlySpan<byte>.Empty, left, hasTop: false, hasLeft: true);
        Assert.All(dst, v => Assert.Equal(value, v));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(8)]
    public void Dc_NeitherAvailable_Is128(int size)
    {
        var (dst, stride) = NewBlock(size);
        Vp8Prediction.FillDc(dst, stride, size, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, false, false);
        Assert.All(dst, v => Assert.Equal(128, v));
    }

    [Fact]
    public void Vertical_CopiesTopRowToEveryRow()
    {
        const int size = 8;
        var top = new byte[size];
        for (var i = 0; i < size; i++) top[i] = (byte)(i * 10);
        var (dst, stride) = NewBlock(size);

        Vp8Prediction.FillVertical(dst, stride, size, top);
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                Assert.Equal(top[x], dst[y * stride + x]);
    }

    [Fact]
    public void Horizontal_CopiesLeftColumnAcrossEachRow()
    {
        const int size = 8;
        var left = new byte[size];
        for (var i = 0; i < size; i++) left[i] = (byte)(i * 7 + 3);
        var (dst, stride) = NewBlock(size);

        Vp8Prediction.FillHorizontal(dst, stride, size, left);
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                Assert.Equal(left[y], dst[y * stride + x]);
    }

    [Fact]
    public void TrueMotion_IsLeftPlusTopMinusCorner_Clamped()
    {
        const int size = 4;
        var top = new byte[] { 100, 110, 120, 130 };
        var left = new byte[] { 50, 60, 70, 80 };
        byte corner = 90;
        var (dst, stride) = NewBlock(size);

        Vp8Prediction.FillTrueMotion(dst, stride, size, top, left, corner);
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var expected = left[y] + top[x] - corner;
                expected = expected < 0 ? 0 : expected > 255 ? 255 : expected;
                Assert.Equal(expected, dst[y * stride + x]);
            }
    }

    [Fact]
    public void TrueMotion_ClampsOutOfRange()
    {
        const int size = 4;
        var top = new byte[] { 255, 255, 0, 0 };
        var left = new byte[] { 255, 0, 255, 0 };
        byte corner = 0;
        var (dst, stride) = NewBlock(size);

        Vp8Prediction.FillTrueMotion(dst, stride, size, top, left, corner);
        // top=255, left=255, corner=0 -> 510 clamps to 255
        Assert.Equal(255, dst[0]);
        // top=0 (x=2), left=0 (y=3) -> 0
        Assert.Equal(0, dst[3 * stride + 2]);
    }
}

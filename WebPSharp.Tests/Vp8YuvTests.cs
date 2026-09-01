using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8YuvTests
{
    private static (int r, int g, int b) Reference(int y, int u, int v)
    {
        int MultHi(int val, int coeff) => (val * coeff) >> 8;
        int Clip8(int val) => (val & ~((256 << 6) - 1)) == 0 ? val >> 6 : val < 0 ? 0 : 255;
        var r = Clip8(MultHi(y, 19077) + MultHi(v, 26149) - 14234);
        var g = Clip8(MultHi(y, 19077) - MultHi(u, 6419) - MultHi(v, 13320) + 8708);
        var b = Clip8(MultHi(y, 19077) + MultHi(u, 33050) - 17685);
        return (r, g, b);
    }

    [Fact]
    public void NeutralChroma_ProducesGrayscale()
    {
        for (var y = 0; y <= 255; y += 5)
        {
            Vp8Yuv.YuvToRgb(y, 128, 128, out var r, out var g, out var b);
            Assert.True(Math.Abs(r - g) <= 2 && Math.Abs(g - b) <= 2,
                $"Y={y} produced non-gray ({r},{g},{b}).");
        }
    }

    [Fact]
    public void MaxLuma_NeutralChroma_IsWhite()
    {
        Vp8Yuv.YuvToRgb(255, 128, 128, out var r, out var g, out var b);
        Assert.Equal(255, r);
        Assert.Equal(255, g);
        Assert.Equal(255, b);
    }

    [Fact]
    public void MinLuma_NeutralChroma_IsBlack()
    {
        Vp8Yuv.YuvToRgb(0, 128, 128, out var r, out var g, out var b);
        Assert.Equal(0, r);
        Assert.Equal(0, g);
        Assert.Equal(0, b);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MatchesReferenceFormula_OverManySamples(int seed)
    {
        var rng = new Random(seed);
        for (var i = 0; i < 5000; i++)
        {
            var y = rng.Next(256);
            var u = rng.Next(256);
            var v = rng.Next(256);
            var (er, eg, eb) = Reference(y, u, v);
            Vp8Yuv.YuvToRgb(y, u, v, out var r, out var g, out var b);
            Assert.Equal(er, r);
            Assert.Equal(eg, g);
            Assert.Equal(eb, b);
        }
    }

    [Fact]
    public void OutputsAreAlwaysInByteRange()
    {
        for (var y = 0; y <= 255; y += 15)
        for (var u = 0; u <= 255; u += 15)
        for (var v = 0; v <= 255; v += 15)
        {
            Vp8Yuv.YuvToRgb(y, u, v, out var r, out var g, out var b);
            Assert.InRange(r, 0, 255);
            Assert.InRange(g, 0, 255);
            Assert.InRange(b, 0, 255);
        }
    }
}

using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8TransformTests
{
    [Fact]
    public void InverseDct_Zero_ProducesZero()
    {
        var residual = new int[16];
        Vp8Transform.InverseDct(new short[16], residual);
        Assert.All(residual, v => Assert.Equal(0, v));
    }

    [Theory]
    [InlineData(8, 1)]    // (8+4)>>3 = 1
    [InlineData(32, 4)]   // (32+4)>>3 = 4
    [InlineData(100, 13)] // (100+4)>>3 = 13
    public void InverseDct_DcOnly_ProducesUniformBlock(short dc, int expected)
    {
        var coeffs = new short[16];
        coeffs[0] = dc;
        var residual = new int[16];
        Vp8Transform.InverseDct(coeffs, residual);
        Assert.All(residual, v => Assert.Equal(expected, v));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(99)]
    public void ForwardThenInverseDct_RecoversResidual(int seed)
    {
        var rng = new Random(seed);
        for (var trial = 0; trial < 200; trial++)
        {
            var residual = new int[16];
            for (var i = 0; i < 16; i++)
                residual[i] = rng.Next(-255, 256);

            var coeffs = new short[16];
            Vp8Transform.ForwardDct(residual, coeffs);
            var recovered = new int[16];
            Vp8Transform.InverseDct(coeffs, recovered);

            for (var i = 0; i < 16; i++)
                Assert.True(Math.Abs(recovered[i] - residual[i]) <= 2,
                    $"residual[{i}]={residual[i]} recovered={recovered[i]}");
        }
    }

    [Fact]
    public void InverseWht_Zero_ProducesZero()
    {
        var output = new short[16];
        Vp8Transform.InverseWht(new short[16], output);
        Assert.All(output, v => Assert.Equal(0, v));
    }

    [Theory]
    [InlineData(8, 1)]    // (8+3)>>3 = 1
    [InlineData(40, 5)]   // (40+3)>>3 = 5
    public void InverseWht_DcOnly_ProducesUniform(short dc, short expected)
    {
        var input = new short[16];
        input[0] = dc;
        var output = new short[16];
        Vp8Transform.InverseWht(input, output);
        Assert.All(output, v => Assert.Equal(expected, v));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void ForwardThenInverseWht_RecoversInput(int seed)
    {
        var rng = new Random(seed);
        for (var trial = 0; trial < 200; trial++)
        {
            var input = new short[16];
            for (var i = 0; i < 16; i++)
                input[i] = (short)rng.Next(-512, 513);

            var forward = new short[16];
            Vp8Transform.ForwardWht(input, forward);
            var recovered = new short[16];
            Vp8Transform.InverseWht(forward, recovered);

            for (var i = 0; i < 16; i++)
                Assert.True(Math.Abs(recovered[i] - input[i]) <= 1,
                    $"input[{i}]={input[i]} recovered={recovered[i]}");
        }
    }

    [Fact]
    public void AddResidual_ClipsToByteRange()
    {
        var block = new byte[16];
        Array.Fill(block, (byte)250);
        var residual = new int[16];
        residual[0] = 100;   // 250+100 -> clip 255
        residual[1] = -300;  // 250-300 -> clip 0
        residual[2] = 3;     // 253

        Vp8Transform.AddResidual(block, 4, residual);
        Assert.Equal(255, block[0]);
        Assert.Equal(0, block[1]);
        Assert.Equal(253, block[2]);
    }
}

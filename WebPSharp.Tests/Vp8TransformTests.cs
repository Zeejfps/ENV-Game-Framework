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

    // Known-answer: a block with DC plus two AC coefficients. Expected pixels hand-computed from the
    // exact fixed-point formulas Mul1(a)=a+((a*20091)>>16), Mul2(a)=(a*35468)>>16, column pass then
    // row pass with the +4 rounding bias and final >>3. This pins both multiplier constants.
    [Fact]
    public void InverseDct_KnownAnswer_PinsMultiplierConstants()
    {
        var coeffs = new short[16];
        coeffs[0] = 200;
        coeffs[1] = -40;
        coeffs[4] = 30;
        coeffs[5] = 10;

        var residual = new int[16];
        Vp8Transform.InverseDct(coeffs, residual);

        var expected = new[]
        {
            25, 28, 32, 34,
            21, 25, 29, 33,
            16, 20, 26, 30,
            11, 17, 24, 29,
        };
        Assert.Equal(expected, residual);
    }

    // Known-answer: a single AC coefficient at raster position 1 exercises Mul1 and Mul2 in isolation.
    // Mul1(64) = 64 + ((64*20091)>>16) = 83 and Mul2(64) = (64*35468)>>16 = 34, so each row becomes
    // ((4+83)>>3, (4+34)>>3, (4-34)>>3, (4-83)>>3) = (10, 4, -4, -10). A wrong constant shifts these.
    [Fact]
    public void InverseDct_SingleAcCoeff_PinsMul1AndMul2()
    {
        var coeffs = new short[16];
        coeffs[1] = 64;

        var residual = new int[16];
        Vp8Transform.InverseDct(coeffs, residual);

        var expected = new[]
        {
            10, 4, -4, -10,
            10, 4, -4, -10,
            10, 4, -4, -10,
            10, 4, -4, -10,
        };
        Assert.Equal(expected, residual);
    }

    // The DC-only output must be uniform (DC+4)>>3 and identical to running the general path (there is
    // a single code path; this asserts the fast-path invariant holds for the actual implementation).
    [Fact]
    public void InverseDct_DcOnly_EqualsUniformRoundedDc()
    {
        var coeffs = new short[16];
        coeffs[0] = 100;
        var residual = new int[16];
        Vp8Transform.InverseDct(coeffs, residual);

        var expected = (100 + 4) >> 3; // 13
        Assert.All(residual, v => Assert.Equal(expected, v));
    }

    // Known-answer for the inverse Walsh-Hadamard transform: pins the +3 rounding bias, the final >>3,
    // and the strided butterfly ordering. Expected values hand-computed from the exact integer formula.
    [Fact]
    public void InverseWht_KnownAnswer_PinsRoundingAndScatter()
    {
        var input = new short[16];
        input[0] = 160;
        input[1] = -32;
        input[4] = 24;
        input[8] = 8;

        var output = new short[16];
        Vp8Transform.InverseWht(input, output);

        var expected = new short[]
        {
            20, 20, 28, 28,
            18, 18, 26, 26,
            12, 12, 20, 20,
            14, 14, 22, 22,
        };
        Assert.Equal(expected, output);
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

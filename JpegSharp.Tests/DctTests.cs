using JpegSharp.Transforms;
using Xunit;

namespace JpegSharp.Tests;

public class DctTests
{
    [Fact]
    public void Forward_ConstantBlock_ProducesOnlyDc()
    {
        Span<double> input = stackalloc double[64];
        input.Fill(50.0);

        Span<double> output = stackalloc double[64];
        Dct.Forward(input, output);

        // Orthonormal DCT-II: DC = 8 * sampleValue for a flat block.
        Assert.Equal(8.0 * 50.0, output[0], 6);
        for (var i = 1; i < 64; i++)
            Assert.Equal(0.0, output[i], 6);
    }

    [Fact]
    public void Inverse_OfConstantDc_ProducesFlatBlock()
    {
        Span<double> coeffs = stackalloc double[64];
        coeffs[0] = 8.0 * 30.0;

        Span<double> output = stackalloc double[64];
        Dct.Inverse(coeffs, output);

        for (var i = 0; i < 64; i++)
            Assert.Equal(30.0, output[i], 6);
    }

    [Fact]
    public void ForwardThenInverse_RoundTrips()
    {
        Span<double> input = stackalloc double[64];
        for (var i = 0; i < 64; i++)
            input[i] = ((i * 37) % 256) - 128;

        Span<double> freq = stackalloc double[64];
        Span<double> restored = stackalloc double[64];
        Dct.Forward(input, freq);
        Dct.Inverse(freq, restored);

        for (var i = 0; i < 64; i++)
            Assert.Equal(input[i], restored[i], 6);
    }

    [Fact]
    public void Transform_IsEnergyPreserving()
    {
        Span<double> input = stackalloc double[64];
        var rng = new Random(12345);
        double energyIn = 0;
        for (var i = 0; i < 64; i++)
        {
            input[i] = rng.Next(-128, 128);
            energyIn += input[i] * input[i];
        }

        Span<double> freq = stackalloc double[64];
        Dct.Forward(input, freq);

        double energyOut = 0;
        for (var i = 0; i < 64; i++)
            energyOut += freq[i] * freq[i];

        // Parseval's theorem holds for the orthonormal transform.
        Assert.Equal(energyIn, energyOut, 4);
    }

    [Fact]
    public void Forward_IsLinear()
    {
        Span<double> a = stackalloc double[64];
        Span<double> b = stackalloc double[64];
        var rng = new Random(7);
        for (var i = 0; i < 64; i++)
        {
            a[i] = rng.Next(-100, 100);
            b[i] = rng.Next(-100, 100);
        }

        Span<double> sum = stackalloc double[64];
        for (var i = 0; i < 64; i++)
            sum[i] = a[i] + b[i];

        Span<double> fa = stackalloc double[64];
        Span<double> fb = stackalloc double[64];
        Span<double> fsum = stackalloc double[64];
        Dct.Forward(a, fa);
        Dct.Forward(b, fb);
        Dct.Forward(sum, fsum);

        for (var i = 0; i < 64; i++)
            Assert.Equal(fa[i] + fb[i], fsum[i], 6);
    }

    [Fact]
    public void Forward_SingleVerticalRamp_MatchesReference()
    {
        // A horizontal gradient concentrates energy in the first AC column (u=0, v=1).
        Span<double> input = stackalloc double[64];
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
                input[y * 8 + x] = x; // varies along columns only

        Span<double> freq = stackalloc double[64];
        Dct.Forward(input, freq);

        // Energy should be confined to the top row of the frequency block (v index),
        // meaning coefficients with u>0 are ~0.
        for (var u = 1; u < 8; u++)
            for (var v = 0; v < 8; v++)
                Assert.Equal(0.0, freq[u * 8 + v], 6);

        Assert.True(Math.Abs(freq[1]) > 1.0); // (u=0, v=1) is significantly non-zero
    }

    [Theory]
    [InlineData(65)]
    [InlineData(63)]
    public void Forward_WrongBlockSize_Throws(int size)
    {
        var input = new double[size];
        var output = new double[64];
        Assert.Throws<ArgumentException>(() => Dct.Forward(input, output));
    }
}

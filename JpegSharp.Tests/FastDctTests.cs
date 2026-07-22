using JpegSharp.Transforms;
using Xunit;

namespace JpegSharp.Tests;

public class FastDctTests
{
    [Fact]
    public void FastForward_MatchesReference()
    {
        var rng = new Random(1);
        Span<double> input = stackalloc double[64];
        Span<double> fast = stackalloc double[64];
        Span<double> reference = stackalloc double[64];

        for (var trial = 0; trial < 50; trial++)
        {
            for (var i = 0; i < 64; i++)
                input[i] = rng.Next(-128, 128);

            FastDct.Forward(input, fast);
            Dct.Forward(input, reference);

            for (var i = 0; i < 64; i++)
                Assert.Equal(reference[i], fast[i], 6);
        }
    }

    [Fact]
    public void FastInverse_MatchesReference()
    {
        var rng = new Random(2);
        Span<double> input = stackalloc double[64];
        Span<double> fast = stackalloc double[64];
        Span<double> reference = stackalloc double[64];

        for (var trial = 0; trial < 50; trial++)
        {
            for (var i = 0; i < 64; i++)
                input[i] = rng.Next(-500, 500);

            FastDct.Inverse(input, fast);
            Dct.Inverse(input, reference);

            for (var i = 0; i < 64; i++)
                Assert.Equal(reference[i], fast[i], 6);
        }
    }

    [Fact]
    public void FastForwardInverse_RoundTrips()
    {
        var rng = new Random(3);
        Span<double> input = stackalloc double[64];
        Span<double> freq = stackalloc double[64];
        Span<double> restored = stackalloc double[64];

        for (var i = 0; i < 64; i++)
            input[i] = rng.Next(-128, 128);

        FastDct.Forward(input, freq);
        FastDct.Inverse(freq, restored);

        for (var i = 0; i < 64; i++)
            Assert.Equal(input[i], restored[i], 6);
    }

    [Fact]
    public void FastForward_FlatBlock_ProducesOnlyDc()
    {
        Span<double> input = stackalloc double[64];
        input.Fill(40.0);
        Span<double> output = stackalloc double[64];
        FastDct.Forward(input, output);

        Assert.Equal(8.0 * 40.0, output[0], 6);
        for (var i = 1; i < 64; i++)
            Assert.Equal(0.0, output[i], 6);
    }
}

using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8BooleanCoderTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(12345)]
    public void RoundTrip_RandomBitsWithVaryingProbabilities(int seed)
    {
        var rng = new Random(seed);
        var probs = new int[3000];
        var bits = new int[3000];
        for (var i = 0; i < bits.Length; i++)
        {
            probs[i] = rng.Next(1, 256); // probability 1..255
            bits[i] = rng.Next(2);
        }

        var encoder = new Vp8BooleanEncoder();
        for (var i = 0; i < bits.Length; i++)
            encoder.PutBit(probs[i], bits[i]);
        var bytes = encoder.Finish();

        var decoder = new Vp8BooleanDecoder(bytes);
        for (var i = 0; i < bits.Length; i++)
            Assert.Equal(bits[i], decoder.GetBit(probs[i]));
    }

    [Fact]
    public void RoundTrip_AllZeros()
    {
        var encoder = new Vp8BooleanEncoder();
        for (var i = 0; i < 500; i++)
            encoder.PutBit(200, 0);
        var decoder = new Vp8BooleanDecoder(encoder.Finish());
        for (var i = 0; i < 500; i++)
            Assert.Equal(0, decoder.GetBit(200));
    }

    [Fact]
    public void RoundTrip_AllOnes()
    {
        var encoder = new Vp8BooleanEncoder();
        for (var i = 0; i < 500; i++)
            encoder.PutBit(50, 1);
        var decoder = new Vp8BooleanDecoder(encoder.Finish());
        for (var i = 0; i < 500; i++)
            Assert.Equal(1, decoder.GetBit(50));
    }

    [Fact]
    public void RoundTrip_UniformBits()
    {
        var rng = new Random(9);
        var bits = new int[1000];
        var encoder = new Vp8BooleanEncoder();
        for (var i = 0; i < bits.Length; i++)
        {
            bits[i] = rng.Next(2);
            encoder.PutBitUniform(bits[i]);
        }
        var decoder = new Vp8BooleanDecoder(encoder.Finish());
        for (var i = 0; i < bits.Length; i++)
            Assert.Equal(bits[i], decoder.GetBitUniform());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(55)]
    public void RoundTrip_Literals(int seed)
    {
        var rng = new Random(seed);
        var widths = new int[500];
        var values = new uint[500];
        var encoder = new Vp8BooleanEncoder();
        for (var i = 0; i < values.Length; i++)
        {
            var w = rng.Next(1, 25);
            widths[i] = w;
            values[i] = (uint)rng.NextInt64(0, 1L << w);
            encoder.PutLiteral(values[i], w);
        }
        var decoder = new Vp8BooleanDecoder(encoder.Finish());
        for (var i = 0; i < values.Length; i++)
            Assert.Equal(values[i], decoder.GetLiteral(widths[i]));
    }

    [Fact]
    public void RoundTrip_SignedLiterals()
    {
        var rng = new Random(3);
        var widths = new int[300];
        var values = new int[300];
        var encoder = new Vp8BooleanEncoder();
        for (var i = 0; i < values.Length; i++)
        {
            var w = rng.Next(1, 12);
            widths[i] = w;
            values[i] = rng.Next(-(1 << w) + 1, (1 << w));
            encoder.PutSigned(values[i], w);
        }
        var decoder = new Vp8BooleanDecoder(encoder.Finish());
        for (var i = 0; i < values.Length; i++)
            Assert.Equal(values[i], decoder.GetSigned(widths[i]));
    }

    [Fact]
    public void SingleBit_RoundTrips()
    {
        var encoder = new Vp8BooleanEncoder();
        encoder.PutBit(128, 1);
        var decoder = new Vp8BooleanDecoder(encoder.Finish());
        Assert.Equal(1, decoder.GetBit(128));
    }
}

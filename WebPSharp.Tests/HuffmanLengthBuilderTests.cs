using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class HuffmanLengthBuilderTests
{
    private static void AssertValidCode(int[] lengths, int[] freqs)
    {
        // Must not throw: the lengths form a valid canonical prefix code.
        PrefixCode.BuildCanonicalCodes(lengths, out _);

        for (var s = 0; s < lengths.Length; s++)
        {
            Assert.InRange(lengths[s], 0, PrefixCode.MaxCodeLength);
            if (freqs[s] == 0)
                Assert.Equal(0, lengths[s]);
            else
                Assert.True(lengths[s] > 0, $"Used symbol {s} must have a positive length.");
        }
    }

    [Fact]
    public void SingleSymbol_GetsLengthOne()
    {
        var freqs = new int[10];
        freqs[3] = 42;
        var lengths = HuffmanLengthBuilder.Build(freqs, PrefixCode.MaxCodeLength);
        Assert.Equal(1, lengths[3]);
        AssertValidCode(lengths, freqs);
    }

    [Fact]
    public void TwoSymbols_EachLengthOne()
    {
        var freqs = new[] { 5, 9, 0, 0 };
        var lengths = HuffmanLengthBuilder.Build(freqs, PrefixCode.MaxCodeLength);
        Assert.Equal(1, lengths[0]);
        Assert.Equal(1, lengths[1]);
        AssertValidCode(lengths, freqs);
    }

    [Fact]
    public void UniformFrequencies_ProduceBalancedCode()
    {
        var freqs = new int[256];
        Array.Fill(freqs, 1);
        var lengths = HuffmanLengthBuilder.Build(freqs, PrefixCode.MaxCodeLength);
        foreach (var len in lengths)
            Assert.Equal(8, len);
        AssertValidCode(lengths, freqs);
    }

    [Fact]
    public void SkewedDistribution_RespectsMaxLength()
    {
        // Fibonacci-like frequencies drive natural Huffman depth well beyond 15.
        var freqs = new int[40];
        long a = 1, b = 1;
        for (var i = 0; i < freqs.Length; i++)
        {
            freqs[i] = (int)(a % 1_000_000);
            (a, b) = (b, a + b);
        }
        var lengths = HuffmanLengthBuilder.Build(freqs, PrefixCode.MaxCodeLength);
        foreach (var len in lengths)
            Assert.InRange(len, 0, PrefixCode.MaxCodeLength);
        AssertValidCode(lengths, freqs);
    }

    [Fact]
    public void MoreFrequentSymbol_GetsShorterOrEqualCode()
    {
        var freqs = new[] { 1000, 500, 250, 125, 60, 30, 10, 5 };
        var lengths = HuffmanLengthBuilder.Build(freqs, PrefixCode.MaxCodeLength);
        for (var i = 1; i < freqs.Length; i++)
            Assert.True(lengths[i] >= lengths[i - 1],
                $"Less frequent symbol {i} should not have a shorter code than {i - 1}.");
        AssertValidCode(lengths, freqs);
    }

    [Theory]
    [InlineData(1, 64)]
    [InlineData(7, 256)]
    [InlineData(99, 280)]
    public void RandomFrequencies_RoundTripThroughCode(int seed, int alphabet)
    {
        var rng = new Random(seed);
        var freqs = new int[alphabet];
        var stream = new List<int>();
        for (var i = 0; i < 3000; i++)
        {
            var sym = rng.Next(alphabet);
            // Leave some symbols unused to exercise zero-frequency handling.
            if (sym % 5 == 0) sym = rng.Next(alphabet / 2);
            freqs[sym]++;
            stream.Add(sym);
        }

        var lengths = HuffmanLengthBuilder.Build(freqs, PrefixCode.MaxCodeLength);
        AssertValidCode(lengths, freqs);

        var codeWriter = new PrefixCodeWriter(lengths);
        var writer = new Vp8LBitWriter();
        foreach (var sym in stream)
            codeWriter.WriteSymbol(writer, sym);

        var tree = HuffmanTree.FromCodeLengths(lengths);
        var reader = new Vp8LBitReader(writer.ToArray());
        foreach (var expected in stream)
            Assert.Equal(expected, tree.ReadSymbol(ref reader));
    }
}

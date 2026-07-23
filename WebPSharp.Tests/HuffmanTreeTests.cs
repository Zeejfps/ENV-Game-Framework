using WebPSharp.Api.Exceptions;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class HuffmanTreeTests
{
    [Fact]
    public void Decode_HandCraftedStream_MatchesCanonicalCodes()
    {
        // Lengths [1,2,2] -> canonical MSB-first codes: sym0="0", sym1="10", sym2="11".
        // Stream decoding [sym1, sym0, sym2] packs LSB-first to the single byte 0x19.
        var tree = HuffmanTree.FromCodeLengths(new[] { 1, 2, 2 });
        var reader = new Vp8LBitReader(new byte[] { 0x19 });
        Assert.Equal(1, tree.ReadSymbol(ref reader));
        Assert.Equal(0, tree.ReadSymbol(ref reader));
        Assert.Equal(2, tree.ReadSymbol(ref reader));
    }

    [Fact]
    public void SingleSymbol_ReadsZeroBits()
    {
        var tree = HuffmanTree.FromSingleSymbol(5, alphabetSize: 280);
        var reader = new Vp8LBitReader(Array.Empty<byte>());
        Assert.Equal(5, tree.ReadSymbol(ref reader));
        Assert.Equal(5, tree.ReadSymbol(ref reader));
        Assert.False(reader.IsEndOfStream); // no bits consumed
    }

    [Fact]
    public void SingleUsedLength_IsTreatedAsZeroBitCode()
    {
        // Only one symbol carries a length; it should decode without consuming bits.
        var lengths = new int[10];
        lengths[7] = 1;
        var tree = HuffmanTree.FromCodeLengths(lengths);
        var reader = new Vp8LBitReader(Array.Empty<byte>());
        Assert.Equal(7, tree.ReadSymbol(ref reader));
    }

    [Fact]
    public void OverSubscribedCode_Throws()
    {
        // Three symbols of length 1 cannot fit (only two length-1 codes exist).
        Assert.Throws<WebPCorruptException>(() => HuffmanTree.FromCodeLengths(new[] { 1, 1, 1 }));
    }

    [Fact]
    public void IncompleteCode_WithMultipleSymbols_Throws()
    {
        // Two symbols each of length 2 leave code space unfilled -> invalid.
        Assert.Throws<WebPCorruptException>(() => HuffmanTree.FromCodeLengths(new[] { 2, 2, 0, 0 }));
    }

    [Fact]
    public void AllZeroLengths_Throws()
    {
        Assert.Throws<WebPCorruptException>(() => HuffmanTree.FromCodeLengths(new int[8]));
    }

    [Fact]
    public void Decode_RunningOffTree_SetsCorruption()
    {
        // Complete tree for [1,1]: sym0="0", sym1="1". Any bit decodes; ensure both reachable.
        var tree = HuffmanTree.FromCodeLengths(new[] { 1, 1 });
        var reader = new Vp8LBitReader(new byte[] { 0b10 });
        Assert.Equal(0, tree.ReadSymbol(ref reader));
        Assert.Equal(1, tree.ReadSymbol(ref reader));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2025)]
    public void RoundTrip_EncodeThenDecode(int seed)
    {
        var rng = new Random(seed);
        const int alphabet = 64;

        // Build a valid canonical length assignment by drawing a random full binary tree shape:
        // start from a complete code of a fixed depth and let symbols share it.
        var lengths = BuildRandomCompleteLengths(rng, alphabet);
        var encoder = new PrefixCodeWriter(lengths);
        var tree = HuffmanTree.FromCodeLengths(lengths);

        var symbols = new List<int>();
        var usable = new List<int>();
        for (var s = 0; s < alphabet; s++)
            if (lengths[s] > 0)
                usable.Add(s);

        var writer = new Vp8LBitWriter();
        for (var i = 0; i < 1000; i++)
        {
            var sym = usable[rng.Next(usable.Count)];
            symbols.Add(sym);
            encoder.WriteSymbol(writer, sym);
        }

        var reader = new Vp8LBitReader(writer.ToArray());
        foreach (var expected in symbols)
            Assert.Equal(expected, tree.ReadSymbol(ref reader));
    }

    private static int[] BuildRandomCompleteLengths(Random rng, int alphabet)
    {
        // Kraft-complete construction: repeatedly split the deepest available slot.
        // Begin with one node of length 0, then split random leaves into two length+1 leaves.
        var lengths = new List<int> { 0 };
        var targetSymbols = rng.Next(2, alphabet + 1);
        while (lengths.Count < targetSymbols)
        {
            var idx = rng.Next(lengths.Count);
            var len = lengths[idx];
            if (len >= 14) continue; // respect VP8L max code length
            lengths[idx] = len + 1;
            lengths.Add(len + 1);
        }

        var result = new int[alphabet];
        for (var i = 0; i < lengths.Count; i++)
            result[i] = lengths[i];
        return result;
    }
}

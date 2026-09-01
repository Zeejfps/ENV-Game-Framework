using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LHuffmanTests
{
    [Theory]
    [InlineData(1, 19)]
    [InlineData(7, 40)]
    [InlineData(99, 256)]
    [InlineData(2025, 280)]
    public void PrefixCode_Description_RoundTrips(int seed, int alphabet)
    {
        var rng = new Random(seed);
        var lengths = BuildRandomCompleteLengths(rng, alphabet);

        // Encode the prefix-code description, then a run of symbols.
        var writer = new Vp8LBitWriter();
        Vp8LHuffman.WritePrefixCode(writer, lengths);

        var codeWriter = new PrefixCodeWriter(lengths);
        var usable = new List<int>();
        for (var s = 0; s < alphabet; s++)
            if (lengths[s] > 0)
                usable.Add(s);

        var symbols = new List<int>();
        for (var i = 0; i < 400; i++)
        {
            var sym = usable[rng.Next(usable.Count)];
            symbols.Add(sym);
            codeWriter.WriteSymbol(writer, sym);
        }

        // Decode the description, then the symbols.
        var reader = new Vp8LBitReader(writer.ToArray());
        var tree = Vp8LHuffman.ReadPrefixCode(ref reader, alphabet);
        foreach (var expected in symbols)
            Assert.Equal(expected, tree.ReadSymbol(ref reader));
    }

    [Fact]
    public void SimpleCode_TwoSymbols_DecodesByCanonicalOrder()
    {
        // Hand-emit a "simple code" header with two 8-bit symbols (10 and 20).
        var writer = new Vp8LBitWriter();
        writer.PutBit(1);          // simple code
        writer.PutBit(1);          // num_symbols - 1 == 1 -> 2 symbols
        writer.PutBit(1);          // first symbol uses 8 bits
        writer.PutBits(10, 8);     // symbol 0
        writer.PutBits(20, 8);     // symbol 1
        // Two symbols, each 1 bit; canonical assigns the lower symbol index code "0".
        writer.PutBit(0);          // -> symbol 10
        writer.PutBit(1);          // -> symbol 20

        var reader = new Vp8LBitReader(writer.ToArray());
        var tree = Vp8LHuffman.ReadPrefixCode(ref reader, 256);
        Assert.Equal(10, tree.ReadSymbol(ref reader));
        Assert.Equal(20, tree.ReadSymbol(ref reader));
    }

    [Fact]
    public void SimpleCode_SingleSymbol_ReadsZeroBits()
    {
        var writer = new Vp8LBitWriter();
        writer.PutBit(1);          // simple code
        writer.PutBit(0);          // num_symbols - 1 == 0 -> 1 symbol
        writer.PutBit(1);          // 8-bit symbol
        writer.PutBits(200, 8);    // the only symbol

        var reader = new Vp8LBitReader(writer.ToArray());
        var tree = Vp8LHuffman.ReadPrefixCode(ref reader, 256);
        Assert.Equal(200, tree.ReadSymbol(ref reader));
        Assert.Equal(200, tree.ReadSymbol(ref reader));
    }

    [Fact]
    public void SimpleCode_SingleSymbol_OneBitFlavor()
    {
        var writer = new Vp8LBitWriter();
        writer.PutBit(1);          // simple code
        writer.PutBit(0);          // 1 symbol
        writer.PutBit(0);          // first symbol uses 1 bit
        writer.PutBit(1);          // symbol == 1

        var reader = new Vp8LBitReader(writer.ToArray());
        var tree = Vp8LHuffman.ReadPrefixCode(ref reader, 256);
        Assert.Equal(1, tree.ReadSymbol(ref reader));
    }

    private static int[] BuildRandomCompleteLengths(Random rng, int alphabet)
    {
        var lengths = new List<int> { 0 };
        var targetSymbols = rng.Next(2, alphabet + 1);
        while (lengths.Count < targetSymbols)
        {
            var idx = rng.Next(lengths.Count);
            var len = lengths[idx];
            if (len >= 15) continue;
            lengths[idx] = len + 1;
            lengths.Add(len + 1);
        }

        var result = new int[alphabet];
        // Shuffle which symbols get which lengths so codes aren't trivially ordered.
        var perm = new int[alphabet];
        for (var i = 0; i < alphabet; i++) perm[i] = i;
        for (var i = alphabet - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (perm[i], perm[j]) = (perm[j], perm[i]);
        }
        for (var i = 0; i < lengths.Count; i++)
            result[perm[i]] = lengths[i];
        return result;
    }
}

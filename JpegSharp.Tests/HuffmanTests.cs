using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using JpegSharp.Huffman;
using Xunit;

namespace JpegSharp.Tests;

public class HuffmanTests
{
    [Fact]
    public void CanonicalCodes_MatchSpecForDcLuminance()
    {
        var table = StandardHuffmanTables.DcLuminance;

        table.GetCode(0, out var code0, out var size0);
        Assert.Equal((0, 2), (code0, size0));

        table.GetCode(1, out var code1, out var size1);
        Assert.Equal((0b010, 3), (code1, size1));

        table.GetCode(6, out var code6, out var size6);
        Assert.Equal((0b1110, 4), (code6, size6));

        table.GetCode(11, out var code11, out var size11);
        Assert.Equal((0b111111110, 9), (code11, size11));
    }

    [Fact]
    public void DecodeSymbol_ReadsCanonicalCode()
    {
        var table = StandardHuffmanTables.DcLuminance;

        // Symbol 0 => "00"
        var r0 = new BitReader(new byte[] { 0b0000_0000 });
        Assert.Equal(0, table.DecodeSymbol(ref r0));

        // Symbol 6 => "1110"
        var r6 = new BitReader(new byte[] { 0b1110_0000 });
        Assert.Equal(6, table.DecodeSymbol(ref r6));
    }

    [Fact]
    public void EncodeThenDecode_RoundTripsEverySymbol_DcLuminance()
        => AssertRoundTrip(StandardHuffmanTables.DcLuminance);

    [Fact]
    public void EncodeThenDecode_RoundTripsEverySymbol_DcChrominance()
        => AssertRoundTrip(StandardHuffmanTables.DcChrominance);

    [Fact]
    public void EncodeThenDecode_RoundTripsEverySymbol_AcLuminance()
        => AssertRoundTrip(StandardHuffmanTables.AcLuminance);

    [Fact]
    public void EncodeThenDecode_RoundTripsEverySymbol_AcChrominance()
        => AssertRoundTrip(StandardHuffmanTables.AcChrominance);

    [Fact]
    public void GeneratedCodes_ArePrefixFree()
    {
        var table = StandardHuffmanTables.AcLuminance;
        var codes = new List<(int code, int size)>();
        foreach (var symbol in table.Symbols)
        {
            table.GetCode(symbol, out var code, out var size);
            codes.Add((code, size));
        }

        for (var i = 0; i < codes.Count; i++)
            for (var j = 0; j < codes.Count; j++)
            {
                if (i == j)
                    continue;
                var (ci, si) = codes[i];
                var (cj, sj) = codes[j];
                if (si <= sj)
                {
                    // ci must not be a prefix of cj
                    Assert.NotEqual(ci, cj >> (sj - si));
                }
            }
    }

    [Fact]
    public void Constructor_MismatchedCounts_Throws()
    {
        var counts = new byte[16];
        counts[0] = 2; // declares 2 codes but only 1 symbol given
        Assert.Throws<ArgumentException>(() => new HuffmanTable(counts, new byte[] { 5 }));
    }

    [Fact]
    public void Constructor_WrongCountsLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => new HuffmanTable(new byte[15], new byte[] { 1 }));
    }

    [Fact]
    public void Constructor_OversubscribedTable_Throws()
    {
        // Three codes of length 1 is impossible (max 2).
        var counts = new byte[16];
        counts[0] = 3;
        Assert.Throws<ArgumentException>(() => new HuffmanTable(counts, new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void DecodeSymbol_InvalidCode_Throws()
    {
        // A table with a single 1-bit code "0" for symbol 42.
        var counts = new byte[16];
        counts[0] = 1;
        var table = new HuffmanTable(counts, new byte[] { 42 });

        Assert.Throws<JpegCorruptException>(() => DecodeFirst(table, [0xFF])); // code "1..." never matches
    }

    private static int DecodeFirst(HuffmanTable table, byte[] data)
    {
        var reader = new BitReader(data);
        return table.DecodeSymbol(ref reader);
    }

    [Fact]
    public void CountsAndSymbols_AreExposedForDhtWriting()
    {
        var table = StandardHuffmanTables.DcLuminance;
        Assert.Equal(16, table.Counts.Length);
        var total = 0;
        foreach (var c in table.Counts)
            total += c;
        Assert.Equal(table.Symbols.Length, total);
        Assert.Equal(12, table.Symbols.Length);
    }

    private static void AssertRoundTrip(HuffmanTable table)
    {
        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        foreach (var symbol in table.Symbols)
            table.Encode(writer, symbol);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        foreach (var expected in table.Symbols)
            Assert.Equal(expected, table.DecodeSymbol(ref reader));
    }
}

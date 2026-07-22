using JpegSharp.Bitstream;
using JpegSharp.Huffman;
using Xunit;

namespace JpegSharp.Tests;

public class HuffmanLookaheadTests
{
    [Fact]
    public void PeekByte_DoesNotConsume()
    {
        var reader = new BitReader([0b1011_0010, 0b1100_0000]);
        Assert.Equal(0b1011_0010, reader.PeekByte());
        Assert.Equal(0b1011_0010, reader.PeekByte()); // same value again -> not consumed
        Assert.Equal(0b101, reader.ReadBits(3));       // still reads from the start
    }

    [Fact]
    public void SkipBits_AdvancesByExactCount()
    {
        var reader = new BitReader([0b1011_0010, 0b1100_0000]);
        reader.PeekByte();
        reader.SkipBits(3);
        Assert.Equal(0b10010, reader.ReadBits(5));
    }

    [Fact]
    public void PeekByte_PadsWithOnesAtEnd()
    {
        var reader = new BitReader([0b1010_0000]);
        reader.ReadBits(4); // consume high nibble
        // Only 4 real bits remain; PeekByte pads the rest with 1s.
        Assert.Equal(0b0000_1111, reader.PeekByte());
    }

    [Fact]
    public void DecodeSymbol_WithLookahead_MatchesEverySymbol()
    {
        foreach (var table in new[]
                 {
                     StandardHuffmanTables.DcLuminance,
                     StandardHuffmanTables.AcLuminance,
                     StandardHuffmanTables.DcChrominance,
                     StandardHuffmanTables.AcChrominance,
                 })
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

    [Fact]
    public void DecodeSymbol_HandlesCodesLongerThanLookahead()
    {
        // The DC luminance symbol 11 has a 9-bit code, exceeding the 8-bit lookahead window,
        // so it exercises the slow decode path.
        var table = StandardHuffmanTables.DcLuminance;
        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        table.Encode(writer, 11);
        table.Encode(writer, 0);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        Assert.Equal(11, table.DecodeSymbol(ref reader));
        Assert.Equal(0, table.DecodeSymbol(ref reader));
    }
}

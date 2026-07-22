using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Huffman;
using Xunit;

namespace JpegSharp.Tests;

public class BlockCoderTests
{
    private static readonly HuffmanTable Dc = StandardHuffmanTables.DcLuminance;
    private static readonly HuffmanTable Ac = StandardHuffmanTables.AcLuminance;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(-1, 1)]
    [InlineData(2, 2)]
    [InlineData(-2, 2)]
    [InlineData(-3, 2)]
    [InlineData(7, 3)]
    [InlineData(255, 8)]
    [InlineData(-255, 8)]
    [InlineData(2047, 11)]
    public void MagnitudeCategory_MatchesSpec(int value, int expected)
        => Assert.Equal(expected, BlockScanCoder.MagnitudeCategory(value));

    [Fact]
    public void SingleBlock_RoundTrips()
    {
        short[] block = new short[64];
        block[0] = 120; // DC
        block[1] = -5;
        block[2] = 3;
        block[5] = 1;
        block[40] = -2;

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        var dcOut = BlockScanCoder.EncodeBlock(writer, block, 0, Dc, Ac);
        writer.Flush();
        Assert.Equal(120, dcOut);

        var decoded = new short[64];
        var reader = new BitReader(ms.ToArray());
        var dcIn = BlockScanCoder.DecodeBlock(ref reader, decoded, 0, Dc, Ac);

        Assert.Equal(120, dcIn);
        Assert.Equal(block, decoded);
    }

    [Fact]
    public void DcPrediction_ChainsAcrossBlocks()
    {
        short[][] blocks =
        [
            NewBlock(100), NewBlock(105), NewBlock(90), NewBlock(90),
        ];

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        var predictor = 0;
        foreach (var block in blocks)
            predictor = BlockScanCoder.EncodeBlock(writer, block, predictor, Dc, Ac);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        var decoded = new short[64];
        predictor = 0;
        foreach (var block in blocks)
        {
            predictor = BlockScanCoder.DecodeBlock(ref reader, decoded, predictor, Dc, Ac);
            Assert.Equal(block[0], decoded[0]);
        }
    }

    [Fact]
    public void DcOnlyBlock_EmitsEob()
    {
        var block = NewBlock(50); // DC only, all AC zero

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        BlockScanCoder.EncodeBlock(writer, block, 0, Dc, Ac);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        var decoded = new short[64];
        BlockScanCoder.DecodeBlock(ref reader, decoded, 0, Dc, Ac);
        Assert.Equal(block, decoded);
    }

    [Fact]
    public void LongZeroRun_UsesZrlAndRoundTrips()
    {
        var block = new short[64];
        block[0] = 10;
        block[21] = 5; // 20 leading AC zeros forces a ZRL (16) + run(4)

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        BlockScanCoder.EncodeBlock(writer, block, 0, Dc, Ac);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        var decoded = new short[64];
        BlockScanCoder.DecodeBlock(ref reader, decoded, 0, Dc, Ac);
        Assert.Equal(block, decoded);
    }

    [Fact]
    public void FullNonZeroBlock_RoundTrips()
    {
        var block = new short[64];
        var rng = new Random(3);
        for (var i = 0; i < 64; i++)
            block[i] = (short)rng.Next(-50, 51);

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        BlockScanCoder.EncodeBlock(writer, block, 0, Dc, Ac);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        var decoded = new short[64];
        BlockScanCoder.DecodeBlock(ref reader, decoded, 0, Dc, Ac);
        Assert.Equal(block, decoded);
    }

    [Fact]
    public void ManyRandomBlocks_RoundTripWithChainedPrediction()
    {
        var rng = new Random(2025);
        var blocks = new short[200][];
        for (var b = 0; b < blocks.Length; b++)
        {
            var block = new short[64];
            block[0] = (short)rng.Next(-512, 512);
            for (var k = 1; k < 64; k++)
                if (rng.NextDouble() < 0.25)
                    block[k] = (short)rng.Next(-30, 31);
            blocks[b] = block;
        }

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        var predictor = 0;
        foreach (var block in blocks)
            predictor = BlockScanCoder.EncodeBlock(writer, block, predictor, Dc, Ac);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        var decoded = new short[64];
        predictor = 0;
        foreach (var block in blocks)
        {
            predictor = BlockScanCoder.DecodeBlock(ref reader, decoded, predictor, Dc, Ac);
            Assert.Equal(block, decoded);
        }
    }

    [Fact]
    public void GatheredFrequencies_ProduceWorkingOptimizedTables()
    {
        var rng = new Random(77);
        var blocks = new short[100][];
        for (var b = 0; b < blocks.Length; b++)
        {
            var block = new short[64];
            block[0] = (short)rng.Next(-200, 200);
            for (var k = 1; k < 64; k++)
                if (rng.NextDouble() < 0.2)
                    block[k] = (short)rng.Next(-20, 20);
            blocks[b] = block;
        }

        var dcFreq = new int[256];
        var acFreq = new int[256];
        var predictor = 0;
        foreach (var block in blocks)
            predictor = BlockScanCoder.GatherBlockFrequencies(block, predictor, dcFreq, acFreq);

        var dcTable = HuffmanTable.BuildOptimized(dcFreq);
        var acTable = HuffmanTable.BuildOptimized(acFreq);

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        predictor = 0;
        foreach (var block in blocks)
            predictor = BlockScanCoder.EncodeBlock(writer, block, predictor, dcTable, acTable);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        var decoded = new short[64];
        predictor = 0;
        foreach (var block in blocks)
        {
            predictor = BlockScanCoder.DecodeBlock(ref reader, decoded, predictor, dcTable, acTable);
            Assert.Equal(block, decoded);
        }
    }

    [Fact]
    public void DecodeBlock_BadRunPastBlockEnd_Throws()
    {
        // DC category 0 (diff 0), then repeated ZRL (0xF0): 4 * 16 = 64 zeros overruns index 63.
        using var bad = new MemoryStream();
        var w = new BitWriter(bad);
        Dc.Encode(w, 0);
        for (var i = 0; i < 4; i++)
            Ac.Encode(w, 0xF0);
        w.Flush();

        Assert.Throws<JpegCorruptException>(() => DecodeBad(bad.ToArray()));
    }

    private static void DecodeBad(byte[] data)
    {
        var reader = new BitReader(data);
        var block = new short[64];
        BlockScanCoder.DecodeBlock(ref reader, block, 0, Dc, Ac);
    }

    private static short[] NewBlock(short dc)
    {
        var block = new short[64];
        block[0] = dc;
        return block;
    }
}

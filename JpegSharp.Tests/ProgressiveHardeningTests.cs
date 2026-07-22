using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using JpegSharp.Markers;
using Xunit;

namespace JpegSharp.Tests;

// Hardening for progressive entropy decoding against malformed streams (FOLLOWUP-PROG,
// ITU-T T.81 G.1.2). The decoder is private, so each malformed case is expressed as a
// hand-built progressive JPEG: a real SOF2 frame + DQT + custom single-code Huffman table(s)
// + SOS whose entropy bits are written directly, forcing the exact corrupt symbol under test.
public class ProgressiveHardeningTests
{
    [Fact]
    public void Progressive_DcCategory16_Rejected()
    {
        // DC-first scan whose only DC Huffman code decodes to magnitude category 16 (T.81 caps
        // the DC difference category at 15; category 16 would overflow the short coefficient).
        var rejected = BuildProgressive(
            8, 8,
            [(0, 0, OneCodeCounts(), [16])],
            Sos(ns: 1, compId: 1, tableSel: 0x00, ss: 0, se: 0, ahAl: 0x00),
            bw => bw.WriteBits(0, 1)); // code "0" -> symbol 16

        Assert.Throws<JpegCorruptException>(() => Jpeg.Decode(rejected));

        // Category 15 (the spec maximum) must still be accepted and decode cleanly.
        var accepted = BuildProgressive(
            8, 8,
            [(0, 0, OneCodeCounts(), [15])],
            Sos(ns: 1, compId: 1, tableSel: 0x00, ss: 0, se: 0, ahAl: 0x00),
            bw =>
            {
                bw.WriteBits(0, 1);      // code "0" -> symbol 15
                bw.WriteBits(16384, 15); // 15 magnitude bits -> in-range predictor
            });

        var decoded = Jpeg.Decode(accepted);
        Assert.Equal(8, decoded.Width);
        Assert.Equal(8, decoded.Height);
    }

    [Fact]
    public void Progressive_DcPredictorOverflow_Rejected()
    {
        // Two DC blocks in one non-interleaved scan (no restart), each adding the maximum
        // category-15 positive difference. The running predictor exceeds short.MaxValue on the
        // second block, which must throw rather than silently wrap the coefficient.
        var bytes = BuildProgressive(
            16, 8, // 2 blocks wide, 1 block tall -> predictor accumulates across both
            [(0, 0, OneCodeCounts(), [15])],
            Sos(ns: 1, compId: 1, tableSel: 0x00, ss: 0, se: 0, ahAl: 0x00),
            bw =>
            {
                bw.WriteBits(0, 1);      // block 1: category 15
                bw.WriteBits(0x7FFF, 15); // +32767 -> predictor 32767 (still in range)
                bw.WriteBits(0, 1);      // block 2: category 15
                bw.WriteBits(0x7FFF, 15); // +32767 -> predictor 65534 (overflow)
            });

        Assert.Throws<JpegCorruptException>(() => Jpeg.Decode(bytes));
    }

    [Fact]
    public void Progressive_AcFirst_ZrlOverrun_Rejected()
    {
        // AC-first scan over band [1,5]. A single ZRL (16 zeros) advances the coefficient index
        // far past Se, which must be rejected instead of overrunning the coefficient buffer.
        var bytes = BuildProgressive(
            8, 8,
            [(1, 0, OneCodeCounts(), [0xF0])], // AC table: single code -> ZRL (r=15, s=0)
            Sos(ns: 1, compId: 1, tableSel: 0x00, ss: 1, se: 5, ahAl: 0x00),
            bw => bw.WriteBits(0, 1)); // code "0" -> 0xF0 -> ZRL -> k = 1 + 16 = 17 > Se+1

        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsType<JpegCorruptException>(ex);
    }

    [Fact]
    public void Progressive_AcRefine_InvalidSizeNibble_Rejected()
    {
        // AC-refine scan (Ah=1). The only valid nonzero SSSS in AC refinement is 1; a symbol with
        // SSSS=2 for a newly-nonzero coefficient is invalid and must be rejected.
        var bytes = BuildProgressive(
            8, 8,
            [(1, 0, OneCodeCounts(), [0x02])], // AC table: single code -> r=0, s=2
            Sos(ns: 1, compId: 1, tableSel: 0x00, ss: 1, se: 5, ahAl: 0x10), // Ah=1, Al=0
            bw => bw.WriteBits(0, 1)); // code "0" -> 0x02 -> s=2 -> invalid

        Assert.Throws<JpegCorruptException>(() => Jpeg.Decode(bytes));
    }

    [Fact]
    public void Progressive_ValidImage_DecodesUnchanged()
    {
        // Regression guard: full DC+AC progressive encodes (grayscale and RGB 4:2:0) must remain
        // byte-identical to the equivalent baseline decode.
        var gray = JpegImage.CreateGrayscale(40, 40, Gradient(40, 40));
        var grayBaseline = Jpeg.Decode(Jpeg.Encode(gray, new JpegEncoderOptions { Quality = 82 }));
        var grayProgressive = Jpeg.Decode(Jpeg.Encode(gray,
            new JpegEncoderOptions { Quality = 82, Progressive = true }));
        Assert.Equal(grayBaseline.PixelData, grayProgressive.PixelData);

        var rgb = JpegImage.CreateRgb(48, 40, ColorGradient(48, 40));
        var rgbBaseline = Jpeg.Decode(Jpeg.Encode(rgb,
            new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 }));
        var rgbProgressive = Jpeg.Decode(Jpeg.Encode(rgb,
            new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420, Progressive = true }));
        Assert.Equal(rgbBaseline.PixelData, rgbProgressive.PixelData);
    }

    // ----- helpers -----

    private static byte[] OneCodeCounts()
    {
        var counts = new byte[16];
        counts[0] = 1; // a single 1-bit code "0"
        return counts;
    }

    private static byte[] Sos(int ns, int compId, byte tableSel, int ss, int se, int ahAl)
    {
        return [(byte)ns, (byte)compId, tableSel, (byte)ss, (byte)se, (byte)ahAl];
    }

    // Builds a single-component (grayscale) SOF2 progressive stream with the given Huffman tables,
    // one SOS, and hand-written entropy bits.
    private static byte[] BuildProgressive(
        int width, int height,
        (int TableClass, int TableId, byte[] Counts, byte[] Symbols)[] huffTables,
        byte[] sos,
        Action<BitWriter> writeEntropy)
    {
        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteMarker(JpegMarkers.StartOfImage);

        // DQT: id 0, all-ones (a valid identity-scaling quantization table).
        Span<byte> dqt = stackalloc byte[1 + 64];
        dqt[0] = 0;
        for (var k = 0; k < 64; k++)
            dqt[1 + k] = 1;
        writer.WriteSegment(JpegMarkers.DefineQuantizationTables, dqt);

        // SOF2: one component (id 1, H=V=1, quant id 0).
        Span<byte> sof = stackalloc byte[6 + 3];
        var p = 0;
        sof[p++] = 8;
        sof[p++] = (byte)(height >> 8); sof[p++] = (byte)height;
        sof[p++] = (byte)(width >> 8); sof[p++] = (byte)width;
        sof[p++] = 1;
        sof[p++] = 1;    // component id 1
        sof[p++] = 0x11; // H=1, V=1
        sof[p++] = 0;    // quant id 0
        writer.WriteSegment(JpegMarkers.StartOfFrameProgressive, sof[..p]);

        Span<byte> dht = stackalloc byte[1 + 16 + 256];
        foreach (var (tableClass, tableId, counts, symbols) in huffTables)
        {
            dht[0] = (byte)((tableClass << 4) | tableId);
            for (var i = 0; i < 16; i++)
                dht[1 + i] = counts[i];
            for (var i = 0; i < symbols.Length; i++)
                dht[17 + i] = symbols[i];
            writer.WriteSegment(JpegMarkers.DefineHuffmanTables, dht[..(17 + symbols.Length)]);
        }

        writer.WriteSegment(JpegMarkers.StartOfScan, sos);

        var bitWriter = new BitWriter(ms);
        writeEntropy(bitWriter);
        bitWriter.Flush();

        writer.WriteMarker(JpegMarkers.EndOfImage);
        return ms.ToArray();
    }

    private static byte[] Gradient(int w, int h)
    {
        var data = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                data[y * w + x] = (byte)((x * 255 / Math.Max(1, w - 1) + y * 255 / Math.Max(1, h - 1)) / 2);
        return data;
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var data = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                data[i] = (byte)(x * 255 / Math.Max(1, w - 1));
                data[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                data[i + 2] = (byte)((x + y) * 255 / Math.Max(1, w + h - 2));
            }
        return data;
    }
}

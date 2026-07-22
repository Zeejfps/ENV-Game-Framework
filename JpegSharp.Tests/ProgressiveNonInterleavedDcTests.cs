using JpegSharp.Api;
using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Huffman;
using JpegSharp.Markers;
using JpegSharp.Quantization;
using JpegSharp.Transforms;
using Xunit;

namespace JpegSharp.Tests;

// Covers JPEG-PROG-01: a progressive DC scan may be NON-interleaved (Ns==1), which must traverse
// the single component's own block raster rather than the interleaved MCU grid. The stock encoder
// only emits interleaved DC scans, so the non-interleaved stream here is hand-built from the
// encoder's own entropy primitives (MarkerWriter, BitWriter, the standard DC Huffman tables,
// BlockScanCoder.MagnitudeCategory/Mantissa): one SOF2, then one DC SOS per component with Ns==1,
// each followed by that component's DC coefficients written in pure block-raster order.
public class ProgressiveNonInterleavedDcTests
{
    [Fact]
    public void Progressive_NonInterleavedDcScan_SubsampledComponent_DecodesCorrectly()
    {
        // 4:2:0: luma (id 1) is H=2,V=2 so its non-interleaved raster order differs from the
        // interleaved MCU-group order — the exact case the old code mis-decoded. Chroma (ids 2,3)
        // are half-resolution. Dimensions are multiples of 16 so there are no padded blocks and the
        // two encodings share an identical coefficient set; the ONLY difference is DC-scan traversal
        // order. Both are DC-only progressive streams (AC coefficients stay 0), decoded through the
        // same reconstruction, so an interleaved-vs-non-interleaved pixel match proves the
        // non-interleaved branch places every DC coefficient at the correct block.
        const int w = 32, h = 32;
        var y = Gradient(w, h, seed: 1);
        var cb = Gradient(w / 2, h / 2, seed: 5);
        var cr = Gradient(w / 2, h / 2, seed: 9);

        var interleaved = Jpeg.Decode(BuildProgressiveDcOnly(w, h, y, cb, cr, interleavedDc: true));
        var nonInterleaved = Jpeg.Decode(BuildProgressiveDcOnly(w, h, y, cb, cr, interleavedDc: false));

        Assert.Equal(w, nonInterleaved.Width);
        Assert.Equal(h, nonInterleaved.Height);
        Assert.Equal(interleaved.PixelData, nonInterleaved.PixelData);

        // Guard against a degenerate (all-DC-zero) decode that would trivially match: the gradient
        // must produce real variation across blocks.
        var min = 255; var max = 0;
        foreach (var v in nonInterleaved.PixelData)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
        Assert.True(max - min > 20, "expected real luma/chroma variation, not a flat image");
    }

    [Fact]
    public void Progressive_InterleavedDcScan_Unchanged()
    {
        // Grayscale progressive is a 1-component DC scan and now flows through the new
        // non-interleaved branch; it must still reconstruct bit-for-bit identically to baseline
        // (Hi=Vi=Hmax=Vmax=1 makes the raster geometry equal to the old interleaved geometry).
        var gray = Gradient(40, 40, seed: 3);
        var grayImg = JpegImage.CreateGrayscale(40, 40, gray);
        var grayBaseline = Jpeg.Decode(Jpeg.Encode(grayImg, new JpegEncoderOptions { Quality = 80 }));
        var grayProgressive = Jpeg.Decode(Jpeg.Encode(grayImg,
            new JpegEncoderOptions { Quality = 80, Progressive = true }));
        Assert.Equal(grayBaseline.PixelData, grayProgressive.PixelData);

        // Multi-component (interleaved, Ns>1) progressive DC scans keep the unchanged code path and
        // must still match baseline across subsamplings.
        var rgb = ColorGradient(48, 40);
        var rgbImg = JpegImage.CreateRgb(48, 40, rgb);
        foreach (var samp in new[] { ChromaSubsampling.Samp444, ChromaSubsampling.Samp420, ChromaSubsampling.Samp422 })
        {
            var baseline = Jpeg.Decode(Jpeg.Encode(rgbImg,
                new JpegEncoderOptions { Quality = 85, Subsampling = samp }));
            var progressive = Jpeg.Decode(Jpeg.Encode(rgbImg,
                new JpegEncoderOptions { Quality = 85, Subsampling = samp, Progressive = true }));
            Assert.Equal(baseline.PixelData, progressive.PixelData);
        }
    }

    // ----- helpers -----

    // Builds a progressive (SOF2) DC-only stream for a 4:2:0 YCbCr image. When interleavedDc is true
    // a single Ns=3 DC scan is emitted (MCU-group traversal); otherwise one Ns=1 DC scan per
    // component is emitted, each in pure block-raster order.
    private static byte[] BuildProgressiveDcOnly(int w, int h, byte[] y, byte[] cb, byte[] cr, bool interleavedDc)
    {
        var lumaQuant = QuantizationTable.Luminance(80);
        var chromaQuant = QuantizationTable.Chrominance(80);
        var ids = new byte[] { 1, 2, 3 };
        var hv = new[] { (H: 2, V: 2), (H: 1, V: 1), (H: 1, V: 1) };
        var planes = new[] { y, cb, cr };
        var planeW = new[] { w, w / 2, w / 2 };
        var planeH = new[] { h, h / 2, h / 2 };
        var quants = new[] { lumaQuant, chromaQuant, chromaQuant };
        var quantIds = new byte[] { 0, 1, 1 };
        var dcTables = new[] { StandardHuffmanTables.DcLuminance, StandardHuffmanTables.DcChrominance, StandardHuffmanTables.DcChrominance };
        var dcTableIds = new[] { 0, 1, 1 };

        // Quantized DC coefficient per block, in block-raster order, for each component.
        var dcValues = new int[3][];
        var blocksWide = new int[3];
        var blocksHigh = new int[3];
        for (var ci = 0; ci < 3; ci++)
        {
            blocksWide[ci] = (planeW[ci] + 7) / 8;
            blocksHigh[ci] = (planeH[ci] + 7) / 8;
            dcValues[ci] = ComputeDc(planes[ci], planeW[ci], planeH[ci], quants[ci]);
        }

        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteMarker(JpegMarkers.StartOfImage);

        WriteDqt(writer, 0, lumaQuant);
        WriteDqt(writer, 1, chromaQuant);

        // SOF2: three components (4:2:0), progressive.
        Span<byte> sof = stackalloc byte[6 + 3 * 3];
        var p = 0;
        sof[p++] = 8;
        sof[p++] = (byte)(h >> 8); sof[p++] = (byte)h;
        sof[p++] = (byte)(w >> 8); sof[p++] = (byte)w;
        sof[p++] = 3;
        for (var ci = 0; ci < 3; ci++)
        {
            sof[p++] = ids[ci];
            sof[p++] = (byte)((hv[ci].H << 4) | hv[ci].V);
            sof[p++] = quantIds[ci];
        }
        writer.WriteSegment(JpegMarkers.StartOfFrameProgressive, sof[..p]);

        WriteDht(writer, 0, 0, StandardHuffmanTables.DcLuminance);
        WriteDht(writer, 0, 1, StandardHuffmanTables.DcChrominance);

        if (interleavedDc)
        {
            // One interleaved DC scan (Ns=3): MCU-group traversal.
            Span<byte> sos = stackalloc byte[1 + 3 * 2 + 3];
            var q = 0;
            sos[q++] = 3;
            for (var ci = 0; ci < 3; ci++)
            {
                sos[q++] = ids[ci];
                sos[q++] = (byte)((dcTableIds[ci] << 4) | 0);
            }
            sos[q++] = 0; sos[q++] = 0; sos[q++] = 0; // Ss=0, Se=0, Ah/Al=0
            writer.WriteSegment(JpegMarkers.StartOfScan, sos[..q]);

            var bw = new BitWriter(ms);
            var predictors = new int[3];
            var hmax = 2; var vmax = 2;
            var mcusPerRow = (w + 8 * hmax - 1) / (8 * hmax);
            var mcusPerCol = (h + 8 * vmax - 1) / (8 * vmax);
            for (var my = 0; my < mcusPerCol; my++)
            {
                for (var mx = 0; mx < mcusPerRow; mx++)
                {
                    for (var ci = 0; ci < 3; ci++)
                    {
                        for (var by = 0; by < hv[ci].V; by++)
                        {
                            for (var bx = 0; bx < hv[ci].H; bx++)
                            {
                                var blockRow = my * hv[ci].V + by;
                                var blockCol = mx * hv[ci].H + bx;
                                var d = dcValues[ci][blockRow * blocksWide[ci] + blockCol];
                                EncodeDcDiff(bw, dcTables[ci], d - predictors[ci]);
                                predictors[ci] = d;
                            }
                        }
                    }
                }
            }
            bw.Flush();
        }
        else
        {
            // One non-interleaved DC scan per component (Ns=1): pure block-raster traversal.
            for (var ci = 0; ci < 3; ci++)
            {
                Span<byte> sos = stackalloc byte[1 + 2 + 3];
                sos[0] = 1;
                sos[1] = ids[ci];
                sos[2] = (byte)((dcTableIds[ci] << 4) | 0);
                sos[3] = 0; sos[4] = 0; sos[5] = 0; // Ss=0, Se=0, Ah/Al=0
                writer.WriteSegment(JpegMarkers.StartOfScan, sos);

                var bw = new BitWriter(ms);
                var predictor = 0;
                for (var by = 0; by < blocksHigh[ci]; by++)
                {
                    for (var bx = 0; bx < blocksWide[ci]; bx++)
                    {
                        var d = dcValues[ci][by * blocksWide[ci] + bx];
                        EncodeDcDiff(bw, dcTables[ci], d - predictor);
                        predictor = d;
                    }
                }
                bw.Flush();
            }
        }

        writer.WriteMarker(JpegMarkers.EndOfImage);
        return ms.ToArray();
    }

    private static void EncodeDcDiff(BitWriter bw, HuffmanTable dc, int diff)
    {
        var category = BlockScanCoder.MagnitudeCategory(diff);
        dc.Encode(bw, category);
        if (category > 0)
            bw.WriteBits(BlockScanCoder.Mantissa(diff, category), category);
    }

    private static int[] ComputeDc(byte[] plane, int w, int h, QuantizationTable quant)
    {
        var blocksWide = (w + 7) / 8;
        var blocksHigh = (h + 7) / 8;
        var result = new int[blocksWide * blocksHigh];
        Span<double> samples = stackalloc double[64];
        Span<double> coeffs = stackalloc double[64];
        Span<short> quantized = stackalloc short[64];
        for (var by = 0; by < blocksHigh; by++)
        {
            for (var bx = 0; bx < blocksWide; bx++)
            {
                ExtractBlock(plane, w, h, bx * 8, by * 8, samples);
                FastDct.Forward(samples, coeffs);
                Quantizer.Quantize(coeffs, quant.AsSpan(), quantized);
                result[by * blocksWide + bx] = quantized[0]; // natural DC == zig-zag index 0
            }
        }
        return result;
    }

    private static void ExtractBlock(byte[] plane, int w, int h, int x0, int y0, Span<double> samples)
    {
        for (var yy = 0; yy < 8; yy++)
        {
            var sy = Math.Min(y0 + yy, h - 1);
            for (var xx = 0; xx < 8; xx++)
            {
                var sx = Math.Min(x0 + xx, w - 1);
                samples[yy * 8 + xx] = plane[sy * w + sx] - 128;
            }
        }
    }

    private static void WriteDqt(MarkerWriter writer, int tableId, QuantizationTable quant)
    {
        Span<ushort> zig = stackalloc ushort[64];
        quant.CopyToZigZag(zig);
        Span<byte> dqt = stackalloc byte[1 + 64];
        dqt[0] = (byte)tableId;
        for (var k = 0; k < 64; k++)
            dqt[1 + k] = (byte)zig[k];
        writer.WriteSegment(JpegMarkers.DefineQuantizationTables, dqt);
    }

    private static void WriteDht(MarkerWriter writer, int tableClass, int tableId, HuffmanTable table)
    {
        var counts = table.Counts;
        var symbols = table.Symbols;
        Span<byte> payload = stackalloc byte[1 + 16 + 256];
        payload[0] = (byte)((tableClass << 4) | tableId);
        for (var i = 0; i < 16; i++)
            payload[1 + i] = counts[i];
        for (var i = 0; i < symbols.Length; i++)
            payload[17 + i] = symbols[i];
        writer.WriteSegment(JpegMarkers.DefineHuffmanTables, payload[..(17 + symbols.Length)]);
    }

    private static byte[] Gradient(int w, int h, int seed)
    {
        var p = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var v = x * 150 / Math.Max(1, w - 1) + y * 90 / Math.Max(1, h - 1) + seed * 6;
                p[y * w + x] = (byte)Math.Clamp(v, 0, 255);
            }
        return p;
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

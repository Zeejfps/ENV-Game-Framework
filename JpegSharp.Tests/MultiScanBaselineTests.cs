using JpegSharp.Api;
using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Huffman;
using JpegSharp.Markers;
using JpegSharp.Quantization;
using JpegSharp.Transforms;
using Xunit;

namespace JpegSharp.Tests;

// Covers JPEG-MCU-01 (non-interleaved, Ns==1 scans) and JPEG-MCU-02 (multi-scan baseline).
// The stock encoder only emits a single interleaved scan, so the multi-scan / non-interleaved
// streams here are hand-built from the encoder's own entropy primitives (MarkerWriter, BitWriter,
// BlockScanCoder.EncodeBlock, the standard luma Huffman tables): one SOF0, then one SOS per
// component with Ns==1, each followed by that component's non-interleaved entropy data.
public class MultiScanBaselineTests
{
    [Fact]
    public void NonInterleaved_SingleComponentScans_DecodeCorrectly()
    {
        const int w = 24, h = 16;
        var r = Gradient(w, h, seed: 1);
        var g = Gradient(w, h, seed: 2);
        var b = Gradient(w, h, seed: 3);

        var bytes = BuildNonInterleavedRgb(w, h, r, g, b);
        var decoded = Jpeg.Decode(bytes);

        Assert.Equal(w, decoded.Width);
        Assert.Equal(h, decoded.Height);
        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);

        // Direct-RGB component ids ('R','G','B') mean no YCbCr conversion, so each decoded channel
        // maps straight back to its source plane (within quantization/IDCT rounding).
        for (var i = 0; i < w * h; i++)
        {
            Assert.InRange(decoded.PixelData[i * 3] - r[i], -8, 8);
            Assert.InRange(decoded.PixelData[i * 3 + 1] - g[i], -8, 8);
            Assert.InRange(decoded.PixelData[i * 3 + 2] - b[i], -8, 8);
        }
    }

    [Fact]
    public void MultiScan_Baseline_AllScansProcessed()
    {
        const int w = 16, h = 16;
        // Solid per-channel colors: a constant plane survives DCT/quantize/IDCT exactly, so any
        // dropped scan would leave that channel at the level-shift default (128) instead.
        var r = Solid(w, h, 40);
        var g = Solid(w, h, 130);
        var b = Solid(w, h, 220);

        var decoded = Jpeg.Decode(BuildNonInterleavedRgb(w, h, r, g, b));

        for (var i = 0; i < w * h; i++)
        {
            Assert.InRange(decoded.PixelData[i * 3] - 40, -2, 2);
            Assert.InRange(decoded.PixelData[i * 3 + 1] - 130, -2, 2);
            Assert.InRange(decoded.PixelData[i * 3 + 2] - 220, -2, 2);
        }

        // Guard against the "only the first scan decoded" regression explicitly.
        Assert.NotEqual(128, decoded.PixelData[1]);
        Assert.NotEqual(128, decoded.PixelData[2]);
    }

    [Fact]
    public void Baseline_SingleInterleavedScan_Unchanged()
    {
        // Standard single-interleaved-scan baseline (the common case) must still decode, and do so
        // deterministically, through the restructured scan loop.
        var rgb = Gradient(40, 40, seed: 7);
        var color = new JpegImage(40, 40, JpegColorSpace.Rgb, Rgb(rgb));
        var colorBytes = Jpeg.Encode(color, new JpegEncoderOptions { Quality = 88, Subsampling = ChromaSubsampling.Samp420 });
        var d1 = Jpeg.Decode(colorBytes);
        var d2 = Jpeg.Decode(colorBytes);
        Assert.Equal(d1.PixelData, d2.PixelData);
        Assert.True(TestMetrics.Psnr(Rgb(rgb), d1.PixelData) > 30);

        var gray = JpegImage.CreateGrayscale(40, 40, Gradient(40, 40, seed: 9));
        var grayBytes = Jpeg.Encode(gray, new JpegEncoderOptions { Quality = 90 });
        var g1 = Jpeg.Decode(grayBytes);
        var g2 = Jpeg.Decode(grayBytes);
        Assert.Equal(g1.PixelData, g2.PixelData);
        Assert.True(TestMetrics.Psnr(gray.PixelData, g1.PixelData) > 30);
    }

    // ----- helpers -----

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

    private static byte[] Solid(int w, int h, byte value)
    {
        var p = new byte[w * h];
        Array.Fill(p, value);
        return p;
    }

    private static byte[] Rgb(byte[] gray)
    {
        var rgb = new byte[gray.Length * 3];
        for (var i = 0; i < gray.Length; i++)
        {
            rgb[i * 3] = gray[i];
            rgb[i * 3 + 1] = (byte)(255 - gray[i]);
            rgb[i * 3 + 2] = (byte)(gray[i] / 2);
        }
        return rgb;
    }

    // Builds a baseline SOF0 stream with three full-resolution (H=V=1) components each carried in
    // its own non-interleaved single-component scan. Component ids are 'R','G','B' so the decoder
    // treats the channels as direct RGB.
    private static byte[] BuildNonInterleavedRgb(int w, int h, byte[] r, byte[] g, byte[] b)
    {
        var quant = QuantizationTable.Luminance(90);
        var dc = StandardHuffmanTables.DcLuminance;
        var ac = StandardHuffmanTables.AcLuminance;
        var ids = new[] { (byte)'R', (byte)'G', (byte)'B' };
        var planes = new[] { r, g, b };

        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteMarker(JpegMarkers.StartOfImage);

        // DQT (8-bit, id 0), zig-zag order.
        Span<ushort> zig = stackalloc ushort[64];
        quant.CopyToZigZag(zig);
        Span<byte> dqt = stackalloc byte[1 + 64];
        dqt[0] = 0;
        for (var k = 0; k < 64; k++)
            dqt[1 + k] = (byte)zig[k];
        writer.WriteSegment(JpegMarkers.DefineQuantizationTables, dqt);

        // SOF0: three components, all H=V=1, quant id 0.
        Span<byte> sof = stackalloc byte[6 + 3 * 3];
        var p = 0;
        sof[p++] = 8;
        sof[p++] = (byte)(h >> 8); sof[p++] = (byte)h;
        sof[p++] = (byte)(w >> 8); sof[p++] = (byte)w;
        sof[p++] = 3;
        foreach (var id in ids)
        {
            sof[p++] = id;
            sof[p++] = 0x11; // H=1, V=1
            sof[p++] = 0;    // quant id 0
        }
        writer.WriteSegment(JpegMarkers.StartOfFrameBaseline, sof[..p]);

        // DHT: DC luma (class 0, id 0) and AC luma (class 1, id 0).
        WriteDht(writer, 0, 0, dc);
        WriteDht(writer, 1, 0, ac);

        var blocksWide = (w + 7) / 8;
        var blocksHigh = (h + 7) / 8;

        for (var ci = 0; ci < 3; ci++)
        {
            // SOS: Ns=1 referencing this component, DC/AC table 0.
            Span<byte> sos = stackalloc byte[1 + 2 + 3];
            sos[0] = 1;
            sos[1] = ids[ci];
            sos[2] = 0x00; // DC table 0, AC table 0
            sos[3] = 0;    // Ss
            sos[4] = 63;   // Se
            sos[5] = 0;    // Ah/Al
            writer.WriteSegment(JpegMarkers.StartOfScan, sos);

            var bitWriter = new BitWriter(ms);
            var predictor = 0;
            Span<double> samples = stackalloc double[64];
            Span<double> coeffs = stackalloc double[64];
            Span<short> quantized = stackalloc short[64];
            Span<short> block = stackalloc short[64];
            for (var by = 0; by < blocksHigh; by++)
            {
                for (var bx = 0; bx < blocksWide; bx++)
                {
                    ExtractBlock(planes[ci], w, h, bx * 8, by * 8, samples);
                    FastDct.Forward(samples, coeffs);
                    Quantizer.Quantize(coeffs, quant.AsSpan(), quantized);
                    ZigZag.FromNatural(quantized, block);
                    predictor = BlockScanCoder.EncodeBlock(bitWriter, block, predictor, dc, ac);
                }
            }
            bitWriter.Flush();
        }

        writer.WriteMarker(JpegMarkers.EndOfImage);
        return ms.ToArray();
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
}

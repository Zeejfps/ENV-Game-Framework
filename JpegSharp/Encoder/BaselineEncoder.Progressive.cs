using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Huffman;
using JpegSharp.Markers;
using JpegSharp.Quantization;
using JpegSharp.Transforms;

namespace JpegSharp.Encoder;

// Progressive (SOF2) encoding: the scan script (DC first/refine + per-component AC
// first/refine with successive approximation) and coefficient computation.
internal sealed partial class BaselineEncoder
{
    private void EncodeProgressive(Stream output)
    {
        var coefficients = BuildComponentCoefficients();
        var tables = StandardTables();

        var writer = new MarkerWriter(output);
        WriteHeader(writer, JpegMarkers.StartOfFrameProgressive, tables);

        var allComponents = new int[_components.Length];
        for (var i = 0; i < allComponents.Length; i++)
            allComponents[i] = i;

        // DC scans: first (Al=1) then a refinement (Ah=1, Al=0), both interleaved.
        WriteScan(writer, allComponents, 0, 0, 0, 1);
        WriteDcScanEntropy(output, coefficients, tables, al: 1);
        WriteScan(writer, allComponents, 0, 0, 1, 0);
        WriteDcRefineScan(output, coefficients, al: 0);

        // AC scans per component: first (Al=1) then a refinement (Ah=1, Al=0).
        for (var ci = 0; ci < _components.Length; ci++)
        {
            WriteScan(writer, [ci], 1, 63, 0, 1);
            WriteAcScanEntropy(output, ci, coefficients, tables, al: 1);
            WriteScan(writer, [ci], 1, 63, 1, 0);
            WriteAcRefineScan(output, ci, coefficients, tables, al: 0);
        }

        writer.WriteMarker(JpegMarkers.EndOfImage);
    }

    private short[][] BuildComponentCoefficients()
    {
        var result = new short[_components.Length][];
        Span<double> samples = stackalloc double[64];
        Span<double> coeffs = stackalloc double[64];
        Span<short> quantized = stackalloc short[64];
        Span<short> zz = stackalloc short[64];

        for (var ci = 0; ci < _components.Length; ci++)
        {
            var c = _components[ci];
            var table = _quantTables[c.QuantId].AsSpan();
            var buffer = new short[c.BlocksWide * c.BlocksHigh * 64];
            for (var by = 0; by < c.BlocksHigh; by++)
            {
                for (var bx = 0; bx < c.BlocksWide; bx++)
                {
                    ExtractBlock(c, bx * 8, by * 8, samples);
                    FastDct.Forward(samples, coeffs);
                    Quantizer.Quantize(coeffs, table, quantized);
                    ZigZag.FromNatural(quantized, zz);
                    var offset = (by * c.BlocksWide + bx) * 64;
                    zz.CopyTo(buffer.AsSpan(offset, 64));
                }
            }

            result[ci] = buffer;
        }

        return result;
    }

    private void WriteDcScanEntropy(Stream output, short[][] coefficients, HuffmanTable[] tables, int al)
    {
        var writer = new BitWriter(output);
        var predictors = new int[_components.Length];
        var interval = _options.RestartInterval;
        var mcuIndex = 0;
        var rstIndex = 0;

        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                if (interval > 0 && mcuIndex > 0 && mcuIndex % interval == 0)
                {
                    writer.Flush();
                    output.WriteByte(0xFF);
                    output.WriteByte((byte)(JpegMarkers.Restart0 + (rstIndex++ & 7)));
                    Array.Clear(predictors);
                }

                for (var ci = 0; ci < _components.Length; ci++)
                {
                    var c = _components[ci];
                    var dc = c.TableClass == 0 ? tables[0] : tables[2];
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            var blockCol = mx * c.H + bx;
                            var blockRow = my * c.V + by;
                            var offset = (blockRow * c.BlocksWide + blockCol) * 64;
                            var dcValue = coefficients[ci][offset] >> al; // point transform (arithmetic)
                            var diff = dcValue - predictors[ci];
                            predictors[ci] = dcValue;
                            var category = BlockScanCoder.MagnitudeCategory(diff);
                            dc.Encode(writer, category);
                            if (category > 0)
                                writer.WriteBits(BlockScanCoder.Mantissa(diff, category), category);
                        }
                    }
                }

                mcuIndex++;
            }
        }

        writer.Flush();
    }

    private void WriteDcRefineScan(Stream output, short[][] coefficients, int al)
    {
        var writer = new BitWriter(output);
        var interval = _options.RestartInterval;
        var mcuIndex = 0;
        var rstIndex = 0;

        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                if (interval > 0 && mcuIndex > 0 && mcuIndex % interval == 0)
                {
                    writer.Flush();
                    output.WriteByte(0xFF);
                    output.WriteByte((byte)(JpegMarkers.Restart0 + (rstIndex++ & 7)));
                }

                for (var ci = 0; ci < _components.Length; ci++)
                {
                    var c = _components[ci];
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            var blockCol = mx * c.H + bx;
                            var blockRow = my * c.V + by;
                            var offset = (blockRow * c.BlocksWide + blockCol) * 64;
                            writer.WriteBits((coefficients[ci][offset] >> al) & 1, 1);
                        }
                    }
                }

                mcuIndex++;
            }
        }

        writer.Flush();
    }

    private void WriteAcScanEntropy(Stream output, int ci, short[][] coefficients, HuffmanTable[] tables, int al)
    {
        var c = _components[ci];
        var ac = c.TableClass == 0 ? tables[1] : tables[3];
        var buffer = coefficients[ci];
        var blocksPerLine = CeilDiv(c.PlaneWidth, 8);
        var blocksPerCol = CeilDiv(c.PlaneHeight, 8);

        var writer = new BitWriter(output);
        var interval = _options.RestartInterval;
        var blockIndex = 0;
        var rstIndex = 0;

        for (var by = 0; by < blocksPerCol; by++)
        {
            for (var bx = 0; bx < blocksPerLine; bx++)
            {
                if (interval > 0 && blockIndex > 0 && blockIndex % interval == 0)
                {
                    writer.Flush();
                    output.WriteByte(0xFF);
                    output.WriteByte((byte)(JpegMarkers.Restart0 + (rstIndex++ & 7)));
                }

                var offset = (by * c.BlocksWide + bx) * 64;
                var run = 0;
                for (var k = 1; k < 64; k++)
                {
                    var value = PointTransform(buffer[offset + k], al);
                    if (value == 0)
                    {
                        run++;
                        continue;
                    }

                    while (run > 15)
                    {
                        ac.Encode(writer, 0xF0);
                        run -= 16;
                    }

                    var category = BlockScanCoder.MagnitudeCategory(value);
                    ac.Encode(writer, (run << 4) | category);
                    writer.WriteBits(BlockScanCoder.Mantissa(value, category), category);
                    run = 0;
                }

                if (run > 0)
                    ac.Encode(writer, 0x00); // EOB

                blockIndex++;
            }
        }

        writer.Flush();
    }

    private void WriteAcRefineScan(Stream output, int ci, short[][] coefficients, HuffmanTable[] tables, int al)
    {
        var c = _components[ci];
        var ac = c.TableClass == 0 ? tables[1] : tables[3];
        var buffer = coefficients[ci];
        var blocksPerLine = CeilDiv(c.PlaneWidth, 8);
        var blocksPerCol = CeilDiv(c.PlaneHeight, 8);

        var writer = new BitWriter(output);
        var interval = _options.RestartInterval;
        var blockIndex = 0;
        var rstIndex = 0;
        Span<int> correctionBits = stackalloc int[64];

        for (var by = 0; by < blocksPerCol; by++)
        {
            for (var bx = 0; bx < blocksPerLine; bx++)
            {
                if (interval > 0 && blockIndex > 0 && blockIndex % interval == 0)
                {
                    writer.Flush();
                    output.WriteByte(0xFF);
                    output.WriteByte((byte)(JpegMarkers.Restart0 + (rstIndex++ & 7)));
                }

                var offset = (by * c.BlocksWide + bx) * 64;
                var run = 0;
                var br = 0;
                for (var k = 1; k < 64; k++)
                {
                    var value = buffer[offset + k];
                    var absShifted = (value < 0 ? -value : value) >> al;
                    if (absShifted == 0)
                    {
                        run++;
                        continue;
                    }

                    while (run > 15)
                    {
                        ac.Encode(writer, 0xF0);
                        run -= 16;
                        for (var i = 0; i < br; i++)
                            writer.WriteBits(correctionBits[i], 1);
                        br = 0;
                    }

                    if (absShifted > 1)
                    {
                        // Already-nonzero coefficient: buffer its correction bit.
                        correctionBits[br++] = absShifted & 1;
                        continue;
                    }

                    // Newly-nonzero coefficient (magnitude becomes 1 at this precision).
                    ac.Encode(writer, (run << 4) | 1);
                    writer.WriteBits(value < 0 ? 0 : 1, 1); // sign bit: 1 = positive
                    for (var i = 0; i < br; i++)
                        writer.WriteBits(correctionBits[i], 1);
                    br = 0;
                    run = 0;
                }

                if (run > 0 || br > 0)
                {
                    ac.Encode(writer, 0x00); // EOB (single)
                    for (var i = 0; i < br; i++)
                        writer.WriteBits(correctionBits[i], 1);
                }

                blockIndex++;
            }
        }

        writer.Flush();
    }

    private static int PointTransform(int value, int al)
    {
        // Integer division toward zero by 2^al.
        return value >= 0 ? value >> al : -((-value) >> al);
    }

    private void WriteScan(MarkerWriter writer, int[] componentIndices, int ss, int se, int ah, int al)
    {
        Span<byte> payload = stackalloc byte[1 + 2 * 4 + 3];
        var p = 0;
        payload[p++] = (byte)componentIndices.Length;
        foreach (var ci in componentIndices)
        {
            var c = _components[ci];
            payload[p++] = (byte)c.Id;
            payload[p++] = (byte)((c.TableClass << 4) | c.TableClass);
        }

        payload[p++] = (byte)ss;
        payload[p++] = (byte)se;
        payload[p++] = (byte)((ah << 4) | al);
        writer.WriteSegment(JpegMarkers.StartOfScan, payload[..p]);
    }
}

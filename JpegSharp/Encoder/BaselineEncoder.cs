using JpegSharp.Api;
using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Color;
using JpegSharp.Huffman;
using JpegSharp.Markers;
using JpegSharp.Quantization;
using JpegSharp.Transforms;

namespace JpegSharp.Encoder;

/// <summary>
/// Encodes a <see cref="JpegImage"/> as a baseline sequential DCT JPEG (SOF0) with Huffman
/// coding. Supports grayscale and RGB (encoded as YCbCr) with configurable chroma subsampling
/// and optional optimized Huffman tables.
/// </summary>
internal sealed class BaselineEncoder
{
    private sealed class Component
    {
        public required int Id;
        public required int H;
        public required int V;
        public required int QuantId;
        public required int TableClass; // 0 = luma tables, 1 = chroma tables
        public required byte[] Plane;
        public required int PlaneWidth;
        public required int PlaneHeight;
        public int BlocksWide;
        public int BlocksHigh;
    }

    private readonly JpegImage _image;
    private readonly JpegEncoderOptions _options;
    private readonly int _hmax;
    private readonly int _vmax;
    private readonly int _mcusPerRow;
    private readonly int _mcusPerCol;
    private readonly Component[] _components;
    private readonly QuantizationTable[] _quantTables;
    private readonly bool _hasChromaTables;
    private readonly bool _writeAdobe;
    private readonly int _adobeTransform;
    private readonly JpegMetadata? _metadata;

    /// <summary>The maximum image dimension representable in a JPEG frame header (16-bit).</summary>
    private const int MaxDimension = 65535;

    public BaselineEncoder(JpegImage image, JpegEncoderOptions options)
    {
        _image = image;
        _options = options;
        _metadata = options.Metadata ?? image.Metadata;

        if (image.Width > MaxDimension || image.Height > MaxDimension)
            throw new ArgumentException($"JPEG dimensions must not exceed {MaxDimension}; got {image.Width}x{image.Height}.", nameof(image));
        if (options.RestartInterval > MaxDimension)
            throw new ArgumentException($"RestartInterval must not exceed {MaxDimension}; got {options.RestartInterval}.", nameof(options));

        if (image.ColorSpace == JpegColorSpace.Grayscale)
        {
            _hmax = _vmax = 1;
            _quantTables = [LumaQuant(options)];
            _components =
            [
                new Component
                {
                    Id = 1, H = 1, V = 1, QuantId = 0, TableClass = 0,
                    Plane = (byte[])image.PixelData.Clone(),
                    PlaneWidth = image.Width, PlaneHeight = image.Height,
                },
            ];
        }
        else if (image.ColorSpace == JpegColorSpace.Rgb)
        {
            if (options.RgbEncoding == JpegRgbEncoding.Rgb)
            {
                _hmax = _vmax = 1;
                _quantTables = [LumaQuant(options)];
                _components = BuildRgbDirectComponents(image);
                _writeAdobe = true;
                _adobeTransform = 0;
            }
            else
            {
                var (h, v) = options.Subsampling.LumaFactors();
                _hmax = h;
                _vmax = v;
                _quantTables = [LumaQuant(options), ChromaQuant(options)];
                _components = BuildColorComponents(image, h, v);
            }
        }
        else if (image.ColorSpace == JpegColorSpace.Cmyk)
        {
            _hmax = _vmax = 1;
            _writeAdobe = true;
            if (options.CmykAsYcck)
            {
                _quantTables = [LumaQuant(options), ChromaQuant(options)];
                _components = BuildYcckComponents(image);
                _adobeTransform = 2;
            }
            else
            {
                _quantTables = [LumaQuant(options)];
                _components = BuildCmykComponents(image);
                _adobeTransform = 0;
            }
        }
        else
        {
            throw new NotSupportedException($"Encoding of {image.ColorSpace} is not supported.");
        }

        _hasChromaTables = Array.Exists(_components, c => c.TableClass == 1);
        _mcusPerRow = CeilDiv(image.Width, 8 * _hmax);
        _mcusPerCol = CeilDiv(image.Height, 8 * _vmax);
        foreach (var c in _components)
        {
            c.BlocksWide = _mcusPerRow * c.H;
            c.BlocksHigh = _mcusPerCol * c.V;
        }
    }

    public void Encode(Stream output)
    {
        if (_options.Progressive)
        {
            EncodeProgressive(output);
            return;
        }

        // Compute all quantized zig-zag blocks in interleaved scan order.
        var blocks = BuildQuantizedBlocks(out var blockComponent);

        // Select Huffman tables (standard or optimized).
        var tableSet = _options.OptimizeHuffman
            ? BuildOptimizedTables(blocks, blockComponent)
            : StandardTables();

        var writer = new MarkerWriter(output);
        WriteHeader(writer, JpegMarkers.StartOfFrameBaseline, tableSet);
        WriteScanHeader(writer);
        WriteEntropyData(output, blocks, blockComponent, tableSet);
        writer.WriteMarker(JpegMarkers.EndOfImage);
    }

    /// <summary>
    /// Writes the common header prelude shared by baseline and progressive output: SOI, the
    /// APPn/COM metadata segments, quantization tables, the frame header, Huffman tables, and
    /// the optional restart-interval definition.
    /// </summary>
    private void WriteHeader(MarkerWriter writer, byte frameMarker, HuffmanTable[] tables)
    {
        writer.WriteMarker(JpegMarkers.StartOfImage);
        if (_writeAdobe)
            WriteAdobe(writer);
        else
            WriteJfif(writer);
        WriteExif(writer);
        WriteIcc(writer);
        WriteComments(writer);
        WriteApplicationSegments(writer);
        WriteQuantTables(writer);
        WriteFrameHeader(writer, frameMarker);
        WriteHuffmanTables(writer, tables);
        if (_options.RestartInterval > 0)
            WriteDri(writer);
    }

    private static QuantizationTable LumaQuant(JpegEncoderOptions options) =>
        options.LuminanceQuantizationTable ?? QuantizationTable.Luminance(options.Quality);

    private static QuantizationTable ChromaQuant(JpegEncoderOptions options) =>
        options.ChrominanceQuantizationTable ?? QuantizationTable.Chrominance(options.Quality);

    private static Component[] BuildColorComponents(JpegImage image, int h, int v)
    {
        var pixelCount = image.Width * image.Height;
        var y = new byte[pixelCount];
        var cbFull = new byte[pixelCount];
        var crFull = new byte[pixelCount];
        ColorConverter.RgbToYCbCr(image.PixelData, y, cbFull, crFull);

        var chromaWidth = ChromaSampler.SubsampledSize(image.Width, h);
        var chromaHeight = ChromaSampler.SubsampledSize(image.Height, v);
        var cb = new byte[chromaWidth * chromaHeight];
        var cr = new byte[chromaWidth * chromaHeight];
        ChromaSampler.Downsample(cbFull, image.Width, image.Height, h, v, cb, chromaWidth, chromaHeight);
        ChromaSampler.Downsample(crFull, image.Width, image.Height, h, v, cr, chromaWidth, chromaHeight);

        return
        [
            new Component { Id = 1, H = h, V = v, QuantId = 0, TableClass = 0, Plane = y, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 2, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cb, PlaneWidth = chromaWidth, PlaneHeight = chromaHeight },
            new Component { Id = 3, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cr, PlaneWidth = chromaWidth, PlaneHeight = chromaHeight },
        ];
    }

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

    private static Component[] BuildCmykComponents(JpegImage image)
    {
        // Adobe CMYK is stored inverted (255 - value); all four components are full resolution.
        var pixelCount = image.Width * image.Height;
        var components = new Component[4];
        for (var ch = 0; ch < 4; ch++)
        {
            var plane = new byte[pixelCount];
            for (var i = 0; i < pixelCount; i++)
                plane[i] = (byte)(255 - image.PixelData[i * 4 + ch]);
            components[ch] = new Component
            {
                Id = ch + 1, H = 1, V = 1, QuantId = 0, TableClass = 0,
                Plane = plane, PlaneWidth = image.Width, PlaneHeight = image.Height,
            };
        }

        return components;
    }

    private static Component[] BuildRgbDirectComponents(JpegImage image)
    {
        // Store R, G, B directly (no color transform, no subsampling). Component ids 'R','G','B'
        // let decoders without an Adobe marker still recognize the layout.
        var pixelCount = image.Width * image.Height;
        var r = new byte[pixelCount];
        var g = new byte[pixelCount];
        var b = new byte[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            r[i] = image.PixelData[i * 3];
            g[i] = image.PixelData[i * 3 + 1];
            b[i] = image.PixelData[i * 3 + 2];
        }

        return
        [
            new Component { Id = (byte)'R', H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = r, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = (byte)'G', H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = g, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = (byte)'B', H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = b, PlaneWidth = image.Width, PlaneHeight = image.Height },
        ];
    }

    private static Component[] BuildYcckComponents(JpegImage image)
    {
        // Adobe YCCK: apply the YCbCr transform to the inverted CMY channels, and store the
        // inverted K channel. All four components are full resolution.
        var pixelCount = image.Width * image.Height;
        var y = new byte[pixelCount];
        var cb = new byte[pixelCount];
        var cr = new byte[pixelCount];
        var k = new byte[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            var r = (byte)(255 - image.PixelData[i * 4]);
            var g = (byte)(255 - image.PixelData[i * 4 + 1]);
            var b = (byte)(255 - image.PixelData[i * 4 + 2]);
            ColorConverter.RgbToYCbCr(r, g, b, out y[i], out cb[i], out cr[i]);
            k[i] = (byte)(255 - image.PixelData[i * 4 + 3]);
        }

        return
        [
            new Component { Id = 1, H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = y, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 2, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cb, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 3, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cr, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 4, H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = k, PlaneWidth = image.Width, PlaneHeight = image.Height },
        ];
    }

    private short[][] BuildQuantizedBlocks(out int[] blockComponent)
    {
        var perMcu = 0;
        foreach (var c in _components)
            perMcu += c.H * c.V;
        var totalBlocks = _mcusPerRow * _mcusPerCol * perMcu;

        var blocks = new short[totalBlocks][];
        blockComponent = new int[totalBlocks];

        Span<double> samples = stackalloc double[64];
        Span<double> coeffs = stackalloc double[64];
        Span<short> quantized = stackalloc short[64];

        var index = 0;
        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                for (var ci = 0; ci < _components.Length; ci++)
                {
                    var c = _components[ci];
                    var table = _quantTables[c.QuantId].AsSpan();
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            var blockCol = mx * c.H + bx;
                            var blockRow = my * c.V + by;
                            ExtractBlock(c, blockCol * 8, blockRow * 8, samples);
                            FastDct.Forward(samples, coeffs);
                            Quantizer.Quantize(coeffs, table, quantized);
                            var zz = new short[64];
                            ZigZag.FromNatural(quantized, zz);
                            blocks[index] = zz;
                            blockComponent[index] = ci;
                            index++;
                        }
                    }
                }
            }
        }

        return blocks;
    }

    private static void ExtractBlock(Component c, int x0, int y0, Span<double> samples)
    {
        for (var yy = 0; yy < 8; yy++)
        {
            var sy = Math.Min(y0 + yy, c.PlaneHeight - 1);
            var row = sy * c.PlaneWidth;
            for (var xx = 0; xx < 8; xx++)
            {
                var sx = Math.Min(x0 + xx, c.PlaneWidth - 1);
                samples[yy * 8 + xx] = c.Plane[row + sx] - 128;
            }
        }
    }

    private HuffmanTable[] StandardTables()
    {
        // Index layout: [0]=DC luma, [1]=AC luma, [2]=DC chroma, [3]=AC chroma.
        // Custom tables from the options override the standard ones.
        return
        [
            _options.LuminanceDcHuffmanTable ?? StandardHuffmanTables.DcLuminance,
            _options.LuminanceAcHuffmanTable ?? StandardHuffmanTables.AcLuminance,
            _options.ChrominanceDcHuffmanTable ?? StandardHuffmanTables.DcChrominance,
            _options.ChrominanceAcHuffmanTable ?? StandardHuffmanTables.AcChrominance,
        ];
    }

    private HuffmanTable[] BuildOptimizedTables(short[][] blocks, int[] blockComponent)
    {
        var dcLuma = new int[256];
        var acLuma = new int[256];
        var dcChroma = new int[256];
        var acChroma = new int[256];
        var predictors = new int[_components.Length];

        // Mirror the restart-interval predictor resets performed by WriteEntropyData so the
        // gathered DC symbol frequencies exactly match the symbols that will be emitted.
        var perMcu = 0;
        foreach (var c in _components)
            perMcu += c.H * c.V;

        var interval = _options.RestartInterval;
        var mcuIndex = 0;
        var blockInMcu = 0;

        for (var i = 0; i < blocks.Length; i++)
        {
            if (blockInMcu == 0 && interval > 0 && mcuIndex > 0 && mcuIndex % interval == 0)
                Array.Clear(predictors);

            var ci = blockComponent[i];
            var c = _components[ci];
            if (c.TableClass == 0)
                predictors[ci] = BlockScanCoder.GatherBlockFrequencies(blocks[i], predictors[ci], dcLuma, acLuma);
            else
                predictors[ci] = BlockScanCoder.GatherBlockFrequencies(blocks[i], predictors[ci], dcChroma, acChroma);

            blockInMcu++;
            if (blockInMcu == perMcu)
            {
                blockInMcu = 0;
                mcuIndex++;
            }
        }

        return
        [
            HuffmanTable.BuildOptimized(dcLuma),
            HuffmanTable.BuildOptimized(acLuma),
            _hasChromaTables ? HuffmanTable.BuildOptimized(dcChroma) : StandardHuffmanTables.DcChrominance,
            _hasChromaTables ? HuffmanTable.BuildOptimized(acChroma) : StandardHuffmanTables.AcChrominance,
        ];
    }

    private void WriteEntropyData(Stream output, short[][] blocks, int[] blockComponent, HuffmanTable[] tables)
    {
        var writer = new BitWriter(output);
        var predictors = new int[_components.Length];

        var perMcu = 0;
        foreach (var c in _components)
            perMcu += c.H * c.V;

        var interval = _options.RestartInterval;
        var mcuIndex = 0;
        var blockInMcu = 0;
        var rstIndex = 0;

        for (var i = 0; i < blocks.Length; i++)
        {
            if (blockInMcu == 0 && interval > 0 && mcuIndex > 0 && mcuIndex % interval == 0)
            {
                writer.Flush();
                output.WriteByte(0xFF);
                output.WriteByte((byte)(JpegMarkers.Restart0 + (rstIndex & 7)));
                rstIndex++;
                Array.Clear(predictors);
            }

            var ci = blockComponent[i];
            var component = _components[ci];
            var dc = component.TableClass == 0 ? tables[0] : tables[2];
            var ac = component.TableClass == 0 ? tables[1] : tables[3];
            predictors[ci] = BlockScanCoder.EncodeBlock(writer, blocks[i], predictors[ci], dc, ac);

            blockInMcu++;
            if (blockInMcu == perMcu)
            {
                blockInMcu = 0;
                mcuIndex++;
            }
        }

        writer.Flush();
    }

    private void WriteDri(MarkerWriter writer)
    {
        Span<byte> payload = [(byte)(_options.RestartInterval >> 8), (byte)(_options.RestartInterval & 0xFF)];
        writer.WriteSegment(JpegMarkers.DefineRestartInterval, payload);
    }

    private void WriteJfif(MarkerWriter writer)
    {
        var density = _metadata?.Density ?? new JfifDensity(JpegDensityUnit.None, 1, 1);
        var x = Math.Clamp(density.X, 1, MaxDimension);
        var y = Math.Clamp(density.Y, 1, MaxDimension);
        Span<byte> payload =
        [
            (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0x00,
            0x01, 0x01,              // version 1.1
            (byte)density.Unit,
            (byte)(x >> 8), (byte)(x & 0xFF),
            (byte)(y >> 8), (byte)(y & 0xFF),
            0x00, 0x00,              // no thumbnail
        ];
        writer.WriteSegment(JpegMarkers.App0, payload);
    }

    private void WriteExif(MarkerWriter writer)
    {
        var exif = _metadata?.Exif;
        if (exif is null || exif.Length == 0)
            return;

        var payload = new byte[6 + exif.Length];
        payload[0] = (byte)'E';
        payload[1] = (byte)'x';
        payload[2] = (byte)'i';
        payload[3] = (byte)'f';
        payload[4] = 0;
        payload[5] = 0;
        exif.CopyTo(payload, 6);
        writer.WriteSegment(JpegMarkers.App1, payload);
    }

    private void WriteIcc(MarkerWriter writer)
    {
        var icc = _metadata?.IccProfile;
        if (icc is null || icc.Length == 0)
            return;

        ReadOnlySpan<byte> identifier = "ICC_PROFILE\0"u8;
        const int maxData = 65533 - 14; // segment limit minus (identifier + seq + count)
        var chunkCount = (icc.Length + maxData - 1) / maxData;
        if (chunkCount > 255)
            throw new JpegSharp.Api.Exceptions.JpegException("ICC profile is too large to embed (exceeds 255 chunks).");

        for (var i = 0; i < chunkCount; i++)
        {
            var offset = i * maxData;
            var length = Math.Min(maxData, icc.Length - offset);
            var payload = new byte[14 + length];
            identifier.CopyTo(payload);
            payload[12] = (byte)(i + 1);
            payload[13] = (byte)chunkCount;
            Array.Copy(icc, offset, payload, 14, length);
            writer.WriteSegment(JpegMarkers.App2, payload);
        }
    }

    private void WriteComments(MarkerWriter writer)
    {
        if (_metadata is null)
            return;

        foreach (var comment in _metadata.Comments)
            writer.WriteSegment(JpegMarkers.Comment, System.Text.Encoding.UTF8.GetBytes(comment));
    }

    private void WriteApplicationSegments(MarkerWriter writer)
    {
        if (_metadata is null)
            return;

        foreach (var segment in _metadata.ApplicationSegments)
            writer.WriteSegment(segment.MarkerCode, segment.Data);
    }

    private void WriteAdobe(MarkerWriter writer)
    {
        // "Adobe" + version 100 + flags0 + flags1 + transform (0 = no color transform, CMYK).
        Span<byte> payload =
        [
            (byte)'A', (byte)'d', (byte)'o', (byte)'b', (byte)'e',
            0x00, 0x64, // version 100
            0x00, 0x00, // flags0
            0x00, 0x00, // flags1
            (byte)_adobeTransform,
        ];
        writer.WriteSegment(JpegMarkers.App14, payload);
    }

    private void WriteQuantTables(MarkerWriter writer)
    {
        Span<byte> payload8 = stackalloc byte[1 + 64];
        Span<byte> payload16 = stackalloc byte[1 + 128];
        Span<ushort> zig = stackalloc ushort[64];
        for (var id = 0; id < _quantTables.Length; id++)
        {
            _quantTables[id].CopyToZigZag(zig);

            var needs16Bit = false;
            for (var k = 0; k < 64; k++)
            {
                if (zig[k] > 255)
                {
                    needs16Bit = true;
                    break;
                }
            }

            if (needs16Bit)
            {
                payload16[0] = (byte)(0x10 | id); // precision 1 (16-bit) | table id
                for (var k = 0; k < 64; k++)
                {
                    payload16[1 + 2 * k] = (byte)(zig[k] >> 8);
                    payload16[1 + 2 * k + 1] = (byte)(zig[k] & 0xFF);
                }
                writer.WriteSegment(JpegMarkers.DefineQuantizationTables, payload16);
            }
            else
            {
                payload8[0] = (byte)id; // precision 0 (8-bit) | table id
                for (var k = 0; k < 64; k++)
                    payload8[1 + k] = (byte)zig[k];
                writer.WriteSegment(JpegMarkers.DefineQuantizationTables, payload8);
            }
        }
    }

    private void WriteFrameHeader(MarkerWriter writer, byte marker)
    {
        var n = _components.Length;
        Span<byte> payload = stackalloc byte[6 + 3 * 4];
        var p = 0;
        payload[p++] = 8; // sample precision
        payload[p++] = (byte)(_image.Height >> 8);
        payload[p++] = (byte)(_image.Height & 0xFF);
        payload[p++] = (byte)(_image.Width >> 8);
        payload[p++] = (byte)(_image.Width & 0xFF);
        payload[p++] = (byte)n;
        foreach (var c in _components)
        {
            payload[p++] = (byte)c.Id;
            payload[p++] = (byte)((c.H << 4) | c.V);
            payload[p++] = (byte)c.QuantId;
        }

        writer.WriteSegment(marker, payload[..p]);
    }

    private void WriteHuffmanTables(MarkerWriter writer, HuffmanTable[] tables)
    {
        // DC luma (class 0, id 0), AC luma (class 1, id 0).
        WriteDht(writer, 0, 0, tables[0]);
        WriteDht(writer, 1, 0, tables[1]);
        if (_hasChromaTables)
        {
            WriteDht(writer, 0, 1, tables[2]);
            WriteDht(writer, 1, 1, tables[3]);
        }
    }

    private static void WriteDht(MarkerWriter writer, int tableClass, int tableId, HuffmanTable table)
    {
        var symbols = table.Symbols;
        Span<byte> payload = stackalloc byte[1 + 16 + 256];
        payload[0] = (byte)((tableClass << 4) | tableId);
        var counts = table.Counts;
        for (var i = 0; i < 16; i++)
            payload[1 + i] = counts[i];
        for (var i = 0; i < symbols.Length; i++)
            payload[17 + i] = symbols[i];
        writer.WriteSegment(JpegMarkers.DefineHuffmanTables, payload[..(17 + symbols.Length)]);
    }

    private void WriteScanHeader(MarkerWriter writer)
    {
        var n = _components.Length;
        Span<byte> payload = stackalloc byte[1 + 2 * 4 + 3];
        var p = 0;
        payload[p++] = (byte)n;
        foreach (var c in _components)
        {
            payload[p++] = (byte)c.Id;
            var tableId = c.TableClass; // luma tables id 0, chroma tables id 1
            payload[p++] = (byte)((tableId << 4) | tableId);
        }

        payload[p++] = 0;  // Ss
        payload[p++] = 63; // Se
        payload[p++] = 0;  // Ah/Al
        writer.WriteSegment(JpegMarkers.StartOfScan, payload[..p]);
    }

    private static int CeilDiv(int a, int b) => (a + b - 1) / b;
}

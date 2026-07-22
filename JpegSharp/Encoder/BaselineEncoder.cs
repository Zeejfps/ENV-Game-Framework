using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
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
internal sealed partial class BaselineEncoder
{
    private sealed class Component
    {
        public required int Id;
        public required int H;
        public required int V;
        public required int QuantId;
        public required int TableClass; // 0 = luma tables, 1 = chroma tables
        public required byte[] Plane;       // sample plane for 8-bit input
        public ushort[]? Plane16;           // sample plane for high-precision input
        public required int PlaneWidth;
        public required int PlaneHeight;
        public int BlocksWide;
        public int BlocksHigh;
    }

    private readonly JpegEncoderOptions _options;
    private readonly int _width;
    private readonly int _height;
    private readonly int _precision;
    private readonly int _hmax;
    private readonly int _vmax;
    private int _mcusPerRow;
    private int _mcusPerCol;
    private readonly Component[] _components;
    private readonly QuantizationTable[] _quantTables;
    private bool _hasChromaTables;
    private readonly bool _writeAdobe;
    private readonly int _adobeTransform;
    private readonly JpegMetadata? _metadata;

    /// <summary>The maximum image dimension representable in a JPEG frame header (16-bit).</summary>
    private const int MaxDimension = 65535;

    public BaselineEncoder(JpegImage image, JpegEncoderOptions options)
    {
        _options = options;
        _metadata = options.Metadata ?? image.Metadata;
        _width = image.Width;
        _height = image.Height;
        _precision = 8;

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

        InitGeometry();
    }

    /// <summary>
    /// Encodes a high-precision (9–16 bit) image as an extended-sequential (SOF1) Huffman JPEG.
    /// Supports grayscale and RGB (as YCbCr, or stored directly with <see cref="JpegRgbEncoding.Rgb"/>);
    /// chroma subsampling and progressive output are not supported at high precision.
    /// </summary>
    public BaselineEncoder(JpegImage16 image, JpegEncoderOptions options)
    {
        _options = options;
        _metadata = options.Metadata ?? image.Metadata;
        _width = image.Width;
        _height = image.Height;
        _precision = image.Precision;

        if (image.Precision != 12)
            throw new NotSupportedException($"Only 12-bit high-precision JPEG encoding is supported; got {image.Precision}-bit. (JPEG DCT sample precision is 8 or 12 per ITU-T T.81.)");
        if (image.Width > MaxDimension || image.Height > MaxDimension)
            throw new ArgumentException($"JPEG dimensions must not exceed {MaxDimension}; got {image.Width}x{image.Height}.", nameof(image));

        if (image.ColorSpace == JpegColorSpace.Grayscale)
        {
            _hmax = _vmax = 1;
            _quantTables = [LumaQuant(options)];
            _components = [HighPrecisionComponent(1, 0, 0, ClonePlane(image.PixelData))];
        }
        else if (image.ColorSpace == JpegColorSpace.Rgb)
        {
            if (options.RgbEncoding == JpegRgbEncoding.Rgb)
            {
                _hmax = _vmax = 1; // direct RGB is not subsampled
                _quantTables = [LumaQuant(options)];
                _components = BuildRgbDirectComponents16(image);
                _writeAdobe = true;
                _adobeTransform = 0;
            }
            else
            {
                var (h, v) = options.Subsampling.LumaFactors();
                _hmax = h;
                _vmax = v;
                _quantTables = [LumaQuant(options), ChromaQuant(options)];
                _components = BuildYCbCrComponents16(image, h, v);
            }
        }
        else if (image.ColorSpace == JpegColorSpace.Cmyk)
        {
            _hmax = _vmax = 1; // CMYK/YCCK components are full resolution
            _writeAdobe = true;
            if (options.CmykAsYcck)
            {
                _quantTables = [LumaQuant(options), ChromaQuant(options)];
                _components = BuildYcckComponents16(image);
                _adobeTransform = 2;
            }
            else
            {
                _quantTables = [LumaQuant(options)];
                _components = BuildCmykComponents16(image);
                _adobeTransform = 0;
            }
        }
        else
        {
            throw new NotSupportedException($"High-precision encoding of {image.ColorSpace} is not supported.");
        }

        InitGeometry();
    }

    private void InitGeometry()
    {
        _hasChromaTables = Array.Exists(_components, c => c.TableClass == 1);
        _mcusPerRow = CeilDiv(_width, 8 * _hmax);
        _mcusPerCol = CeilDiv(_height, 8 * _vmax);
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

        // Select Huffman tables. High precision forces optimized tables: the standard tables only
        // define DC categories up to 11 (8-bit), whereas 9–16 bit blocks can reach category 15.
        var tableSet = _options.OptimizeHuffman || _precision > 8
            ? BuildOptimizedTables(blocks, blockComponent)
            : StandardTables();

        // 12-bit and other >8-bit precisions are extended-sequential (SOF1), not baseline (SOF0).
        var frameMarker = _precision == 8
            ? JpegMarkers.StartOfFrameBaseline
            : JpegMarkers.StartOfFrameExtendedSequential;
        var writer = new MarkerWriter(output);
        WriteHeader(writer, frameMarker, tableSet);
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
        var order = _metadata?.HeaderSegmentOrder;
        if (order is { Count: > 0 })
            WriteMetadataSegmentsInOrder(writer, order);
        else
            WriteMetadataSegmentsFixedOrder(writer);
        WriteQuantTables(writer);
        WriteFrameHeader(writer, frameMarker);
        WriteHuffmanTables(writer, tables);
        if (_options.RestartInterval > 0)
            WriteDri(writer);
    }

    private QuantizationTable LumaQuant(JpegEncoderOptions options) =>
        options.LuminanceQuantizationTable ?? QuantizationTable.Luminance(options.Quality, _precision);

    private QuantizationTable ChromaQuant(JpegEncoderOptions options) =>
        options.ChrominanceQuantizationTable ?? QuantizationTable.Chrominance(options.Quality, _precision);

    // Baseline blocks are stored in one flat buffer (64 shorts per block, in scan order) to
    // avoid a per-block array allocation on large images.
    private short[] BuildQuantizedBlocks(out int[] blockComponent)
    {
        var perMcu = 0;
        foreach (var c in _components)
            perMcu += c.H * c.V;
        var totalBlocks = _mcusPerRow * _mcusPerCol * perMcu;

        var blocks = new short[totalBlocks * 64];
        blockComponent = new int[totalBlocks];

        Span<double> samples = stackalloc double[64];
        Span<double> coeffs = stackalloc double[64];

        var index = 0;
        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                for (var ci = 0; ci < _components.Length; ci++)
                {
                    var c = _components[ci];
                    var table = _quantTables[c.QuantId].AsZigZagSpan();
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            var blockCol = mx * c.H + bx;
                            var blockRow = my * c.V + by;
                            ExtractBlock(c, blockCol * 8, blockRow * 8, samples);
                            FastDct.Forward(samples, coeffs);
                            Quantizer.QuantizeToZigZag(coeffs, table, blocks.AsSpan(index * 64, 64));
                            blockComponent[index] = ci;
                            index++;
                        }
                    }
                }
            }
        }

        return blocks;
    }

    private void ExtractBlock(Component c, int x0, int y0, Span<double> samples)
    {
        if (x0 + 8 <= c.PlaneWidth && y0 + 8 <= c.PlaneHeight)
        {
            if (_precision == 8)
            {
                var plane = c.Plane;
                var row = y0 * c.PlaneWidth + x0;
                for (var yy = 0; yy < 8; yy++)
                {
                    for (var xx = 0; xx < 8; xx++)
                        samples[yy * 8 + xx] = plane[row + xx] - 128;
                    row += c.PlaneWidth;
                }
            }
            else
            {
                var center = 1 << (_precision - 1);
                var plane = c.Plane16!;
                var row = y0 * c.PlaneWidth + x0;
                for (var yy = 0; yy < 8; yy++)
                {
                    for (var xx = 0; xx < 8; xx++)
                        samples[yy * 8 + xx] = plane[row + xx] - center;
                    row += c.PlaneWidth;
                }
            }

            return;
        }

        if (_precision == 8)
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
        else
        {
            var center = 1 << (_precision - 1);
            var plane = c.Plane16!;
            for (var yy = 0; yy < 8; yy++)
            {
                var sy = Math.Min(y0 + yy, c.PlaneHeight - 1);
                var row = sy * c.PlaneWidth;
                for (var xx = 0; xx < 8; xx++)
                {
                    var sx = Math.Min(x0 + xx, c.PlaneWidth - 1);
                    samples[yy * 8 + xx] = plane[row + sx] - center;
                }
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

    private HuffmanTable[] BuildOptimizedTables(short[] blocks, int[] blockComponent)
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

        for (var i = 0; i < blockComponent.Length; i++)
        {
            if (blockInMcu == 0 && interval > 0 && mcuIndex > 0 && mcuIndex % interval == 0)
                Array.Clear(predictors);

            var ci = blockComponent[i];
            var c = _components[ci];
            var block = blocks.AsSpan(i * 64, 64);
            if (c.TableClass == 0)
                predictors[ci] = BlockScanCoder.GatherBlockFrequencies(block, predictors[ci], dcLuma, acLuma);
            else
                predictors[ci] = BlockScanCoder.GatherBlockFrequencies(block, predictors[ci], dcChroma, acChroma);

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

    private void WriteEntropyData(Stream output, short[] blocks, int[] blockComponent, HuffmanTable[] tables)
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

        for (var i = 0; i < blockComponent.Length; i++)
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
            predictors[ci] = BlockScanCoder.EncodeBlock(writer, blocks.AsSpan(i * 64, 64), predictors[ci], dc, ac);

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

            if (needs16Bit && _precision == 8)
                throw new JpegException("8-bit JPEG quantization table steps must be <= 255 (Pq=0 per T.81 B.2.4.1); a custom table contains a value that requires 16-bit precision.");

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
        payload[p++] = (byte)_precision; // sample precision
        payload[p++] = (byte)(_height >> 8);
        payload[p++] = (byte)(_height & 0xFF);
        payload[p++] = (byte)(_width >> 8);
        payload[p++] = (byte)(_width & 0xFF);
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

using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Color;
using JpegSharp.Huffman;
using JpegSharp.Markers;
using JpegSharp.Quantization;
using JpegSharp.Transforms;

namespace JpegSharp.Decoder;

/// <summary>
/// Decodes a baseline sequential DCT (SOF0) Huffman-coded JPEG into a <see cref="JpegImage"/>.
/// </summary>
internal sealed partial class BaselineDecoder
{
    private sealed class Component
    {
        public int Id;
        public int H;
        public int V;
        public int QuantId;
        public int DcTableId;
        public int AcTableId;
        public byte[] Plane = [];        // sample plane for 8-bit images
        public ushort[] Plane16 = [];    // sample plane for high-precision (9–16 bit) images
        public int PlaneWidth;
        public int PlaneHeight;
        public int BlocksWide;
        public int BlocksHigh;
    }

    private readonly byte[] _data;
    private readonly JpegDecoderOptions _options;
    private readonly QuantizationTable?[] _quantTables = new QuantizationTable?[4];
    private readonly HuffmanTable?[] _dcTables = new HuffmanTable?[4];
    private readonly HuffmanTable?[] _acTables = new HuffmanTable?[4];

    private int _width;
    private int _height;
    private int _hmax;
    private int _vmax;
    private Component[] _components = [];
    private int _restartInterval;
    private int _adobeTransform = -1;
    private int _precision = 8;
    private bool _isProgressive;

    private JfifDensity? _density;
    private byte[]? _exif;
    private readonly List<(int Seq, int Count, byte[] Data)> _iccChunks = [];
    private readonly List<string> _comments = [];
    private readonly List<byte[]> _commentBytes = [];
    private readonly List<JpegApplicationSegment> _appSegments = [];
    private readonly List<HeaderSegmentRef> _headerOrder = [];
    private bool _jfifRecorded;
    private bool _exifRecorded;
    private bool _iccRecorded;
    private bool _adobeRecorded;

    private ScanHeader _scan;
    private short[][] _coefficients = [];
    private int _mcusPerRow;
    private int _mcusPerCol;

    private struct ScanHeader
    {
        public int[] Components; // indices into _components
        public int Ss;
        public int Se;
        public int Ah;
        public int Al;
    }

    public BaselineDecoder(byte[] data, JpegDecoderOptions? options = null)
    {
        _data = data;
        _options = options ?? new JpegDecoderOptions();
    }

    public JpegImage Decode()
    {
        try
        {
            using var ms = new MemoryStream(_data, writable: false);
            var reader = new MarkerReader(ms);
            RequireSoi(reader);
            var entropyStart = ParseHeaders(reader, ms);

            if (_precision != 8)
                throw new JpegFormatException($"Unsupported sample precision {_precision} for Decode; only 8-bit is supported. Use Decode16 / DecodeAnyPrecision for higher precision.");
            ValidatePixelBudget();

            FillPlanes(reader, ms, entropyStart);
            var image = AssembleImage(_mcusPerRow, _mcusPerCol);
            if (_options.ReadMetadata)
                image.Metadata = BuildMetadata();
            return image;
        }
        catch (Exception ex) when (WrapAsJpegException(ex) is { } wrapped)
        {
            throw wrapped;
        }
    }

    public JpegImage16 Decode16()
    {
        try
        {
            using var ms = new MemoryStream(_data, writable: false);
            var reader = new MarkerReader(ms);
            RequireSoi(reader);
            var entropyStart = ParseHeaders(reader, ms);

            if (_precision == 8)
                throw new JpegFormatException("This is an 8-bit JPEG; use Decode.");
            // JPEG DCT sample precision is 8 or 12 (ITU-T T.81); 13–16 bit would overflow the
            // 16-bit coefficient buffers, so it is rejected rather than silently corrupted.
            if (_precision != 12)
                throw new JpegFormatException($"Unsupported sample precision {_precision}; JPEG DCT precision is 8 or 12.");
            ValidatePixelBudget();

            FillPlanes(reader, ms, entropyStart);
            var image = AssembleImage16(_mcusPerRow, _mcusPerCol);
            if (_options.ReadMetadata)
                image.Metadata = BuildMetadata();
            return image;
        }
        catch (Exception ex) when (WrapAsJpegException(ex) is { } wrapped)
        {
            throw wrapped;
        }
    }

    // Single typed-failure contract for the public decode entry points: any failure during decode
    // surfaces as a JpegException subtype. Already-typed JpegExceptions (all the DQT/DHT/SOF/SOS
    // and geometry guards) pass through unchanged so their specific type/message survives; any
    // other exception (IndexOutOfRange, Overflow, ArgumentException from a deep helper) is wrapped.
    // OutOfMemoryException from a hostile allocation is intentionally converted to a typed failure.
    // Returns null (leaving the exception to propagate) for already-typed failures.
    private static JpegException? WrapAsJpegException(Exception ex)
    {
        if (ex is JpegException)
            return null;
        return new JpegFormatException("Malformed JPEG stream.", ex);
    }

    private void ValidatePixelBudget()
    {
        if ((long)_width * _height > _options.MaxPixels)
            throw new JpegFormatException($"Image {_width}x{_height} exceeds the configured maximum of {_options.MaxPixels} pixels.");
    }

    // Runs entropy decoding + inverse transform, filling each component's sample plane (byte
    // planes for 8-bit, ushort planes for high precision). Assembly into an image is separate.
    private void FillPlanes(MarkerReader reader, MemoryStream ms, int entropyStart)
    {
        if (_isProgressive)
            DecodeProgressive(reader, ms, entropyStart);
        else
            DecodeSequential(reader, ms, entropyStart);
    }

    public JpegInfo ReadInfo()
    {
        try
        {
            using var ms = new MemoryStream(_data, writable: false);
            var reader = new MarkerReader(ms);
            RequireSoi(reader);
            ParseHeaders(reader, ms);

            var colorSpace = _components.Length switch
            {
                1 => JpegColorSpace.Grayscale,
                4 => JpegColorSpace.Cmyk,
                _ => JpegColorSpace.Rgb,
            };
            return new JpegInfo(_width, _height, _components.Length, colorSpace, _precision, _isProgressive);
        }
        catch (Exception ex) when (WrapAsJpegException(ex) is { } wrapped)
        {
            throw wrapped;
        }
    }

    private static void RequireSoi(MarkerReader reader)
    {
        if (reader.ReadMarker() != JpegMarkers.StartOfImage)
            throw new JpegFormatException("Missing SOI marker; not a JPEG stream.");
    }

    private int ParseHeaders(MarkerReader reader, MemoryStream ms)
    {
        while (true)
        {
            var marker = reader.ReadMarker();
            if (marker == JpegMarkers.EndOfImage)
                throw new JpegFormatException("Reached EOI before any scan data.");

            if (marker == JpegMarkers.StartOfScan)
            {
                ParseScanHeader(reader.ReadSegment());
                return (int)ms.Position;
            }

            // Standalone markers (RSTn/TEM) carry no length field; skip them rather than
            // misreading the following bytes as a segment length.
            if (!JpegMarkers.HasLengthField(marker))
                continue;

            var segment = reader.ReadSegment();
            switch (marker)
            {
                case JpegMarkers.DefineQuantizationTables:
                    ParseQuantTables(segment);
                    break;
                case JpegMarkers.DefineHuffmanTables:
                    ParseHuffmanTables(segment);
                    break;
                case JpegMarkers.StartOfFrameBaseline:
                case JpegMarkers.StartOfFrameExtendedSequential:
                case JpegMarkers.StartOfFrameProgressive:
                    ParseFrameHeader(segment, marker);
                    break;
                case JpegMarkers.DefineArithmeticConditioning:
                    throw new JpegFormatException(
                        "Unsupported entropy coding (DAC marker 0xCC): arithmetic coding is not supported; " +
                        "only Huffman-coded baseline, extended sequential, and progressive frames are supported.");
                case JpegMarkers.DefineRestartInterval:
                    if (segment.Length < 2)
                        throw new JpegFormatException("Truncated DRI segment.");
                    _restartInterval = (segment[0] << 8) | segment[1];
                    break;
                case JpegMarkers.App0:
                    if (ParseJfif(segment))
                        RecordOnce(HeaderSegmentKind.Jfif, ref _jfifRecorded);
                    else
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.App1:
                    if (ParseExif(segment))
                        RecordOnce(HeaderSegmentKind.Exif, ref _exifRecorded);
                    else
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.App2:
                    if (ParseIccChunk(segment))
                        RecordOnce(HeaderSegmentKind.Icc, ref _iccRecorded);
                    else
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.App14:
                    if (ParseAdobe(segment))
                        RecordOnce(HeaderSegmentKind.Adobe, ref _adobeRecorded);
                    else
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.Comment:
                    CaptureComment(segment);
                    break;
                default:
                    if (JpegMarkers.IsAppMarker(marker))
                        PreserveApp(marker, segment);
                    else if (JpegMarkers.IsStartOfFrame(marker))
                        // Any other Start-of-Frame variant (lossless, differential, or
                        // arithmetic coding) is a frame type this decoder does not implement.
                        throw new JpegFormatException(
                            $"Unsupported frame type (SOF marker 0x{marker:X2}): only baseline, extended sequential, " +
                            "and progressive Huffman-coded frames are supported. Arithmetic coding and " +
                            "lossless/differential modes are not supported.");
                    break;
            }
        }
    }

    private void ParseQuantTables(ReadOnlySpan<byte> segment)
    {
        var p = 0;
        Span<ushort> zig = stackalloc ushort[64];
        while (p < segment.Length)
        {
            var pqTq = segment[p++];
            var precision = pqTq >> 4;
            var id = pqTq & 0x0F;
            if (id >= _quantTables.Length)
                throw new JpegFormatException($"Invalid quantization table id {id}.");
            if (precision > 1)
                throw new JpegFormatException($"Invalid quantization table precision {precision}.");

            if (precision == 0)
            {
                if (p + 64 > segment.Length)
                    throw new JpegFormatException("Truncated 8-bit quantization table.");
                for (var k = 0; k < 64; k++)
                    zig[k] = segment[p++];
            }
            else
            {
                if (p + 128 > segment.Length)
                    throw new JpegFormatException("Truncated 16-bit quantization table.");
                for (var k = 0; k < 64; k++)
                {
                    zig[k] = (ushort)((segment[p] << 8) | segment[p + 1]);
                    p += 2;
                }
            }

            try
            {
                _quantTables[id] = QuantizationTable.FromZigZag(zig);
            }
            catch (ArgumentException e)
            {
                throw new JpegFormatException("Invalid quantization table.", e);
            }
        }
    }

    private void ParseHuffmanTables(ReadOnlySpan<byte> segment)
    {
        var p = 0;
        while (p < segment.Length)
        {
            var tcTh = segment[p++];
            var tableClass = tcTh >> 4;
            var id = tcTh & 0x0F;
            if (tableClass > 1)
                throw new JpegFormatException($"Invalid Huffman table class {tableClass}.");
            if (id >= 4)
                throw new JpegFormatException($"Invalid Huffman table id {id}.");
            if (p + 16 > segment.Length)
                throw new JpegFormatException("Truncated Huffman table counts.");

            var counts = segment.Slice(p, 16);
            p += 16;
            var total = 0;
            for (var i = 0; i < 16; i++)
                total += counts[i];
            if (total > 256)
                throw new JpegFormatException($"Huffman table has {total} codes, exceeding the maximum of 256.");
            if (p + total > segment.Length)
                throw new JpegFormatException("Truncated Huffman table symbols.");

            var symbols = segment.Slice(p, total);
            p += total;

            HuffmanTable table;
            try
            {
                table = new HuffmanTable(counts, symbols);
            }
            catch (ArgumentException e)
            {
                throw new JpegFormatException("Invalid Huffman table.", e);
            }

            if (tableClass == 0)
                _dcTables[id] = table;
            else
                _acTables[id] = table;
        }
    }

    private void ParseFrameHeader(ReadOnlySpan<byte> segment, byte marker)
    {
        if (segment.Length < 6)
            throw new JpegFormatException("Truncated frame header.");

        _precision = segment[0];
        _isProgressive = marker == JpegMarkers.StartOfFrameProgressive;
        var precisionValid = marker == JpegMarkers.StartOfFrameBaseline
            ? _precision == 8
            : _precision is 8 or 12;
        if (!precisionValid)
            throw new JpegFormatException(
                $"Unsupported sample precision {_precision} for SOF marker 0x{marker:X2}; baseline (SOF0) requires " +
                "8-bit, extended sequential (SOF1) and progressive (SOF2) require 8- or 12-bit.");
        _height = (segment[1] << 8) | segment[2];
        _width = (segment[3] << 8) | segment[4];
        var count = segment[5];
        if (_width <= 0 || _height <= 0)
            throw new JpegFormatException("Invalid image dimensions.");
        if (count == 0 || segment.Length < 6 + count * 3)
            throw new JpegFormatException("Truncated or invalid frame component list.");

        _components = new Component[count];
        var p = 6;
        _hmax = 1;
        _vmax = 1;
        for (var i = 0; i < count; i++)
        {
            var c = new Component
            {
                Id = segment[p],
                H = segment[p + 1] >> 4,
                V = segment[p + 1] & 0x0F,
                QuantId = segment[p + 2],
            };
            p += 3;
            if (c.H is < 1 or > 4 || c.V is < 1 or > 4)
                throw new JpegFormatException($"Invalid component sampling factor {c.H}x{c.V}; must be 1..4.");
            _hmax = Math.Max(_hmax, c.H);
            _vmax = Math.Max(_vmax, c.V);
            _components[i] = c;
        }
    }

    private void ParseScanHeader(ReadOnlySpan<byte> segment)
    {
        if (segment.Length < 1)
            throw new JpegFormatException("Truncated scan header.");
        var count = segment[0];
        if (count == 0 || count > 4 || segment.Length < 1 + count * 2 + 3)
            throw new JpegFormatException("Truncated or invalid scan component list; Ns must be 1..4.");
        var p = 1;
        var indices = new int[count];
        for (var i = 0; i < count; i++)
        {
            var id = segment[p];
            var tables = segment[p + 1];
            p += 2;
            var index = FindComponentIndex(id);
            _components[index].DcTableId = tables >> 4;
            _components[index].AcTableId = tables & 0x0F;
            indices[i] = index;
        }

        if (count > 1)
        {
            var dataUnits = 0;
            foreach (var index in indices)
                dataUnits += _components[index].H * _components[index].V;
            if (dataUnits > 10)
                throw new JpegFormatException(
                    $"Sum of sampling factors Σ(Hi·Vi)={dataUnits} in interleaved scan exceeds the maximum of 10 data units per MCU (ITU-T T.81 A.2.2).");
        }

        var ss = segment[p];
        var se = segment[p + 1];
        var ahAl = segment[p + 2];
        if (!_isProgressive && (ss != 0 || se != 63 || (ahAl >> 4) != 0 || (ahAl & 0x0F) != 0))
            throw new JpegFormatException($"Sequential scan requires Ss=0, Se=63, Ah=0, Al=0 (got Ss={ss}, Se={se}, Ah={ahAl >> 4}, Al={ahAl & 0x0F}).");
        _scan = new ScanHeader
        {
            Components = indices,
            Ss = ss,
            Se = se,
            Ah = ahAl >> 4,
            Al = ahAl & 0x0F,
        };
    }

    private int FindComponentIndex(int id)
    {
        for (var i = 0; i < _components.Length; i++)
            if (_components[i].Id == id)
                return i;
        throw new JpegFormatException($"Scan references unknown component id {id}.");
    }

    private void SetupGeometry()
    {
        if (_components.Length is not (1 or 3 or 4))
            throw new JpegFormatException($"Unsupported component count {_components.Length}.");

        _mcusPerRow = CeilDiv(_width, 8 * _hmax);
        _mcusPerCol = CeilDiv(_height, 8 * _vmax);

        // Hostile dimensions/sampling (T.81 allows up to 65535x65535 with 4x4 sampling) can make
        // these size products overflow 32-bit, yielding a small/negative allocation that corrupts
        // decode. Compute them in long and reject before allocating. Independent of MaxPixels:
        // sampling can inflate a component plane or coefficient buffer past int.MaxValue even when
        // width*height is within the pixel budget.
        if ((long)_width * _height * _components.Length > int.MaxValue)
            throw new JpegFormatException(
                $"Image {_width}x{_height} with {_components.Length} components exceeds the maximum decodable buffer size.");

        foreach (var c in _components)
        {
            c.BlocksWide = _mcusPerRow * c.H;
            c.BlocksHigh = _mcusPerCol * c.V;
            // Both the sample plane (PlaneWidth*PlaneHeight == BlocksWide*BlocksHigh*64) and the
            // progressive coefficient buffer (BlocksWide*BlocksHigh*64) share this size, so one
            // guard here protects the sequential and progressive allocation paths alike.
            var bufferSize = (long)c.BlocksWide * c.BlocksHigh * 64;
            if (bufferSize > int.MaxValue)
                throw new JpegFormatException(
                    $"Component plane/coefficient buffer size {bufferSize} exceeds the maximum decodable buffer size.");
            c.PlaneWidth = c.BlocksWide * 8;
            c.PlaneHeight = c.BlocksHigh * 8;
        }

        // A pixel-count bound (MaxPixels) does not bound peak memory: component count, precision,
        // and progressive coefficient buffers all amplify bytes-per-pixel. Estimate the peak
        // allocation in long and reject before allocating anything. Enforced alongside MaxPixels
        // (whichever trips first) and independently of the overflow guards above.
        var bytesPerSample = _precision == 8 ? 1 : 2;
        long estimatedBytes = 0;
        foreach (var c in _components)
        {
            estimatedBytes += (long)c.PlaneWidth * c.PlaneHeight * bytesPerSample;
            if (_isProgressive)
                estimatedBytes += (long)c.BlocksWide * c.BlocksHigh * 64 * sizeof(short);
        }
        estimatedBytes += (long)_width * _height * _components.Length * bytesPerSample;

        // Chroma upsampling during assembly allocates a full MCU-padded scratch plane for EACH
        // subsampled component (UpsampleToFull/UpsampleToFull16 return the plane in place only when
        // it is already full resolution — c.H==hmax && c.V==vmax — otherwise they allocate
        // fullWidth*fullHeight samples). These scratch planes coexist with the sample planes and the
        // output buffer at peak, so they belong in the bound. fullWidth/fullHeight are the padded
        // dimensions the assemblers pass (mcusPerRow*hmax*8 × mcusPerCol*vmax*8). A single-component
        // (grayscale) image never subsamples and never upsamples, so nothing is added for it.
        var paddedWidth = (long)_mcusPerRow * _hmax * 8;
        var paddedHeight = (long)_mcusPerCol * _vmax * 8;
        foreach (var c in _components)
        {
            if (c.H < _hmax || c.V < _vmax)
                estimatedBytes += paddedWidth * paddedHeight * bytesPerSample;
        }
        if (estimatedBytes > _options.MaxDecodedBytes)
            throw new JpegFormatException(
                $"Estimated decode allocation {estimatedBytes} bytes exceeds the configured maximum of {_options.MaxDecodedBytes}.");

        foreach (var c in _components)
        {
            if (_precision == 8)
                c.Plane = new byte[c.PlaneWidth * c.PlaneHeight];
            else
                c.Plane16 = new ushort[c.PlaneWidth * c.PlaneHeight];
        }
    }

    // A baseline/extended-sequential frame may be coded as multiple scans (ITU-T T.81 A.2).
    // Mirror the progressive driver: decode a scan, seek past it, parse any tables between
    // scans, and continue at the next SOS until EOI.
    private void DecodeSequential(MarkerReader reader, MemoryStream ms, int entropyStart)
    {
        SetupGeometry();

        var scan = _scan;
        var pos = entropyStart;
        while (true)
        {
            pos = DecodeSequentialScan(scan, pos);
            ms.Position = pos;

            var marker = reader.ReadMarker();
            while (marker != JpegMarkers.EndOfImage && marker != JpegMarkers.StartOfScan)
            {
                var segment = reader.ReadSegment();
                switch (marker)
                {
                    case JpegMarkers.DefineQuantizationTables:
                        ParseQuantTables(segment);
                        break;
                    case JpegMarkers.DefineHuffmanTables:
                        ParseHuffmanTables(segment);
                        break;
                    case JpegMarkers.DefineRestartInterval:
                        if (segment.Length < 2)
                            throw new JpegFormatException("Truncated DRI segment.");
                        _restartInterval = (segment[0] << 8) | segment[1];
                        break;
                    case JpegMarkers.Comment:
                        CaptureComment(segment);
                        break;

                    // Other markers between scans (APPn) are ignored during decode.
                }

                marker = reader.ReadMarker();
            }

            if (marker == JpegMarkers.EndOfImage)
                break;

            ParseScanHeader(reader.ReadSegment());
            scan = _scan;
            pos = (int)ms.Position;
        }
    }

    private int DecodeSequentialScan(ScanHeader scan, int pos)
    {
        var reader = new BitReader(_data.AsSpan(pos));
        if (scan.Components.Length > 1)
            DecodeInterleavedScan(ref reader, scan);
        else
            DecodeNonInterleavedScan(ref reader, scan);
        return pos + reader.BytePosition;
    }

    private void DecodeInterleavedScan(ref BitReader reader, ScanHeader scan)
    {
        var predictors = new int[_components.Length];
        Span<short> zz = stackalloc short[64];
        Span<double> dequant = stackalloc double[64];
        Span<double> spatial = stackalloc double[64];

        var mcuCount = 0;

        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                if (_restartInterval > 0 && mcuCount > 0 && mcuCount % _restartInterval == 0)
                {
                    var expectedRst = (mcuCount / _restartInterval - 1) & 7;
                    reader.SkipRestartMarker(expectedRst, _options.StrictRestartMarkers);
                    Array.Clear(predictors);
                }

                // Interleaved MCUs follow the scan's component order (which may differ from the
                // frame's), per ITU-T T.81 B.2.3.
                foreach (var ci in scan.Components)
                {
                    var c = _components[ci];
                    var quant = GetQuantTable(c.QuantId).AsZigZagSpan();
                    var dc = GetDcTable(c.DcTableId);
                    var ac = GetAcTable(c.AcTableId);
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            predictors[ci] = BlockScanCoder.DecodeBlock(ref reader, zz, predictors[ci], dc, ac);
                            Quantizer.DequantizeFromZigZag(zz, quant, dequant);
                            FastDct.Inverse(dequant, spatial);
                            StoreBlock(c, (mx * c.H + bx) * 8, (my * c.V + by) * 8, spatial);
                        }
                    }
                }

                mcuCount++;
            }
        }
    }

    // Non-interleaved scan (Ns==1): one data unit per MCU over the component's own block grid,
    // sized from its actual pixel dimensions (ITU-T T.81 A.2.2 / A.2.4).
    private void DecodeNonInterleavedScan(ref BitReader reader, ScanHeader scan)
    {
        var c = _components[scan.Components[0]];
        var quant = GetQuantTable(c.QuantId).AsZigZagSpan();
        var dc = GetDcTable(c.DcTableId);
        var ac = GetAcTable(c.AcTableId);
        var blocksPerLine = CeilDiv(ComponentActualWidth(c), 8);
        var blocksPerCol = CeilDiv(ComponentActualHeight(c), 8);

        Span<short> zz = stackalloc short[64];
        Span<double> dequant = stackalloc double[64];
        Span<double> spatial = stackalloc double[64];

        var predictor = 0;
        var blockIndex = 0;

        for (var by = 0; by < blocksPerCol; by++)
        {
            for (var bx = 0; bx < blocksPerLine; bx++)
            {
                if (_restartInterval > 0 && blockIndex > 0 && blockIndex % _restartInterval == 0)
                {
                    var expectedRst = (blockIndex / _restartInterval - 1) & 7;
                    reader.SkipRestartMarker(expectedRst, _options.StrictRestartMarkers);
                    predictor = 0;
                }

                blockIndex++;

                predictor = BlockScanCoder.DecodeBlock(ref reader, zz, predictor, dc, ac);
                Quantizer.DequantizeFromZigZag(zz, quant, dequant);
                FastDct.Inverse(dequant, spatial);
                StoreBlock(c, bx * 8, by * 8, spatial);
            }
        }
    }

    // Level-shifts, rounds and clamps an inverse-DCT block into the component's sample plane.
    // The level-shift offset and clamp ceiling scale with the sample precision.
    private void StoreBlock(Component c, int x0, int y0, ReadOnlySpan<double> spatial)
    {
        if (_precision == 8)
        {
            for (var yy = 0; yy < 8; yy++)
            {
                var row = (y0 + yy) * c.PlaneWidth + x0;
                for (var xx = 0; xx < 8; xx++)
                    c.Plane[row + xx] = LevelShiftClamp8(spatial[yy * 8 + xx]);
            }
        }
        else
        {
            var center = 1 << (_precision - 1);
            var max = (1 << _precision) - 1;
            for (var yy = 0; yy < 8; yy++)
            {
                var row = (y0 + yy) * c.PlaneWidth + x0;
                for (var xx = 0; xx < 8; xx++)
                    c.Plane16[row + xx] = LevelShiftClampHigh(spatial[yy * 8 + xx], center, max);
            }
        }
    }

    internal static byte LevelShiftClamp8(double spatial)
    {
        var value = Math.Round(spatial) + 128;
        return (byte)Math.Clamp(value, 0.0, 255.0);
    }

    internal static ushort LevelShiftClampHigh(double spatial, int center, int max)
    {
        var value = Math.Round(spatial) + center;
        return (ushort)Math.Clamp(value, 0.0, (double)max);
    }

    private JpegImage AssembleImage(int mcusPerRow, int mcusPerCol)
    {
        if (_components.Length == 1)
        {
            var c = _components[0];
            var output = new byte[_width * _height];
            for (var y = 0; y < _height; y++)
                Array.Copy(c.Plane, y * c.PlaneWidth, output, y * _width, _width);
            return new JpegImage(_width, _height, JpegColorSpace.Grayscale, output);
        }

        var paddedWidth = mcusPerRow * _hmax * 8;
        var paddedHeight = mcusPerCol * _vmax * 8;

        return _components.Length == 3
            ? AssembleThreeComponent(paddedWidth, paddedHeight)
            : AssembleCmyk(paddedWidth, paddedHeight);
    }

    private JpegImage AssembleThreeComponent(int paddedWidth, int paddedHeight)
    {
        var p0 = _components[0].Plane;
        var p1 = UpsampleToFull(_components[1], paddedWidth, paddedHeight);
        var p2 = UpsampleToFull(_components[2], paddedWidth, paddedHeight);
        var applyYCbCr = ShouldApplyYCbCr();

        var rgb = new byte[_width * _height * 3];
        for (var y = 0; y < _height; y++)
        {
            var srcRow = y * paddedWidth;
            var dstRow = y * _width * 3;
            if (applyYCbCr)
            {
                ColorConverter.YCbCrToRgb(
                    p0.AsSpan(srcRow, _width),
                    p1.AsSpan(srcRow, _width),
                    p2.AsSpan(srcRow, _width),
                    rgb.AsSpan(dstRow, _width * 3));
            }
            else
            {
                for (var x = 0; x < _width; x++)
                {
                    var d = dstRow + x * 3;
                    rgb[d] = p0[srcRow + x];
                    rgb[d + 1] = p1[srcRow + x];
                    rgb[d + 2] = p2[srcRow + x];
                }
            }
        }

        return new JpegImage(_width, _height, JpegColorSpace.Rgb, rgb);
    }

    private bool ShouldApplyYCbCr()
    {
        // Adobe APP14 transform explicitly signals the color space (0 = RGB, 1 = YCbCr).
        if (_adobeTransform == 0)
            return false;
        if (_adobeTransform == 1)
            return true;

        // No Adobe marker: 'R','G','B' component ids indicate direct RGB; otherwise YCbCr (JFIF).
        if (_components[0].Id == 'R' && _components[1].Id == 'G' && _components[2].Id == 'B')
            return false;
        return true;
    }

    private JpegImage AssembleCmyk(int paddedWidth, int paddedHeight)
    {
        var planes = new byte[4][];
        for (var ch = 0; ch < 4; ch++)
            planes[ch] = UpsampleToFull(_components[ch], paddedWidth, paddedHeight);

        // Adobe stores CMYK and YCCK inverted. For YCCK the color transform is applied to the
        // inverted CMY channels; converting YCbCr back to RGB and taking (255 - value) recovers
        // the original CMY directly. For plain CMYK, an Adobe marker means the channels are
        // inverted and must be undone.
        var isYcck = _adobeTransform == 2;
        var invert = _adobeTransform >= 0; // an Adobe marker was present

        var cmyk = new byte[_width * _height * 4];
        for (var y = 0; y < _height; y++)
        {
            var srcRow = y * paddedWidth;
            var dstRow = y * _width * 4;
            for (var x = 0; x < _width; x++)
            {
                var c0 = planes[0][srcRow + x];
                var c1 = planes[1][srcRow + x];
                var c2 = planes[2][srcRow + x];
                var c3 = planes[3][srcRow + x];

                byte cc, mm, yy;
                if (isYcck)
                {
                    ColorConverter.YCbCrToRgb(c0, c1, c2, out var r, out var g, out var b);
                    cc = (byte)(255 - r);
                    mm = (byte)(255 - g);
                    yy = (byte)(255 - b);
                }
                else if (invert)
                {
                    cc = (byte)(255 - c0);
                    mm = (byte)(255 - c1);
                    yy = (byte)(255 - c2);
                }
                else
                {
                    cc = c0;
                    mm = c1;
                    yy = c2;
                }

                var kk = invert ? (byte)(255 - c3) : c3;

                cmyk[dstRow + x * 4] = cc;
                cmyk[dstRow + x * 4 + 1] = mm;
                cmyk[dstRow + x * 4 + 2] = yy;
                cmyk[dstRow + x * 4 + 3] = kk;
            }
        }

        return new JpegImage(_width, _height, JpegColorSpace.Cmyk, cmyk);
    }

    private byte[] UpsampleToFull(Component c, int fullWidth, int fullHeight)
    {
        if (c.PlaneWidth == fullWidth && c.PlaneHeight == fullHeight)
            return c.Plane;

        // Centered bilinear interpolation gives smoother chroma than replication.
        var full = new byte[fullWidth * fullHeight];
        ChromaSampler.UpsampleLinear(c.Plane, c.PlaneWidth, c.PlaneHeight, full, fullWidth, fullHeight);
        return full;
    }

    // ----- High-precision (9–16 bit) assembly -----

    private JpegImage16 AssembleImage16(int mcusPerRow, int mcusPerCol)
    {
        if (_components.Length == 1)
        {
            var c = _components[0];
            var output = new ushort[_width * _height];
            for (var y = 0; y < _height; y++)
                Array.Copy(c.Plane16, y * c.PlaneWidth, output, y * _width, _width);
            return new JpegImage16(_width, _height, JpegColorSpace.Grayscale, _precision, output);
        }

        var paddedWidth = mcusPerRow * _hmax * 8;
        var paddedHeight = mcusPerCol * _vmax * 8;
        return _components.Length == 3
            ? AssembleThreeComponent16(paddedWidth, paddedHeight)
            : AssembleCmyk16(paddedWidth, paddedHeight);
    }

    private JpegImage16 AssembleCmyk16(int paddedWidth, int paddedHeight)
    {
        var maxValue = (1 << _precision) - 1;
        var planes = new ushort[4][];
        for (var ch = 0; ch < 4; ch++)
            planes[ch] = UpsampleToFull16(_components[ch], paddedWidth, paddedHeight);

        // Same Adobe inversion / YCCK handling as the 8-bit path, scaled to the sample maximum.
        var isYcck = _adobeTransform == 2;
        var invert = _adobeTransform >= 0;

        var cmyk = new ushort[_width * _height * 4];
        for (var y = 0; y < _height; y++)
        {
            var srcRow = y * paddedWidth;
            var dstRow = y * _width * 4;
            for (var x = 0; x < _width; x++)
            {
                var c0 = planes[0][srcRow + x];
                var c1 = planes[1][srcRow + x];
                var c2 = planes[2][srcRow + x];
                var c3 = planes[3][srcRow + x];

                ushort cc, mm, yy;
                if (isYcck)
                {
                    ColorConverter.YCbCrToRgb(c0, c1, c2, maxValue, out var r, out var g, out var b);
                    cc = (ushort)(maxValue - r);
                    mm = (ushort)(maxValue - g);
                    yy = (ushort)(maxValue - b);
                }
                else if (invert)
                {
                    cc = (ushort)(maxValue - c0);
                    mm = (ushort)(maxValue - c1);
                    yy = (ushort)(maxValue - c2);
                }
                else
                {
                    cc = c0;
                    mm = c1;
                    yy = c2;
                }

                var kk = invert ? (ushort)(maxValue - c3) : c3;

                cmyk[dstRow + x * 4] = cc;
                cmyk[dstRow + x * 4 + 1] = mm;
                cmyk[dstRow + x * 4 + 2] = yy;
                cmyk[dstRow + x * 4 + 3] = kk;
            }
        }

        return new JpegImage16(_width, _height, JpegColorSpace.Cmyk, _precision, cmyk);
    }

    private JpegImage16 AssembleThreeComponent16(int paddedWidth, int paddedHeight)
    {
        var maxValue = (1 << _precision) - 1;
        var p0 = UpsampleToFull16(_components[0], paddedWidth, paddedHeight);
        var p1 = UpsampleToFull16(_components[1], paddedWidth, paddedHeight);
        var p2 = UpsampleToFull16(_components[2], paddedWidth, paddedHeight);
        var applyYCbCr = ShouldApplyYCbCr();

        var rgb = new ushort[_width * _height * 3];
        for (var y = 0; y < _height; y++)
        {
            var srcRow = y * paddedWidth;
            var dstRow = y * _width * 3;
            for (var x = 0; x < _width; x++)
            {
                var d = dstRow + x * 3;
                if (applyYCbCr)
                {
                    ColorConverter.YCbCrToRgb(p0[srcRow + x], p1[srcRow + x], p2[srcRow + x], maxValue,
                        out rgb[d], out rgb[d + 1], out rgb[d + 2]);
                }
                else
                {
                    rgb[d] = p0[srcRow + x];
                    rgb[d + 1] = p1[srcRow + x];
                    rgb[d + 2] = p2[srcRow + x];
                }
            }
        }

        return new JpegImage16(_width, _height, JpegColorSpace.Rgb, _precision, rgb);
    }

    private ushort[] UpsampleToFull16(Component c, int fullWidth, int fullHeight)
    {
        if (c.PlaneWidth == fullWidth && c.PlaneHeight == fullHeight)
            return c.Plane16;

        var full = new ushort[fullWidth * fullHeight];
        ChromaSampler.UpsampleLinear(c.Plane16, c.PlaneWidth, c.PlaneHeight, full, fullWidth, fullHeight, (1 << _precision) - 1);
        return full;
    }

    private QuantizationTable GetQuantTable(int id)
    {
        if ((uint)id >= (uint)_quantTables.Length)
            throw new JpegFormatException($"Invalid quantization table id {id}.");
        return _quantTables[id] ?? throw new JpegFormatException($"Missing quantization table {id}.");
    }

    private HuffmanTable GetDcTable(int id)
    {
        if ((uint)id >= (uint)_dcTables.Length)
            throw new JpegFormatException($"Invalid DC Huffman table id {id}.");
        return _dcTables[id] ?? throw new JpegFormatException($"Missing DC Huffman table {id}.");
    }

    private HuffmanTable GetAcTable(int id)
    {
        if ((uint)id >= (uint)_acTables.Length)
            throw new JpegFormatException($"Invalid AC Huffman table id {id}.");
        return _acTables[id] ?? throw new JpegFormatException($"Missing AC Huffman table {id}.");
    }

    private static int CeilDiv(int a, int b) => (a + b - 1) / b;
}

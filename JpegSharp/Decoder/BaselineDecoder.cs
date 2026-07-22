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
internal sealed class BaselineDecoder
{
    private sealed class Component
    {
        public int Id;
        public int H;
        public int V;
        public int QuantId;
        public int DcTableId;
        public int AcTableId;
        public byte[] Plane = [];
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
    private readonly List<(int Seq, byte[] Data)> _iccChunks = [];
    private readonly List<string> _comments = [];
    private readonly List<JpegApplicationSegment> _appSegments = [];

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
        using var ms = new MemoryStream(_data, writable: false);
        var reader = new MarkerReader(ms);
        RequireSoi(reader);
        var entropyStart = ParseHeaders(reader, ms);

        if (_precision != 8)
            throw new JpegFormatException($"Unsupported sample precision {_precision}; only 8-bit is supported.");
        if ((long)_width * _height > _options.MaxPixels)
            throw new JpegFormatException($"Image {_width}x{_height} exceeds the configured maximum of {_options.MaxPixels} pixels.");

        var image = _isProgressive
            ? DecodeProgressive(reader, ms, entropyStart)
            : DecodeScan(_data, entropyStart);
        if (_options.ReadMetadata)
            image.Metadata = BuildMetadata();
        return image;
    }

    public JpegInfo ReadInfo()
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
                case JpegMarkers.DefineRestartInterval:
                    if (segment.Length < 2)
                        throw new JpegFormatException("Truncated DRI segment.");
                    _restartInterval = (segment[0] << 8) | segment[1];
                    break;
                case JpegMarkers.App0:
                    if (!ParseJfif(segment))
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.App1:
                    if (!ParseExif(segment))
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.App2:
                    if (!ParseIccChunk(segment))
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.App14:
                    if (!ParseAdobe(segment))
                        PreserveApp(marker, segment);
                    break;
                case JpegMarkers.Comment:
                    _comments.Add(System.Text.Encoding.UTF8.GetString(segment));
                    break;
                default:
                    if (JpegMarkers.IsAppMarker(marker))
                        PreserveApp(marker, segment);
                    break;
            }
        }
    }

    private bool ParseJfif(ReadOnlySpan<byte> segment)
    {
        if (segment.Length < 14 ||
            segment[0] != 'J' || segment[1] != 'F' || segment[2] != 'I' || segment[3] != 'F' || segment[4] != 0)
        {
            return false;
        }

        var unit = (JpegDensityUnit)segment[7];
        var x = (segment[8] << 8) | segment[9];
        var y = (segment[10] << 8) | segment[11];
        _density = new JfifDensity(unit, x, y);
        return true;
    }

    private bool ParseExif(ReadOnlySpan<byte> segment)
    {
        if (segment.Length < 6 ||
            segment[0] != 'E' || segment[1] != 'x' || segment[2] != 'i' || segment[3] != 'f' || segment[4] != 0 || segment[5] != 0)
        {
            return false;
        }

        _exif = segment[6..].ToArray();
        return true;
    }

    private bool ParseIccChunk(ReadOnlySpan<byte> segment)
    {
        ReadOnlySpan<byte> identifier = "ICC_PROFILE\0"u8;
        if (segment.Length < 14 || !segment[..12].SequenceEqual(identifier))
            return false;

        var seq = segment[12];
        _iccChunks.Add((seq, segment[14..].ToArray()));
        return true;
    }

    private void PreserveApp(byte marker, ReadOnlySpan<byte> segment) =>
        _appSegments.Add(new JpegApplicationSegment(marker, segment.ToArray()));

    private JpegMetadata BuildMetadata()
    {
        var metadata = new JpegMetadata
        {
            Density = _density,
            Exif = _exif,
        };

        if (_adobeTransform >= 0)
            metadata.AdobeColorTransform = _adobeTransform;

        if (_iccChunks.Count > 0)
        {
            _iccChunks.Sort(static (a, b) => a.Seq.CompareTo(b.Seq));
            var total = 0;
            foreach (var chunk in _iccChunks)
                total += chunk.Data.Length;
            var icc = new byte[total];
            var offset = 0;
            foreach (var chunk in _iccChunks)
            {
                chunk.Data.CopyTo(icc, offset);
                offset += chunk.Data.Length;
            }

            metadata.IccProfile = icc;
        }

        foreach (var comment in _comments)
            metadata.Comments.Add(comment);

        foreach (var segment in _appSegments)
            metadata.ApplicationSegments.Add(segment);

        return metadata;
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
            if (id >= 4)
                throw new JpegFormatException($"Invalid Huffman table id {id}.");
            if (p + 16 > segment.Length)
                throw new JpegFormatException("Truncated Huffman table counts.");

            var counts = segment.Slice(p, 16);
            p += 16;
            var total = 0;
            for (var i = 0; i < 16; i++)
                total += counts[i];
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

    private bool ParseAdobe(ReadOnlySpan<byte> segment)
    {
        if (segment.Length < 12 ||
            segment[0] != 'A' || segment[1] != 'd' || segment[2] != 'o' || segment[3] != 'b' || segment[4] != 'e')
        {
            return false;
        }

        _adobeTransform = segment[11];
        return true;
    }

    private void ParseFrameHeader(ReadOnlySpan<byte> segment, byte marker)
    {
        if (segment.Length < 6)
            throw new JpegFormatException("Truncated frame header.");

        _precision = segment[0];
        _isProgressive = marker == JpegMarkers.StartOfFrameProgressive;
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
        if (count == 0 || segment.Length < 1 + count * 2 + 3)
            throw new JpegFormatException("Truncated or invalid scan component list.");
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

        var ss = segment[p];
        var se = segment[p + 1];
        var ahAl = segment[p + 2];
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

        foreach (var c in _components)
        {
            c.BlocksWide = _mcusPerRow * c.H;
            c.BlocksHigh = _mcusPerCol * c.V;
            c.PlaneWidth = c.BlocksWide * 8;
            c.PlaneHeight = c.BlocksHigh * 8;
            c.Plane = new byte[c.PlaneWidth * c.PlaneHeight];
        }
    }

    private JpegImage DecodeProgressive(MarkerReader reader, MemoryStream ms, int entropyStart)
    {
        SetupGeometry();
        _coefficients = new short[_components.Length][];
        for (var ci = 0; ci < _components.Length; ci++)
            _coefficients[ci] = new short[_components[ci].BlocksWide * _components[ci].BlocksHigh * 64];

        var scan = _scan;
        var pos = entropyStart;
        while (true)
        {
            pos = DecodeProgressiveScan(scan, pos);
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
                    default:
                        break;
                }

                marker = reader.ReadMarker();
            }

            if (marker == JpegMarkers.EndOfImage)
                break;

            ParseScanHeader(reader.ReadSegment());
            scan = _scan;
            pos = (int)ms.Position;
        }

        ReconstructComponents();
        return AssembleImage(_mcusPerRow, _mcusPerCol);
    }

    private int DecodeProgressiveScan(ScanHeader scan, int pos)
    {
        if (scan.Ss > 63 || scan.Se > 63 || scan.Ss > scan.Se)
            throw new JpegFormatException($"Invalid spectral selection Ss={scan.Ss}, Se={scan.Se}.");
        if (scan.Ss == 0 && scan.Se != 0)
            throw new JpegFormatException("A DC scan (Ss=0) must have Se=0.");

        var reader = new BitReader(_data.AsSpan(pos));
        if (scan.Ss == 0)
        {
            if (scan.Ah == 0)
                DecodeDcFirst(ref reader, scan);
            else
                DecodeDcRefine(ref reader, scan);
        }
        else
        {
            if (scan.Components.Length != 1)
                throw new JpegFormatException("Progressive AC scans must contain exactly one component.");
            if (scan.Ah == 0)
                DecodeAcFirst(ref reader, scan);
            else
                DecodeAcRefine(ref reader, scan);
        }

        return pos + reader.BytePosition;
    }

    private void DecodeDcFirst(ref BitReader reader, ScanHeader scan)
    {
        var predictors = new int[scan.Components.Length];
        var mcuCount = 0;

        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                if (_restartInterval > 0 && mcuCount > 0 && mcuCount % _restartInterval == 0)
                {
                    reader.SkipRestartMarker();
                    Array.Clear(predictors);
                }

                for (var si = 0; si < scan.Components.Length; si++)
                {
                    var ci = scan.Components[si];
                    var c = _components[ci];
                    var dc = GetDcTable(c.DcTableId);
                    var buffer = _coefficients[ci];
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            var blockCol = mx * c.H + bx;
                            var blockRow = my * c.V + by;
                            var offset = (blockRow * c.BlocksWide + blockCol) * 64;

                            var s = dc.DecodeSymbol(ref reader);
                            if (s is < 0 or > 16)
                                throw new JpegFormatException($"Invalid DC magnitude category {s}.");
                            var diff = s == 0 ? 0 : BitReader.Extend(reader.ReadBits(s), s);
                            predictors[si] += diff;
                            buffer[offset] = (short)(predictors[si] << scan.Al);
                        }
                    }
                }

                mcuCount++;
            }
        }
    }

    private void DecodeAcFirst(ref BitReader reader, ScanHeader scan)
    {
        var ci = scan.Components[0];
        var c = _components[ci];
        var ac = GetAcTable(c.AcTableId);
        var buffer = _coefficients[ci];
        var blocksPerLine = CeilDiv(ComponentActualWidth(c), 8);
        var blocksPerCol = CeilDiv(ComponentActualHeight(c), 8);

        var eobRun = 0;
        var blockIndex = 0;

        for (var by = 0; by < blocksPerCol; by++)
        {
            for (var bx = 0; bx < blocksPerLine; bx++)
            {
                if (_restartInterval > 0 && blockIndex > 0 && blockIndex % _restartInterval == 0)
                {
                    reader.SkipRestartMarker();
                    eobRun = 0;
                }

                blockIndex++;

                if (eobRun > 0)
                {
                    eobRun--;
                    continue;
                }

                var offset = (by * c.BlocksWide + bx) * 64;
                var k = scan.Ss;
                while (k <= scan.Se)
                {
                    var rs = ac.DecodeSymbol(ref reader);
                    var r = rs >> 4;
                    var s = rs & 0x0F;
                    if (s == 0)
                    {
                        if (r < 15)
                        {
                            eobRun = 1 << r;
                            if (r > 0)
                                eobRun += reader.ReadBits(r);
                            eobRun--;
                            break;
                        }

                        k += 16; // ZRL
                    }
                    else
                    {
                        k += r;
                        if (k > scan.Se)
                            throw new JpegCorruptException("AC coefficient index out of range in progressive scan.");
                        buffer[offset + k] = (short)(BitReader.Extend(reader.ReadBits(s), s) << scan.Al);
                        k++;
                    }
                }
            }
        }
    }

    private void DecodeDcRefine(ref BitReader reader, ScanHeader scan)
    {
        var p1 = (short)(1 << scan.Al);
        var mcuCount = 0;

        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                if (_restartInterval > 0 && mcuCount > 0 && mcuCount % _restartInterval == 0)
                    reader.SkipRestartMarker();

                for (var si = 0; si < scan.Components.Length; si++)
                {
                    var ci = scan.Components[si];
                    var c = _components[ci];
                    var buffer = _coefficients[ci];
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            var blockCol = mx * c.H + bx;
                            var blockRow = my * c.V + by;
                            var offset = (blockRow * c.BlocksWide + blockCol) * 64;
                            if (reader.ReadBits(1) != 0)
                                buffer[offset] |= p1;
                        }
                    }
                }

                mcuCount++;
            }
        }
    }

    private void DecodeAcRefine(ref BitReader reader, ScanHeader scan)
    {
        var ci = scan.Components[0];
        var c = _components[ci];
        var ac = GetAcTable(c.AcTableId);
        var buffer = _coefficients[ci];
        var blocksPerLine = CeilDiv(ComponentActualWidth(c), 8);
        var blocksPerCol = CeilDiv(ComponentActualHeight(c), 8);

        var p1 = 1 << scan.Al;
        var m1 = -1 << scan.Al;
        var eobRun = 0;
        var blockIndex = 0;

        for (var by = 0; by < blocksPerCol; by++)
        {
            for (var bx = 0; bx < blocksPerLine; bx++)
            {
                if (_restartInterval > 0 && blockIndex > 0 && blockIndex % _restartInterval == 0)
                {
                    reader.SkipRestartMarker();
                    eobRun = 0;
                }

                blockIndex++;
                var offset = (by * c.BlocksWide + bx) * 64;
                var k = scan.Ss;

                if (eobRun == 0)
                {
                    for (; k <= scan.Se; k++)
                    {
                        var rs = ac.DecodeSymbol(ref reader);
                        var r = rs >> 4;
                        var s = rs & 0x0F;

                        if (s != 0)
                        {
                            s = reader.ReadBits(1) != 0 ? p1 : m1; // newly-nonzero coefficient value
                        }
                        else if (r != 15)
                        {
                            eobRun = 1 << r;
                            if (r != 0)
                                eobRun += reader.ReadBits(r);
                            break;
                        }

                        // Advance over r zero-history coefficients, applying correction bits to
                        // nonzero coefficients along the way.
                        for (; k <= scan.Se; k++)
                        {
                            var coef = buffer[offset + k];
                            if (coef != 0)
                            {
                                if (reader.ReadBits(1) != 0 && (coef & p1) == 0)
                                    buffer[offset + k] = (short)(coef + (coef >= 0 ? p1 : m1));
                            }
                            else
                            {
                                if (--r < 0)
                                    break;
                            }
                        }

                        if (s != 0 && k <= scan.Se)
                            buffer[offset + k] = (short)s;
                    }
                }

                if (eobRun > 0)
                {
                    for (; k <= scan.Se; k++)
                    {
                        var coef = buffer[offset + k];
                        if (coef != 0 && reader.ReadBits(1) != 0 && (coef & p1) == 0)
                            buffer[offset + k] = (short)(coef + (coef >= 0 ? p1 : m1));
                    }

                    eobRun--;
                }
            }
        }
    }

    private void ReconstructComponents()
    {
        Span<short> zz = stackalloc short[64];
        Span<short> natural = stackalloc short[64];
        Span<double> dequant = stackalloc double[64];
        Span<double> spatial = stackalloc double[64];

        for (var ci = 0; ci < _components.Length; ci++)
        {
            var c = _components[ci];
            var quant = GetQuantTable(c.QuantId).AsSpan();
            var buffer = _coefficients[ci];
            for (var by = 0; by < c.BlocksHigh; by++)
            {
                for (var bx = 0; bx < c.BlocksWide; bx++)
                {
                    var offset = (by * c.BlocksWide + bx) * 64;
                    buffer.AsSpan(offset, 64).CopyTo(zz);
                    ZigZag.ToNatural(zz, natural);
                    Quantizer.Dequantize(natural, quant, dequant);
                    FastDct.Inverse(dequant, spatial);
                    StoreBlock(c, bx * 8, by * 8, spatial);
                }
            }
        }
    }

    private int ComponentActualWidth(Component c) => (int)(((long)_width * c.H + _hmax - 1) / _hmax);

    private int ComponentActualHeight(Component c) => (int)(((long)_height * c.V + _vmax - 1) / _vmax);

    private JpegImage DecodeScan(byte[] data, int entropyStart)
    {
        SetupGeometry();
        var mcusPerRow = _mcusPerRow;
        var mcusPerCol = _mcusPerCol;

        var reader = new BitReader(data.AsSpan(entropyStart));
        var predictors = new int[_components.Length];
        Span<short> zz = stackalloc short[64];
        Span<short> natural = stackalloc short[64];
        Span<double> dequant = stackalloc double[64];
        Span<double> spatial = stackalloc double[64];

        var mcuCount = 0;

        for (var my = 0; my < mcusPerCol; my++)
        {
            for (var mx = 0; mx < mcusPerRow; mx++)
            {
                if (_restartInterval > 0 && mcuCount > 0 && mcuCount % _restartInterval == 0)
                {
                    reader.SkipRestartMarker();
                    Array.Clear(predictors);
                }

                // Interleaved MCUs follow the scan's component order (which may differ from the
                // frame's), per ITU-T T.81 B.2.3.
                foreach (var ci in _scan.Components)
                {
                    var c = _components[ci];
                    var quant = GetQuantTable(c.QuantId).AsSpan();
                    var dc = GetDcTable(c.DcTableId);
                    var ac = GetAcTable(c.AcTableId);
                    for (var by = 0; by < c.V; by++)
                    {
                        for (var bx = 0; bx < c.H; bx++)
                        {
                            predictors[ci] = BlockScanCoder.DecodeBlock(ref reader, zz, predictors[ci], dc, ac);
                            ZigZag.ToNatural(zz, natural);
                            Quantizer.Dequantize(natural, quant, dequant);
                            FastDct.Inverse(dequant, spatial);
                            StoreBlock(c, (mx * c.H + bx) * 8, (my * c.V + by) * 8, spatial);
                        }
                    }
                }

                mcuCount++;
            }
        }

        return AssembleImage(mcusPerRow, mcusPerCol);
    }

    private static void StoreBlock(Component c, int x0, int y0, ReadOnlySpan<double> spatial)
    {
        for (var yy = 0; yy < 8; yy++)
        {
            var row = (y0 + yy) * c.PlaneWidth + x0;
            for (var xx = 0; xx < 8; xx++)
            {
                var value = (int)Math.Round(spatial[yy * 8 + xx]) + 128;
                c.Plane[row + xx] = (byte)Math.Clamp(value, 0, 255);
            }
        }
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
            for (var x = 0; x < _width; x++)
            {
                var d = dstRow + x * 3;
                if (applyYCbCr)
                {
                    ColorConverter.YCbCrToRgb(p0[srcRow + x], p1[srcRow + x], p2[srcRow + x],
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

using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Markers;
using JpegSharp.Quantization;
using JpegSharp.Transforms;

namespace JpegSharp.Decoder;

// Progressive (SOF2) decoding: the multi-scan loop plus the DC/AC first and
// successive-approximation refinement passes, and coefficient-buffer reconstruction.
internal sealed partial class BaselineDecoder
{
    private void DecodeProgressive(MarkerReader reader, MemoryStream ms, int entropyStart)
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

        ReconstructComponents();
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
        if (scan.Components.Length == 1)
        {
            DecodeDcFirstNonInterleaved(ref reader, scan);
            return;
        }

        var predictors = new int[scan.Components.Length];
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
                            if (s is < 0 or > 15)
                                throw new JpegCorruptException($"Invalid DC magnitude category {s}.");
                            var diff = s == 0 ? 0 : BitReader.Extend(reader.ReadBits(s), s);
                            predictors[si] += diff;
                            var value = predictors[si] << scan.Al;
                            if (value is < short.MinValue or > short.MaxValue)
                                throw new JpegCorruptException("DC predictor out of range in progressive scan.");
                            buffer[offset] = (short)value;
                        }
                    }
                }

                mcuCount++;
            }
        }
    }

    private void DecodeDcFirstNonInterleaved(ref BitReader reader, ScanHeader scan)
    {
        var ci = scan.Components[0];
        var c = _components[ci];
        var dc = GetDcTable(c.DcTableId);
        var buffer = _coefficients[ci];
        var blocksPerLine = CeilDiv(ComponentActualWidth(c), 8);
        var blocksPerCol = CeilDiv(ComponentActualHeight(c), 8);

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

                var offset = (by * c.BlocksWide + bx) * 64;
                var s = dc.DecodeSymbol(ref reader);
                if (s is < 0 or > 15)
                    throw new JpegCorruptException($"Invalid DC magnitude category {s}.");
                var diff = s == 0 ? 0 : BitReader.Extend(reader.ReadBits(s), s);
                predictor += diff;
                var value = predictor << scan.Al;
                if (value is < short.MinValue or > short.MaxValue)
                    throw new JpegCorruptException("DC predictor out of range in progressive scan.");
                buffer[offset] = (short)value;
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
                    var expectedRst = (blockIndex / _restartInterval - 1) & 7;
                    reader.SkipRestartMarker(expectedRst, _options.StrictRestartMarkers);
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
                        if (k > scan.Se + 1)
                            throw new JpegCorruptException("AC zero-run overruns the spectral band in progressive scan.");
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
        if (scan.Components.Length == 1)
        {
            DecodeDcRefineNonInterleaved(ref reader, scan);
            return;
        }

        var p1 = (short)(1 << scan.Al);
        var mcuCount = 0;

        for (var my = 0; my < _mcusPerCol; my++)
        {
            for (var mx = 0; mx < _mcusPerRow; mx++)
            {
                if (_restartInterval > 0 && mcuCount > 0 && mcuCount % _restartInterval == 0)
                {
                    var expectedRst = (mcuCount / _restartInterval - 1) & 7;
                    reader.SkipRestartMarker(expectedRst, _options.StrictRestartMarkers);
                }

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

    private void DecodeDcRefineNonInterleaved(ref BitReader reader, ScanHeader scan)
    {
        var ci = scan.Components[0];
        var c = _components[ci];
        var buffer = _coefficients[ci];
        var blocksPerLine = CeilDiv(ComponentActualWidth(c), 8);
        var blocksPerCol = CeilDiv(ComponentActualHeight(c), 8);

        var p1 = (short)(1 << scan.Al);
        var blockIndex = 0;

        for (var by = 0; by < blocksPerCol; by++)
        {
            for (var bx = 0; bx < blocksPerLine; bx++)
            {
                if (_restartInterval > 0 && blockIndex > 0 && blockIndex % _restartInterval == 0)
                {
                    var expectedRst = (blockIndex / _restartInterval - 1) & 7;
                    reader.SkipRestartMarker(expectedRst, _options.StrictRestartMarkers);
                }

                blockIndex++;

                var offset = (by * c.BlocksWide + bx) * 64;
                if (reader.ReadBits(1) != 0)
                    buffer[offset] |= p1;
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
                    var expectedRst = (blockIndex / _restartInterval - 1) & 7;
                    reader.SkipRestartMarker(expectedRst, _options.StrictRestartMarkers);
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
                            if (s > 1)
                                throw new JpegCorruptException("Invalid AC refinement magnitude in progressive scan.");
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

                        if (s != 0)
                        {
                            if (k > scan.Se)
                                throw new JpegCorruptException("AC refinement coefficient cannot be placed within the spectral band.");
                            buffer[offset + k] = (short)s;
                        }
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
        for (var ci = 0; ci < _components.Length; ci++)
        {
            var c = _components[ci];
            var quant = GetQuantTable(c.QuantId).AsZigZagSpan();
            var buffer = _coefficients[ci];
            for (var by = 0; by < c.BlocksHigh; by++)
            {
                for (var bx = 0; bx < c.BlocksWide; bx++)
                {
                    var offset = (by * c.BlocksWide + bx) * 64;
                    ReconstructBlock(c, bx * 8, by * 8, buffer.AsSpan(offset, 64), quant);
                }
            }
        }
    }

    private int ComponentActualWidth(Component c) => (int)(((long)_width * c.H + _hmax - 1) / _hmax);

    private int ComponentActualHeight(Component c) => (int)(((long)_height * c.V + _vmax - 1) / _vmax);
}

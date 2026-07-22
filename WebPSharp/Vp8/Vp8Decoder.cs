using WebPSharp.Api.Exceptions;

namespace WebPSharp.Vp8;

/// <summary>
/// Decodes a VP8 (lossy) key-frame bitstream. This type parses the frame and picture headers, the
/// segmentation / loop-filter / quantization / entropy headers, and the DCT partition layout,
/// establishing the per-segment dequantization matrices and coefficient probabilities used by the
/// macroblock decode loop. The bitstream layout follows RFC 6386 / the libwebp reference decoder.
/// </summary>
internal sealed class Vp8Decoder
{
    private const int NumMbSegments = 4;
    private const int MbFeatureTreeProbs = 3;
    private const int NumRefLfDeltas = 4;
    private const int NumModeLfDeltas = 4;

    private readonly byte[] _payload;

    internal Vp8Decoder(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        _payload = payload;
    }

    // ---- Frame / picture header ----
    internal bool KeyFrame { get; private set; }
    internal int Profile { get; private set; }
    internal int Width { get; private set; }
    internal int Height { get; private set; }
    internal int MbWidth { get; private set; }
    internal int MbHeight { get; private set; }

    // ---- Segmentation ----
    internal bool UseSegment { get; private set; }
    internal bool UpdateMap { get; private set; }
    internal bool AbsoluteDelta { get; private set; }
    internal readonly int[] SegmentQuantizer = new int[NumMbSegments];
    internal readonly int[] SegmentFilterStrength = new int[NumMbSegments];
    internal readonly byte[] SegmentProbas = { 255, 255, 255 };

    // ---- Loop filter ----
    internal bool FilterSimple { get; private set; }
    internal int FilterLevel { get; private set; }
    internal int FilterSharpness { get; private set; }
    internal bool UseLfDelta { get; private set; }
    internal readonly int[] RefLfDelta = new int[NumRefLfDeltas];
    internal readonly int[] ModeLfDelta = new int[NumModeLfDeltas];
    internal int FilterType { get; private set; } // 0 = none, 1 = simple, 2 = normal

    // ---- Partitions ----
    internal int NumParts { get; private set; }
    internal Vp8BooleanDecoder FirstPartition { get; private set; } = null!;
    internal Vp8BooleanDecoder[] Partitions { get; private set; } = System.Array.Empty<Vp8BooleanDecoder>();

    // ---- Quantization ----
    internal readonly Vp8QuantMatrix[] Dequant = new Vp8QuantMatrix[NumMbSegments];

    // ---- Entropy ----
    internal readonly byte[] CoeffProbs = new byte[4 * 8 * 3 * 11];
    internal bool UseSkipProba { get; private set; }
    internal byte SkipProba { get; private set; }

    /// <summary>Parses all VP8 headers, leaving the decoder ready for macroblock decoding.</summary>
    /// <exception cref="WebPFormatException">The frame is malformed or unsupported.</exception>
    internal void ParseHeaders()
    {
        var data = _payload;
        if (data.Length < 10)
            throw new WebPFormatException($"VP8 frame is truncated: {data.Length} bytes.");

        // Paragraph 9.1: 3-byte frame tag.
        var bits = data[0] | (data[1] << 8) | (data[2] << 16);
        KeyFrame = (bits & 1) == 0;
        Profile = (bits >> 1) & 7;
        var show = (bits >> 4) & 1;
        var partitionLength = bits >> 5;
        if (Profile > 3)
            throw new WebPFormatException($"Unsupported VP8 profile {Profile}.");
        if (!KeyFrame)
            throw new WebPException("VP8 inter frames are not supported (still images are key frames).");
        if (show == 0)
            throw new WebPFormatException("VP8 frame is not displayable.");

        // Paragraph 9.2: 7-byte key-frame picture header (uncompressed).
        if (data[3] != 0x9D || data[4] != 0x01 || data[5] != 0x2A)
            throw new WebPFormatException("VP8 key frame is missing its 0x9D 0x01 0x2A start code.");
        Width = ((data[7] << 8) | data[6]) & 0x3FFF;
        Height = ((data[9] << 8) | data[8]) & 0x3FFF;
        if (Width == 0 || Height == 0)
            throw new WebPFormatException($"VP8 header declares a zero dimension ({Width}x{Height}).");
        MbWidth = (Width + 15) >> 4;
        MbHeight = (Height + 15) >> 4;

        const int pictureHeaderEnd = 10; // 3 (frame tag) + 7 (picture header)
        if (pictureHeaderEnd + partitionLength > data.Length)
            throw new WebPFormatException("VP8 first partition overruns the frame.");

        var br = new Vp8BooleanDecoder(data, pictureHeaderEnd, partitionLength);
        FirstPartition = br;

        // Compressed header (key-frame): color space + clamping type.
        _ = br.GetBit(128); // color space
        _ = br.GetBit(128); // clamping type

        ParseSegmentHeader(br);
        ParseFilterHeader(br);

        var dctRegionOffset = pictureHeaderEnd + partitionLength;
        ParsePartitions(br, dctRegionOffset, data.Length - dctRegionOffset);

        ParseQuant(br);

        _ = br.GetBit(128); // 'update_proba' flag (value ignored for key frames)
        ParseProba(br);
    }

    private void ParseSegmentHeader(Vp8BooleanDecoder br)
    {
        UseSegment = br.GetBit(128) != 0;
        if (!UseSegment)
        {
            UpdateMap = false;
            return;
        }

        UpdateMap = br.GetBit(128) != 0;
        if (br.GetBit(128) != 0) // update data
        {
            AbsoluteDelta = br.GetBit(128) != 0;
            for (var s = 0; s < NumMbSegments; s++)
                SegmentQuantizer[s] = br.GetBit(128) != 0 ? br.GetSigned(7) : 0;
            for (var s = 0; s < NumMbSegments; s++)
                SegmentFilterStrength[s] = br.GetBit(128) != 0 ? br.GetSigned(6) : 0;
        }

        if (UpdateMap)
        {
            for (var s = 0; s < MbFeatureTreeProbs; s++)
                SegmentProbas[s] = br.GetBit(128) != 0 ? (byte)br.GetLiteral(8) : (byte)255;
        }
    }

    private void ParseFilterHeader(Vp8BooleanDecoder br)
    {
        FilterSimple = br.GetBit(128) != 0;
        FilterLevel = (int)br.GetLiteral(6);
        FilterSharpness = (int)br.GetLiteral(3);
        UseLfDelta = br.GetBit(128) != 0;
        if (UseLfDelta && br.GetBit(128) != 0) // update lf-delta
        {
            for (var i = 0; i < NumRefLfDeltas; i++)
                if (br.GetBit(128) != 0)
                    RefLfDelta[i] = br.GetSigned(6);
            for (var i = 0; i < NumModeLfDeltas; i++)
                if (br.GetBit(128) != 0)
                    ModeLfDelta[i] = br.GetSigned(6);
        }

        FilterType = FilterLevel == 0 ? 0 : FilterSimple ? 1 : 2;
    }

    private void ParsePartitions(Vp8BooleanDecoder br, int offset, int size)
    {
        NumParts = 1 << (int)br.GetLiteral(2);
        var lastPart = NumParts - 1;
        if (size < 3 * lastPart)
            throw new WebPFormatException("VP8 partition size table is truncated.");

        Partitions = new Vp8BooleanDecoder[NumParts];
        var partStart = offset + lastPart * 3;
        var sizeLeft = size - lastPart * 3;
        var szPos = offset;
        for (var p = 0; p < lastPart; p++)
        {
            var psize = _payload[szPos] | (_payload[szPos + 1] << 8) | (_payload[szPos + 2] << 16);
            if (psize > sizeLeft)
                psize = sizeLeft;
            Partitions[p] = new Vp8BooleanDecoder(_payload, partStart, psize);
            partStart += psize;
            sizeLeft -= psize;
            szPos += 3;
        }
        Partitions[lastPart] = new Vp8BooleanDecoder(_payload, partStart, sizeLeft);
    }

    private void ParseQuant(Vp8BooleanDecoder br)
    {
        var baseQ0 = (int)br.GetLiteral(7);
        var dqy1Dc = br.GetOptionalSigned(4);
        var dqy2Dc = br.GetOptionalSigned(4);
        var dqy2Ac = br.GetOptionalSigned(4);
        var dquvDc = br.GetOptionalSigned(4);
        var dquvAc = br.GetOptionalSigned(4);

        for (var i = 0; i < NumMbSegments; i++)
        {
            int q;
            if (UseSegment)
            {
                q = SegmentQuantizer[i];
                if (!AbsoluteDelta)
                    q += baseQ0;
            }
            else
            {
                if (i > 0)
                {
                    Dequant[i] = Dequant[0];
                    continue;
                }
                q = baseQ0;
            }

            var m = new Vp8QuantMatrix
            {
                Y1Dc = Vp8Tables.DcTable[Clip(q + dqy1Dc, 127)],
                Y1Ac = Vp8Tables.AcTable[Clip(q, 127)],
                Y2Dc = Vp8Tables.DcTable[Clip(q + dqy2Dc, 127)] * 2,
                Y2Ac = Math.Max(8, (Vp8Tables.AcTable[Clip(q + dqy2Ac, 127)] * 101581) >> 16),
                UvDc = Vp8Tables.DcTable[Clip(q + dquvDc, 117)],
                UvAc = Vp8Tables.AcTable[Clip(q + dquvAc, 127)],
            };
            Dequant[i] = m;
        }
    }

    private void ParseProba(Vp8BooleanDecoder br)
    {
        var update = Vp8Tables.CoeffUpdateProbs;
        var defaults = Vp8Tables.DefaultCoeffProbs;
        for (var i = 0; i < CoeffProbs.Length; i++)
        {
            CoeffProbs[i] = br.GetBit(update[i]) != 0
                ? (byte)br.GetLiteral(8)
                : defaults[i];
        }

        UseSkipProba = br.GetBit(128) != 0;
        SkipProba = UseSkipProba ? (byte)br.GetLiteral(8) : (byte)0;
    }

    private static int Clip(int value, int max) => value < 0 ? 0 : value > max ? max : value;

    // 16x16 / chroma intra modes. These are aliased onto the 4x4 B-mode enumeration (DC=B_DC,
    // TM=B_TM, V=B_VE, H=B_HE) so that a 16x16 macroblock's mode can serve as the neighbor context
    // for an adjacent 4x4 macroblock's B-mode probabilities.
    internal const int DcPred = 0, TmPred = 1, VPred = 2, HPred = 3;
    // 4x4 (B_PRED) intra modes, in the libwebp enumeration order used by the probability tables.
    internal const int BDc = 0, BTm = 1, BVe = 2, BHe = 3, BRd = 4, BVr = 5, BLd = 6, BVl = 7, BHd = 8, BHu = 9;

    // ---- Per-macroblock decode state ----
    internal int[] MbSegment { get; private set; } = System.Array.Empty<int>();
    internal bool[] MbIsI4x4 { get; private set; } = System.Array.Empty<bool>();
    internal bool[] MbSkip { get; private set; } = System.Array.Empty<bool>();
    internal int[] MbUvMode { get; private set; } = System.Array.Empty<int>();
    internal byte[] MbModes { get; private set; } = System.Array.Empty<byte>();   // mbW*mbH*16
    internal short[] MbCoeffs { get; private set; } = System.Array.Empty<short>(); // mbW*mbH*384
    internal uint[] MbNonZeroY { get; private set; } = System.Array.Empty<uint>();
    internal uint[] MbNonZeroUv { get; private set; } = System.Array.Empty<uint>();

    private Vp8Mb[] _topMb = System.Array.Empty<Vp8Mb>();
    private Vp8Mb _leftMb;
    private byte[] _intraTop = System.Array.Empty<byte>(); // 4 * mbW top 4x4 modes
    private readonly byte[] _intraLeft = new byte[4];

    /// <summary>Decodes every macroblock's modes and dequantized coefficients.</summary>
    internal void DecodeMacroblocks()
    {
        var count = MbWidth * MbHeight;
        MbSegment = new int[count];
        MbIsI4x4 = new bool[count];
        MbSkip = new bool[count];
        MbUvMode = new int[count];
        MbModes = new byte[count * 16];
        MbCoeffs = new short[count * 384];
        MbNonZeroY = new uint[count];
        MbNonZeroUv = new uint[count];

        _topMb = new Vp8Mb[MbWidth];
        _intraTop = new byte[4 * MbWidth];
        System.Array.Fill(_intraTop, (byte)BDc);

        for (var mbY = 0; mbY < MbHeight; mbY++)
        {
            // New scanline: reset left contexts.
            _leftMb = default;
            System.Array.Fill(_intraLeft, (byte)BDc);

            var tokenBr = Partitions[mbY & (NumParts - 1)];

            for (var mbX = 0; mbX < MbWidth; mbX++)
            {
                var mb = mbY * MbWidth + mbX;
                ParseIntraMode(FirstPartition, mbX, mb);
                var skip = UseSkipProba && ParseSkip(FirstPartition);
                MbSkip[mb] = skip;
                ParseResiduals(tokenBr, mbX, mb, skip);
            }
        }
    }

    private bool ParseSkip(Vp8BooleanDecoder br) => br.GetBit(SkipProba) != 0;

    private void ParseIntraMode(Vp8BooleanDecoder br, int mbX, int mb)
    {
        var topOff = 4 * mbX;

        MbSegment[mb] = UpdateMap
            ? (br.GetBit(SegmentProbas[0]) == 0
                ? br.GetBit(SegmentProbas[1])
                : br.GetBit(SegmentProbas[2]) + 2)
            : 0;

        // Note: the skip flag is read by the caller (ParseSkip), matching the bitstream order.
        var isI4x4 = br.GetBit(145) == 0;
        MbIsI4x4[mb] = isI4x4;

        if (!isI4x4)
        {
            var ymode = br.GetBit(156) != 0
                ? (br.GetBit(128) != 0 ? TmPred : HPred)
                : (br.GetBit(163) != 0 ? VPred : DcPred);
            MbModes[mb * 16] = (byte)ymode;
            for (var i = 0; i < 4; i++)
            {
                _intraTop[topOff + i] = (byte)ymode;
                _intraLeft[i] = (byte)ymode;
            }
        }
        else
        {
            for (var y = 0; y < 4; y++)
            {
                var left = _intraLeft[y];
                for (var x = 0; x < 4; x++)
                {
                    var probBase = Vp8Tables.BModeIndex(_intraTop[topOff + x], left);
                    left = (byte)DecodeBMode(br, probBase);
                    _intraTop[topOff + x] = left;
                    MbModes[mb * 16 + y * 4 + x] = left;
                }
                _intraLeft[y] = left;
            }
        }

        MbUvMode[mb] = br.GetBit(142) == 0 ? DcPred
            : br.GetBit(114) == 0 ? VPred
            : br.GetBit(183) != 0 ? TmPred
            : HPred;
    }

    private static int DecodeBMode(Vp8BooleanDecoder br, int probBase)
    {
        var p = Vp8Tables.BModeProbs;
        if (br.GetBit(p[probBase + 0]) == 0) return BDc;
        if (br.GetBit(p[probBase + 1]) == 0) return BTm;
        if (br.GetBit(p[probBase + 2]) == 0) return BVe;
        if (br.GetBit(p[probBase + 3]) == 0)
            return br.GetBit(p[probBase + 4]) == 0 ? BHe
                : br.GetBit(p[probBase + 5]) == 0 ? BRd : BVr;
        return br.GetBit(p[probBase + 6]) == 0 ? BLd
            : br.GetBit(p[probBase + 7]) == 0 ? BVl
            : br.GetBit(p[probBase + 8]) == 0 ? BHd : BHu;
    }

    private static uint NzCodeBits(uint nzCoeffs, int nz, int dcNz)
    {
        nzCoeffs <<= 2;
        nzCoeffs |= (uint)(nz > 3 ? 3 : nz > 1 ? 2 : dcNz);
        return nzCoeffs;
    }

    private void ParseResiduals(Vp8BooleanDecoder br, int mbX, int mb, bool skip)
    {
        var coeffs = MbCoeffs;
        var baseOff = mb * 384;
        System.Array.Clear(coeffs, baseOff, 384);

        ref var top = ref _topMb[mbX];

        if (skip)
        {
            top.Nz = 0;
            _leftMb.Nz = 0;
            if (!MbIsI4x4[mb])
            {
                top.NzDc = 0;
                _leftMb.NzDc = 0;
            }
            MbNonZeroY[mb] = 0;
            MbNonZeroUv[mb] = 0;
            return;
        }

        var q = Dequant[MbSegment[mb]];
        uint nonZeroY = 0;
        uint nonZeroUv = 0;
        int first;
        int acType;

        if (!MbIsI4x4[mb])
        {
            // Second-order (Y2) block via WHT, stored temporarily then scattered to DC positions.
            Span<short> dc = stackalloc short[16];
            dc.Clear();
            var ctx = top.NzDc + _leftMb.NzDc;
            var y2 = new short[16];
            var nz = Vp8Coefficients.GetCoeffs(br, CoeffProbs, 1, ctx, q.Y2Dc, q.Y2Ac, 0, y2, 0);
            top.NzDc = _leftMb.NzDc = nz > 0 ? 1 : 0;

            if (nz > 1)
            {
                var wht = new short[16];
                Vp8Transform.InverseWht(y2, wht);
                for (var i = 0; i < 16; i++)
                    coeffs[baseOff + i * 16] = wht[i];
            }
            else
            {
                var dc0 = (short)((y2[0] + 3) >> 3);
                for (var i = 0; i < 16; i++)
                    coeffs[baseOff + i * 16] = dc0;
            }
            first = 1;
            acType = 0;
        }
        else
        {
            first = 0;
            acType = 3;
        }

        var tnz = top.Nz & 0x0Fu;
        var lnz = _leftMb.Nz & 0x0Fu;
        for (var y = 0; y < 4; y++)
        {
            var l = lnz & 1;
            uint nzCoeffs = 0;
            for (var x = 0; x < 4; x++)
            {
                var ctx = (int)(l + (tnz & 1));
                var blockOff = baseOff + (y * 4 + x) * 16;
                var nz = Vp8Coefficients.GetCoeffs(br, CoeffProbs, acType, ctx, q.Y1Dc, q.Y1Ac, first, coeffs, blockOff);
                l = nz > first ? 1u : 0u;
                tnz = (tnz >> 1) | (l << 7);
                nzCoeffs = NzCodeBits(nzCoeffs, nz, coeffs[blockOff] != 0 ? 1 : 0);
            }
            tnz >>= 4;
            lnz = (lnz >> 1) | (l << 7);
            nonZeroY = (nonZeroY << 8) | nzCoeffs;
        }
        var outTnz = tnz;
        var outLnz = lnz >> 4;

        for (var ch = 0; ch < 4; ch += 2)
        {
            uint nzCoeffs = 0;
            tnz = top.Nz >> (4 + ch);
            lnz = _leftMb.Nz >> (4 + ch);
            for (var y = 0; y < 2; y++)
            {
                var l = lnz & 1;
                for (var x = 0; x < 2; x++)
                {
                    var ctx = (int)(l + (tnz & 1));
                    var blockOff = baseOff + (16 + ch * 2 + y * 2 + x) * 16;
                    var nz = Vp8Coefficients.GetCoeffs(br, CoeffProbs, 2, ctx, q.UvDc, q.UvAc, 0, coeffs, blockOff);
                    l = nz > 0 ? 1u : 0u;
                    tnz = (tnz >> 1) | (l << 3);
                    nzCoeffs = NzCodeBits(nzCoeffs, nz, coeffs[blockOff] != 0 ? 1 : 0);
                }
                tnz >>= 2;
                lnz = (lnz >> 1) | (l << 5);
            }
            nonZeroUv |= nzCoeffs << (4 * ch);
            outTnz |= (tnz << 4) << ch;
            outLnz |= (lnz & 0xF0u) << ch;
        }

        top.Nz = outTnz;
        _leftMb.Nz = outLnz;
        MbNonZeroY[mb] = nonZeroY;
        MbNonZeroUv[mb] = nonZeroUv;
    }

    // ---- Reconstruction ----

    // Maps the libwebp 4x4 B-mode enumeration to the Vp8Prediction4 (RFC-ordered) mode.
    private static readonly int[] BModeToPred =
    {
        Vp8Prediction4.Dc, Vp8Prediction4.TrueMotion, Vp8Prediction4.Vertical, Vp8Prediction4.Horizontal,
        Vp8Prediction4.DownRight, Vp8Prediction4.VerticalRight, Vp8Prediction4.DownLeft,
        Vp8Prediction4.VerticalLeft, Vp8Prediction4.HorizontalDown, Vp8Prediction4.HorizontalUp,
    };

    internal byte[] PlaneY { get; private set; } = System.Array.Empty<byte>();
    internal byte[] PlaneU { get; private set; } = System.Array.Empty<byte>();
    internal byte[] PlaneV { get; private set; } = System.Array.Empty<byte>();
    internal int LumaStride => MbWidth * 16;
    internal int ChromaStride => MbWidth * 8;

    /// <summary>Reconstructs the full (padded) Y/U/V planes from the decoded modes and coefficients.</summary>
    internal void Reconstruct()
    {
        var w = MbWidth * 16;
        var h = MbHeight * 16;
        var cw = MbWidth * 8;
        var chh = MbHeight * 8;
        PlaneY = new byte[w * h];
        PlaneU = new byte[cw * chh];
        PlaneV = new byte[cw * chh];

        Span<int> residual = stackalloc int[16];
        Span<byte> top = stackalloc byte[16];
        Span<byte> left = stackalloc byte[16];
        Span<byte> top8 = stackalloc byte[8];

        for (var mbY = 0; mbY < MbHeight; mbY++)
        for (var mbX = 0; mbX < MbWidth; mbX++)
        {
            var mb = mbY * MbWidth + mbX;
            var coeffOff = mb * 384;

            // ---- Luma ----
            if (MbIsI4x4[mb])
            {
                for (var by = 0; by < 4; by++)
                for (var bx = 0; bx < 4; bx++)
                {
                    var n = by * 4 + bx;
                    var px = mbX * 16 + bx * 4;
                    var py = mbY * 16 + by * 4;
                    GatherLuma4Top(top8, px, py, bx, mbX);
                    for (var i = 0; i < 4; i++)
                        left[i] = SampleY(py + i, px - 1);
                    var corner = SampleY(py - 1, px - 1);

                    Vp8Prediction4.Predict(BModeToPred[MbModes[mb * 16 + n]],
                        PlaneY.AsSpan(py * w + px), w, top8, left.Slice(0, 4), corner);
                    Vp8Transform.InverseDct(MbCoeffs.AsSpan(coeffOff + n * 16, 16), residual);
                    Vp8Transform.AddResidual(PlaneY.AsSpan(py * w + px), w, residual);
                }
            }
            else
            {
                var mbPx = mbX * 16;
                var mbPy = mbY * 16;
                for (var i = 0; i < 16; i++)
                {
                    top[i] = SampleY(mbPy - 1, mbPx + i);
                    left[i] = SampleY(mbPy + i, mbPx - 1);
                }
                PredictFull(MbModes[mb * 16], PlaneY.AsSpan(mbPy * w + mbPx), w, 16, top, left,
                    SampleY(mbPy - 1, mbPx - 1), mbX > 0, mbY > 0);

                for (var n = 0; n < 16; n++)
                {
                    var bx = n & 3;
                    var by = n >> 2;
                    var off = (mbPy + by * 4) * w + mbPx + bx * 4;
                    Vp8Transform.InverseDct(MbCoeffs.AsSpan(coeffOff + n * 16, 16), residual);
                    Vp8Transform.AddResidual(PlaneY.AsSpan(off), w, residual);
                }
            }

            // ---- Chroma ----
            ReconstructChroma(PlaneU, cw, mbX, mbY, MbUvMode[mb], MbCoeffs, coeffOff + 16 * 16, residual, top8, left);
            ReconstructChroma(PlaneV, cw, mbX, mbY, MbUvMode[mb], MbCoeffs, coeffOff + 20 * 16, residual, top8, left);
        }
    }

    private void ReconstructChroma(byte[] plane, int stride, int mbX, int mbY, int mode, short[] coeffs,
        int coeffOff, Span<int> residual, Span<byte> top, Span<byte> left)
    {
        var px = mbX * 8;
        var py = mbY * 8;
        for (var i = 0; i < 8; i++)
        {
            top[i] = SampleC(plane, stride, py - 1, px + i);
            left[i] = SampleC(plane, stride, py + i, px - 1);
        }
        var corner = mbY == 0 ? (byte)127 : mbX == 0 ? (byte)129 : plane[(py - 1) * stride + px - 1];
        PredictFull(mode, plane.AsSpan(py * stride + px), stride, 8, top, left, corner, mbX > 0, mbY > 0);

        for (var n = 0; n < 4; n++)
        {
            var bx = n & 1;
            var by = n >> 1;
            var off = (py + by * 4) * stride + px + bx * 4;
            Vp8Transform.InverseDct(coeffs.AsSpan(coeffOff + n * 16, 16), residual);
            Vp8Transform.AddResidual(plane.AsSpan(off), stride, residual);
        }
    }

    // Full-size (16x16 luma / 8x8 chroma) intra prediction from gathered border samples.
    private static void PredictFull(int mode, Span<byte> dst, int stride, int size,
        ReadOnlySpan<byte> top, ReadOnlySpan<byte> left, byte corner, bool hasLeft, bool hasTop)
    {
        switch (mode)
        {
            case DcPred: Vp8Prediction.FillDc(dst, stride, size, top, left, hasTop, hasLeft); break;
            case VPred: Vp8Prediction.FillVertical(dst, stride, size, top); break;
            case HPred: Vp8Prediction.FillHorizontal(dst, stride, size, left); break;
            default: Vp8Prediction.FillTrueMotion(dst, stride, size, top, left, corner); break;
        }
    }

    private byte SampleY(int row, int col)
    {
        if (row < 0) return 127;
        if (col < 0) return 129;
        return PlaneY[row * LumaStride + col];
    }

    private static byte SampleC(byte[] plane, int stride, int row, int col)
    {
        if (row < 0) return 127;
        if (col < 0) return 129;
        return plane[row * stride + col];
    }

    // Gathers the 8 top samples (4 above + 4 above-right) for a 4x4 luma block, applying the VP8
    // rule that the rightmost 4x4 column uses the macroblock's top-right border (replicated down).
    private void GatherLuma4Top(Span<byte> top, int px, int py, int bx, int mbX)
    {
        for (var i = 0; i < 4; i++)
            top[i] = SampleY(py - 1, px + i);

        int trRow, trCol;
        if (bx == 3)
        {
            // Rightmost 4x4 column: top-right comes from the macroblock's top border, replicated down.
            trRow = (py & ~15) - 1;
            trCol = mbX * 16 + 16;
        }
        else
        {
            trRow = py - 1;
            trCol = px + 4;
        }

        var w = LumaStride;
        for (var i = 0; i < 4; i++)
        {
            var col = trCol + i;
            if (trRow < 0) top[4 + i] = 127;
            else if (col >= w) top[4 + i] = PlaneY[trRow * w + (w - 1)];
            else top[4 + i] = PlaneY[trRow * w + col];
        }
    }

    private struct FilterStrength
    {
        public int Ilevel;
        public int Limit;
        public int HevThresh;
    }

    // Precomputed [segment][is_i4x4] filter strengths.
    private readonly FilterStrength[,] _filterStrengths = new FilterStrength[NumMbSegments, 2];

    private void PrecomputeFilterStrengths()
    {
        for (var s = 0; s < NumMbSegments; s++)
        {
            int baseLevel;
            if (UseSegment)
            {
                baseLevel = SegmentFilterStrength[s];
                if (!AbsoluteDelta)
                    baseLevel += FilterLevel;
            }
            else
            {
                baseLevel = FilterLevel;
            }

            for (var i4x4 = 0; i4x4 <= 1; i4x4++)
            {
                var level = baseLevel;
                if (UseLfDelta)
                {
                    level += RefLfDelta[0]; // intra frames use reference 0
                    if (i4x4 == 1)
                        level += ModeLfDelta[0];
                }
                level = level < 0 ? 0 : level > 63 ? 63 : level;

                ref var info = ref _filterStrengths[s, i4x4];
                if (level > 0)
                {
                    var ilevel = level;
                    if (FilterSharpness > 0)
                    {
                        ilevel >>= FilterSharpness > 4 ? 2 : 1;
                        if (ilevel > 9 - FilterSharpness)
                            ilevel = 9 - FilterSharpness;
                    }
                    if (ilevel < 1)
                        ilevel = 1;
                    info.Ilevel = ilevel;
                    info.Limit = 2 * level + ilevel;
                    info.HevThresh = level >= 40 ? 2 : level >= 15 ? 1 : 0;
                }
                else
                {
                    info.Limit = 0;
                }
            }
        }
    }

    private void ApplyLoopFilter()
    {
        if (FilterType == 0)
            return;

        PrecomputeFilterStrengths();
        var yStride = LumaStride;
        var uvStride = ChromaStride;

        for (var mbY = 0; mbY < MbHeight; mbY++)
        for (var mbX = 0; mbX < MbWidth; mbX++)
        {
            var mb = mbY * MbWidth + mbX;
            var isI4x4 = MbIsI4x4[mb];
            var info = _filterStrengths[MbSegment[mb], isI4x4 ? 1 : 0];
            var limit = info.Limit;
            if (limit == 0)
                continue;

            var fInner = isI4x4 || !MbSkip[mb];
            var yOff = mbY * 16 * yStride + mbX * 16;

            if (FilterType == 1) // simple
            {
                if (mbX > 0) Vp8FilterApply.SimpleH16(PlaneY, yOff, yStride, limit + 4);
                if (fInner) Vp8FilterApply.SimpleH16i(PlaneY, yOff, yStride, limit);
                if (mbY > 0) Vp8FilterApply.SimpleV16(PlaneY, yOff, yStride, limit + 4);
                if (fInner) Vp8FilterApply.SimpleV16i(PlaneY, yOff, yStride, limit);
            }
            else // normal
            {
                var uvOff = mbY * 8 * uvStride + mbX * 8;
                var il = info.Ilevel;
                var hev = info.HevThresh;
                if (mbX > 0)
                {
                    Vp8FilterApply.H16(PlaneY, yOff, yStride, limit + 4, il, hev);
                    Vp8FilterApply.H8(PlaneU, PlaneV, uvOff, uvStride, limit + 4, il, hev);
                }
                if (fInner)
                {
                    Vp8FilterApply.H16i(PlaneY, yOff, yStride, limit, il, hev);
                    Vp8FilterApply.H8i(PlaneU, PlaneV, uvOff, uvStride, limit, il, hev);
                }
                if (mbY > 0)
                {
                    Vp8FilterApply.V16(PlaneY, yOff, yStride, limit + 4, il, hev);
                    Vp8FilterApply.V8(PlaneU, PlaneV, uvOff, uvStride, limit + 4, il, hev);
                }
                if (fInner)
                {
                    Vp8FilterApply.V16i(PlaneY, yOff, yStride, limit, il, hev);
                    Vp8FilterApply.V8i(PlaneU, PlaneV, uvOff, uvStride, limit, il, hev);
                }
            }
        }
    }

    /// <summary>Decodes the frame to a cropped RGBA image using simple (nearest) chroma upsampling.</summary>
    /// <param name="applyFilter">Whether to apply the in-loop deblocking filter.</param>
    /// <returns>The RGBA pixels, <see cref="Width"/> × <see cref="Height"/>, row-major.</returns>
    internal byte[] DecodeToRgba(bool applyFilter = true)
    {
        ParseHeaders();
        DecodeMacroblocks();
        Reconstruct();
        if (applyFilter)
            ApplyLoopFilter();
        return ToRgba();
    }

    private byte[] ToRgba()
    {
        var w = LumaStride;
        var cw = ChromaStride;
        var rgba = new byte[Width * Height * 4];
        for (var y = 0; y < Height; y++)
        {
            var cy = y >> 1;
            for (var x = 0; x < Width; x++)
            {
                var cx = x >> 1;
                Vp8Yuv.YuvToRgb(PlaneY[y * w + x], PlaneU[cy * cw + cx], PlaneV[cy * cw + cx],
                    out var r, out var g, out var b);
                var o = (y * Width + x) * 4;
                rgba[o] = r;
                rgba[o + 1] = g;
                rgba[o + 2] = b;
                rgba[o + 3] = 255;
            }
        }
        return rgba;
    }
}

/// <summary>Non-zero coefficient context for one macroblock column (top) or the rolling left neighbor.</summary>
internal struct Vp8Mb
{
    /// <summary>Packed per-4x4 non-zero flags (Y in bits 0-3, U in 4-5, V in 6-7).</summary>
    public uint Nz;
    /// <summary>Whether the second-order (Y2) DC block was non-zero.</summary>
    public int NzDc;
}

/// <summary>The per-segment VP8 dequantization step sizes.</summary>
internal struct Vp8QuantMatrix
{
    /// <summary>Luma DC step.</summary>
    public int Y1Dc;
    /// <summary>Luma AC step.</summary>
    public int Y1Ac;
    /// <summary>Luma second-order (Y2/WHT) DC step.</summary>
    public int Y2Dc;
    /// <summary>Luma second-order (Y2/WHT) AC step.</summary>
    public int Y2Ac;
    /// <summary>Chroma DC step.</summary>
    public int UvDc;
    /// <summary>Chroma AC step.</summary>
    public int UvAc;
}

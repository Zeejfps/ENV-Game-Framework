using WebPSharp.Api;

namespace WebPSharp.Vp8;

/// <summary>
/// Encodes an RGB(A) image as a VP8 (lossy) key-frame bitstream. This is a straightforward intra
/// encoder: every macroblock uses whole-block prediction (16x16 luma + 8x8 chroma, choosing among
/// DC/V/H/TM by prediction error), a single DCT coefficient partition, no segmentation, no in-loop
/// filter, and the default coefficient probabilities. It is the exact bitstream mirror of
/// <see cref="Vp8Decoder"/>: prediction and reconstruction reuse the same transforms and dequant
/// steps the decoder applies, so a decode of the output reproduces the encoder's reconstruction
/// bit-for-bit. Quality only controls the base quantizer.
/// </summary>
internal sealed class Vp8Encoder
{
    // 16x16 / chroma intra modes, aliased onto the 4x4 B-mode enumeration (see Vp8Decoder).
    private const int DcPred = 0, TmPred = 1, VPred = 2, HPred = 3;
    private const int MaxLevel = 2047;

    private readonly int _width;
    private readonly int _height;
    private readonly int _mbW;
    private readonly int _mbH;
    private readonly int _yStride;
    private readonly int _uvStride;

    private readonly byte[] _srcY;
    private readonly byte[] _srcU;
    private readonly byte[] _srcV;
    private readonly byte[] _recY;
    private readonly byte[] _recU;
    private readonly byte[] _recV;

    private readonly Vp8QuantMatrix _q;
    private readonly int _baseQ;
    private readonly byte[] _probs = Vp8Tables.DefaultCoeffProbs;

    private readonly Vp8BooleanEncoder _header = new(1024);
    private readonly Vp8BooleanEncoder _tokens = new(4096);

    private Vp8Mb[] _topMb = System.Array.Empty<Vp8Mb>();
    private Vp8Mb _leftMb;

    // Quantized coefficient levels (raster order) for the macroblock currently being emitted.
    private readonly short[] _y2Levels = new short[16];
    private readonly short[] _yLevels = new short[16 * 16];
    private readonly short[] _uLevels = new short[4 * 16];
    private readonly short[] _vLevels = new short[4 * 16];

    private Vp8Encoder(WebPImage image, int quality)
    {
        _width = image.Width;
        _height = image.Height;
        _mbW = (_width + 15) >> 4;
        _mbH = (_height + 15) >> 4;
        _yStride = _mbW * 16;
        _uvStride = _mbW * 8;

        _srcY = new byte[_yStride * _mbH * 16];
        _srcU = new byte[_uvStride * _mbH * 8];
        _srcV = new byte[_uvStride * _mbH * 8];
        _recY = new byte[_srcY.Length];
        _recU = new byte[_srcU.Length];
        _recV = new byte[_srcV.Length];

        ConvertToYuv(image);

        _baseQ = QualityToBaseQuant(quality);
        _q = BuildQuantMatrix(_baseQ);
    }

    /// <summary>Encodes an image to a raw VP8 key-frame bitstream (the payload of a <c>VP8 </c> chunk).</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="quality">The quality, 0 (smallest) to 100 (best).</param>
    /// <returns>The VP8 bitstream.</returns>
    public static byte[] Encode(WebPImage image, int quality) => new Vp8Encoder(image, quality).Run();

    // ---- Quantization setup ----

    // Maps a 0..100 quality to a base quantizer index (0..127), matching libwebp's
    // VP8SetSegmentParams / QualityToCompression for the single-segment, no-SNS case.
    private static int QualityToBaseQuant(int quality)
    {
        var qf = Math.Clamp(quality, 0, 100) / 100.0;
        var linear = qf < 0.75 ? qf * (2.0 / 3.0) : 2.0 * qf - 1.0;
        var c = Math.Pow(linear, 1.0 / 3.0);
        return Math.Clamp((int)(127.0 * (1.0 - c)), 0, 127);
    }

    // Identical to Vp8Decoder.ParseQuant with segmentation off and every delta zero, so the encoder
    // and decoder dequantize with exactly the same steps.
    private static Vp8QuantMatrix BuildQuantMatrix(int q) => new()
    {
        Y1Dc = Vp8Tables.DcTable[q],
        Y1Ac = Vp8Tables.AcTable[q],
        Y2Dc = Vp8Tables.DcTable[q] * 2,
        Y2Ac = Math.Max(8, (Vp8Tables.AcTable[q] * 101581) >> 16),
        UvDc = Vp8Tables.DcTable[Math.Min(q, 117)],
        UvAc = Vp8Tables.AcTable[q],
    };

    // ---- RGB -> YUV (BT.601, libwebp fixed-point constants) ----

    private void ConvertToYuv(WebPImage image)
    {
        var px = image.PixelData;
        var comp = image.ComponentCount;
        var rowStride = image.Stride;

        // Luma at full resolution; padding replicates the nearest edge pixel.
        for (var y = 0; y < _mbH * 16; y++)
        {
            var sy = Math.Min(y, _height - 1);
            for (var x = 0; x < _yStride; x++)
            {
                var sx = Math.Min(x, _width - 1);
                var o = sy * rowStride + sx * comp;
                _srcY[y * _yStride + x] = (byte)RgbToY(px[o], px[o + 1], px[o + 2]);
            }
        }

        // Chroma at 4:2:0; each sample averages the covered 2x2 luma-resolution block.
        for (var cy = 0; cy < _mbH * 8; cy++)
        for (var cx = 0; cx < _uvStride; cx++)
        {
            int sr = 0, sg = 0, sb = 0;
            for (var dy = 0; dy < 2; dy++)
            for (var dx = 0; dx < 2; dx++)
            {
                var sx = Math.Min(2 * cx + dx, _width - 1);
                var sy = Math.Min(2 * cy + dy, _height - 1);
                var o = sy * rowStride + sx * comp;
                sr += px[o];
                sg += px[o + 1];
                sb += px[o + 2];
            }
            _srcU[cy * _uvStride + cx] = (byte)RgbToU(sr, sg, sb);
            _srcV[cy * _uvStride + cx] = (byte)RgbToV(sr, sg, sb);
        }
    }

    private static int RgbToY(int r, int g, int b) =>
        (16839 * r + 33059 * g + 6420 * b + (1 << 15) + (16 << 16)) >> 16;

    // r,g,b are sums of four pixels; the extra >>2 (built into the >>18) forms the average.
    private static int RgbToU(int r, int g, int b) => ClipUv(-9719 * r - 19081 * g + 28800 * b);

    private static int RgbToV(int r, int g, int b) => ClipUv(28800 * r - 24116 * g - 4684 * b);

    private static int ClipUv(int uv)
    {
        uv = (uv + (1 << 17) + (128 << 18)) >> 18;
        return uv < 0 ? 0 : uv > 255 ? 255 : uv;
    }

    // ---- Main encode loop ----

    private byte[] Run()
    {
        WriteFrameHeader();

        _topMb = new Vp8Mb[_mbW];
        for (var mbY = 0; mbY < _mbH; mbY++)
        {
            _leftMb = default;
            for (var mbX = 0; mbX < _mbW; mbX++)
            {
                var lumaMode = EncodeLuma(mbX, mbY);
                var uvMode = EncodeChroma(mbX, mbY);
                WriteModes(lumaMode, uvMode);
                EncodeResiduals(mbX);
            }
        }

        return Assemble();
    }

    // Writes the compressed frame header into the first partition (mirrors Vp8Decoder.ParseHeaders).
    private void WriteFrameHeader()
    {
        _header.PutBit(128, 0); // color space
        _header.PutBit(128, 0); // clamping type

        _header.PutBit(128, 0); // use_segment = false

        _header.PutBit(128, 0); // filter_simple (irrelevant at level 0)
        _header.PutLiteral(0, 6); // filter level 0 -> no loop filter
        _header.PutLiteral(0, 3); // sharpness
        _header.PutBit(128, 0); // use loop-filter deltas = false

        _header.PutLiteral(0, 2); // log2(num partitions) = 0 -> 1 DCT partition

        _header.PutLiteral((uint)_baseQ, 7); // base quantizer
        for (var i = 0; i < 5; i++)
            _header.PutBit(128, 0); // y1dc/y2dc/y2ac/uvdc/uvac deltas absent

        _header.PutBit(128, 0); // update_proba flag (ignored for key frames)
        for (var i = 0; i < _probs.Length; i++)
            _header.PutBit(Vp8Tables.CoeffUpdateProbs[i], 0); // keep default coefficient probabilities

        _header.PutBit(128, 0); // use_skip_proba = false
    }

    private void WriteModes(int lumaMode, int uvMode)
    {
        _header.PutBit(145, 1); // not i4x4 (16x16 prediction)

        switch (lumaMode)
        {
            case DcPred: _header.PutBit(156, 0); _header.PutBit(163, 0); break;
            case VPred: _header.PutBit(156, 0); _header.PutBit(163, 1); break;
            case HPred: _header.PutBit(156, 1); _header.PutBit(128, 0); break;
            default: _header.PutBit(156, 1); _header.PutBit(128, 1); break; // TM
        }

        switch (uvMode)
        {
            case DcPred: _header.PutBit(142, 0); break;
            case VPred: _header.PutBit(142, 1); _header.PutBit(114, 0); break;
            case HPred: _header.PutBit(142, 1); _header.PutBit(114, 1); _header.PutBit(183, 0); break;
            default: _header.PutBit(142, 1); _header.PutBit(114, 1); _header.PutBit(183, 1); break; // TM
        }
    }

    // ---- Luma (16x16) ----

    private int EncodeLuma(int mbX, int mbY)
    {
        var px = mbX * 16;
        var py = mbY * 16;
        var hasTop = mbY > 0;
        var hasLeft = mbX > 0;

        Span<byte> top = stackalloc byte[16];
        Span<byte> left = stackalloc byte[16];
        for (var i = 0; i < 16; i++)
        {
            top[i] = SampleRecY(py - 1, px + i);
            left[i] = SampleRecY(py + i, px - 1);
        }
        var corner = SampleRecY(py - 1, px - 1);

        Span<byte> pred = stackalloc byte[256];
        var mode = ChooseMode(_srcY, px, py, _yStride, 16, top, left, corner, hasTop, hasLeft, pred);

        // Residuals -> forward DCT; collect DC of each 4x4 block for the second-order (WHT) block.
        Span<short> dcs = stackalloc short[16];
        Span<int> res = stackalloc int[16];
        Span<short> coeffs = stackalloc short[16];
        for (var blk = 0; blk < 16; blk++)
        {
            var bx = (blk & 3) * 4;
            var by = (blk >> 2) * 4;
            for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                res[r * 4 + c] = _srcY[(py + by + r) * _yStride + px + bx + c] - pred[(by + r) * 16 + bx + c];

            Vp8Transform.ForwardDct(res, coeffs);
            dcs[blk] = coeffs[0];
            var lvl = _yLevels.AsSpan(blk * 16, 16);
            lvl[0] = 0; // DC is carried by the WHT block
            for (var k = 1; k < 16; k++)
                lvl[k] = Quantize(coeffs[k], _q.Y1Ac);
        }

        // Second-order block: forward WHT of the 16 DCs, then quantize.
        Span<short> y2 = stackalloc short[16];
        Vp8Transform.ForwardWht(dcs, y2);
        _y2Levels[0] = Quantize(y2[0], _q.Y2Dc);
        for (var k = 1; k < 16; k++)
            _y2Levels[k] = Quantize(y2[k], _q.Y2Ac);

        ReconstructLuma(px, py, pred);
        return mode;
    }

    private void ReconstructLuma(int px, int py, ReadOnlySpan<byte> pred)
    {
        // Dequantize the WHT block and invert it to recover each 4x4 block's DC (as the decoder does).
        Span<short> y2deq = stackalloc short[16];
        y2deq[0] = (short)(_y2Levels[0] * _q.Y2Dc);
        for (var k = 1; k < 16; k++)
            y2deq[k] = (short)(_y2Levels[k] * _q.Y2Ac);
        Span<short> dcVals = stackalloc short[16];
        Vp8Transform.InverseWht(y2deq, dcVals);

        // Lay down the prediction, then add the dequantized residual of each block.
        for (var r = 0; r < 16; r++)
            pred.Slice(r * 16, 16).CopyTo(_recY.AsSpan((py + r) * _yStride + px, 16));

        Span<short> full = stackalloc short[16];
        Span<int> res = stackalloc int[16];
        for (var blk = 0; blk < 16; blk++)
        {
            var lvl = _yLevels.AsSpan(blk * 16, 16);
            full[0] = dcVals[blk];
            for (var k = 1; k < 16; k++)
                full[k] = (short)(lvl[k] * _q.Y1Ac);
            Vp8Transform.InverseDct(full, res);

            var bx = (blk & 3) * 4;
            var by = (blk >> 2) * 4;
            Vp8Transform.AddResidual(_recY.AsSpan((py + by) * _yStride + px + bx), _yStride, res);
        }
    }

    // ---- Chroma (8x8, shared U/V mode) ----

    private int EncodeChroma(int mbX, int mbY)
    {
        var px = mbX * 8;
        var py = mbY * 8;
        var hasTop = mbY > 0;
        var hasLeft = mbX > 0;

        Span<byte> topU = stackalloc byte[8];
        Span<byte> leftU = stackalloc byte[8];
        Span<byte> topV = stackalloc byte[8];
        Span<byte> leftV = stackalloc byte[8];
        for (var i = 0; i < 8; i++)
        {
            topU[i] = SampleRecC(_recU, py - 1, px + i);
            leftU[i] = SampleRecC(_recU, py + i, px - 1);
            topV[i] = SampleRecC(_recV, py - 1, px + i);
            leftV[i] = SampleRecC(_recV, py + i, px - 1);
        }
        var cornerU = ChromaCorner(_recU, mbX, mbY, px, py);
        var cornerV = ChromaCorner(_recV, mbX, mbY, px, py);

        // U and V share one prediction mode; pick the one minimizing the combined error.
        var bestMode = DcPred;
        var bestSse = long.MaxValue;
        Span<byte> predU = stackalloc byte[64];
        Span<byte> predV = stackalloc byte[64];
        Span<byte> tmpU = stackalloc byte[64];
        Span<byte> tmpV = stackalloc byte[64];
        foreach (var mode in stackalloc[] { DcPred, VPred, HPred, TmPred })
        {
            Predict(mode, tmpU, 8, 8, topU, leftU, cornerU, hasTop, hasLeft);
            Predict(mode, tmpV, 8, 8, topV, leftV, cornerV, hasTop, hasLeft);
            var sse = Sse(_srcU, px, py, _uvStride, 8, tmpU) + Sse(_srcV, px, py, _uvStride, 8, tmpV);
            if (sse < bestSse)
            {
                bestSse = sse;
                bestMode = mode;
                tmpU.CopyTo(predU);
                tmpV.CopyTo(predV);
            }
        }

        ProcessChromaPlane(_srcU, _recU, predU, px, py, _uLevels);
        ProcessChromaPlane(_srcV, _recV, predV, px, py, _vLevels);
        return bestMode;
    }

    private void ProcessChromaPlane(byte[] src, byte[] rec, ReadOnlySpan<byte> pred, int px, int py, short[] levels)
    {
        Span<int> res = stackalloc int[16];
        Span<short> coeffs = stackalloc short[16];
        for (var blk = 0; blk < 4; blk++)
        {
            var bx = (blk & 1) * 4;
            var by = (blk >> 1) * 4;
            for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                res[r * 4 + c] = src[(py + by + r) * _uvStride + px + bx + c] - pred[(by + r) * 8 + bx + c];

            Vp8Transform.ForwardDct(res, coeffs);
            var lvl = levels.AsSpan(blk * 16, 16);
            lvl[0] = Quantize(coeffs[0], _q.UvDc);
            for (var k = 1; k < 16; k++)
                lvl[k] = Quantize(coeffs[k], _q.UvAc);
        }

        for (var r = 0; r < 8; r++)
            pred.Slice(r * 8, 8).CopyTo(rec.AsSpan((py + r) * _uvStride + px, 8));

        Span<short> full = stackalloc short[16];
        for (var blk = 0; blk < 4; blk++)
        {
            var lvl = levels.AsSpan(blk * 16, 16);
            full[0] = (short)(lvl[0] * _q.UvDc);
            for (var k = 1; k < 16; k++)
                full[k] = (short)(lvl[k] * _q.UvAc);
            Vp8Transform.InverseDct(full, res);

            var bx = (blk & 1) * 4;
            var by = (blk >> 1) * 4;
            Vp8Transform.AddResidual(rec.AsSpan((py + by) * _uvStride + px + bx), _uvStride, res);
        }
    }

    private byte ChromaCorner(byte[] plane, int mbX, int mbY, int px, int py) =>
        mbY == 0 ? (byte)127 : mbX == 0 ? (byte)129 : plane[(py - 1) * _uvStride + px - 1];

    // ---- Coefficient token encoding (mirror of Vp8Coefficients.GetCoeffs) ----

    private void EncodeResiduals(int mbX)
    {
        ref var top = ref _topMb[mbX];

        var ctx = top.NzDc + _leftMb.NzDc;
        var nz2 = PutCoeffs(1, ctx, 0, _y2Levels);
        top.NzDc = _leftMb.NzDc = nz2 > 0 ? 1 : 0;

        var tnz = top.Nz & 0x0Fu;
        var lnz = _leftMb.Nz & 0x0Fu;
        for (var y = 0; y < 4; y++)
        {
            var l = lnz & 1;
            for (var x = 0; x < 4; x++)
            {
                var c = (int)(l + (tnz & 1));
                var nz = PutCoeffs(0, c, 1, _yLevels.AsSpan((y * 4 + x) * 16, 16));
                l = nz > 1 ? 1u : 0u;
                tnz = (tnz >> 1) | (l << 7);
            }
            tnz >>= 4;
            lnz = (lnz >> 1) | (l << 7);
        }
        var outTnz = tnz;
        var outLnz = lnz >> 4;

        for (var ch = 0; ch < 4; ch += 2)
        {
            var levels = ch == 0 ? _uLevels : _vLevels;
            tnz = top.Nz >> (4 + ch);
            lnz = _leftMb.Nz >> (4 + ch);
            for (var y = 0; y < 2; y++)
            {
                var l = lnz & 1;
                for (var x = 0; x < 2; x++)
                {
                    var c = (int)(l + (tnz & 1));
                    var nz = PutCoeffs(2, c, 0, levels.AsSpan((y * 2 + x) * 16, 16));
                    l = nz > 0 ? 1u : 0u;
                    tnz = (tnz >> 1) | (l << 3);
                }
                tnz >>= 2;
                lnz = (lnz >> 1) | (l << 5);
            }
            outTnz |= (tnz << 4) << ch;
            outLnz |= (lnz & 0xF0u) << ch;
        }

        top.Nz = outTnz;
        _leftMb.Nz = outLnz;
    }

    // Encodes one 4x4 block's coefficients (levels in raster order) and returns the position of the
    // last non-zero coefficient plus one, exactly as GetCoeffs returns on decode.
    private int PutCoeffs(int type, int ctx, int first, ReadOnlySpan<short> raster)
    {
        var last = first - 1;
        for (var n = first; n < 16; n++)
            if (raster[Vp8Tables.Zigzag[n]] != 0)
                last = n;

        var p = Vp8Tables.CoeffProbIndex(type, Vp8Tables.Bands[first], ctx, 0);
        var pos = first;
        while (true)
        {
            if (pos > last)
            {
                _tokens.PutBit(_probs[p], 0); // end of block
                break;
            }

            _tokens.PutBit(_probs[p], 1);
            while (raster[Vp8Tables.Zigzag[pos]] == 0)
            {
                _tokens.PutBit(_probs[p + 1], 0); // zero coefficient
                pos++;
                p = Vp8Tables.CoeffProbIndex(type, Vp8Tables.Bands[pos], 0, 0);
            }

            _tokens.PutBit(_probs[p + 1], 1); // non-zero
            var v = raster[Vp8Tables.Zigzag[pos]];
            var a = v < 0 ? -v : v;
            int nextCtx;
            if (a == 1)
            {
                _tokens.PutBit(_probs[p + 2], 0);
                nextCtx = 1;
            }
            else
            {
                _tokens.PutBit(_probs[p + 2], 1);
                PutLargeValue(p, a);
                nextCtx = 2;
            }

            _tokens.PutBit(128, v < 0 ? 1 : 0); // sign

            pos++;
            if (pos == 16)
                break;
            p = Vp8Tables.CoeffProbIndex(type, Vp8Tables.Bands[pos], nextCtx, 0);
        }

        return last + 1;
    }

    // Mirror of Vp8Coefficients.GetLargeValue for magnitudes >= 2.
    private void PutLargeValue(int p, int a)
    {
        var cp = _probs;
        if (a <= 4)
        {
            _tokens.PutBit(cp[p + 3], 0);
            if (a == 2)
            {
                _tokens.PutBit(cp[p + 4], 0);
            }
            else
            {
                _tokens.PutBit(cp[p + 4], 1);
                _tokens.PutBit(cp[p + 5], a - 3); // 3 or 4
            }
        }
        else if (a <= 10)
        {
            _tokens.PutBit(cp[p + 3], 1);
            _tokens.PutBit(cp[p + 6], 0);
            if (a <= 6)
            {
                _tokens.PutBit(cp[p + 7], 0);
                _tokens.PutBit(159, a - 5); // 5 or 6
            }
            else
            {
                _tokens.PutBit(cp[p + 7], 1);
                var d = a - 7; // 0..3
                _tokens.PutBit(165, (d >> 1) & 1);
                _tokens.PutBit(145, d & 1);
            }
        }
        else
        {
            _tokens.PutBit(cp[p + 3], 1);
            _tokens.PutBit(cp[p + 6], 1);
            int cat, baseVal;
            byte[] tab;
            if (a <= 18) { cat = 0; baseVal = 11; tab = Vp8Tables.Cat3; }
            else if (a <= 34) { cat = 1; baseVal = 19; tab = Vp8Tables.Cat4; }
            else if (a <= 66) { cat = 2; baseVal = 35; tab = Vp8Tables.Cat5; }
            else { cat = 3; baseVal = 67; tab = Vp8Tables.Cat6; }

            var bit1 = cat >> 1;
            _tokens.PutBit(cp[p + 8], bit1);
            _tokens.PutBit(cp[p + 9 + bit1], cat & 1);

            var bits = tab.Length - 1; // trailing 0 is the sentinel
            var extra = a - baseVal;
            for (var i = 0; i < bits; i++)
                _tokens.PutBit(tab[i], (extra >> (bits - 1 - i)) & 1);
        }
    }

    // ---- Prediction / mode search ----

    private int ChooseMode(byte[] src, int px, int py, int stride, int size,
        ReadOnlySpan<byte> top, ReadOnlySpan<byte> left, byte corner, bool hasTop, bool hasLeft,
        Span<byte> bestPred)
    {
        var bestMode = DcPred;
        var bestSse = long.MaxValue;
        Span<byte> pred = stackalloc byte[256];
        foreach (var mode in stackalloc[] { DcPred, VPred, HPred, TmPred })
        {
            var block = pred.Slice(0, size * size);
            Predict(mode, block, size, size, top, left, corner, hasTop, hasLeft);
            var sse = Sse(src, px, py, stride, size, block);
            if (sse < bestSse)
            {
                bestSse = sse;
                bestMode = mode;
                block.CopyTo(bestPred);
            }
        }
        return bestMode;
    }

    private static void Predict(int mode, Span<byte> dst, int stride, int size,
        ReadOnlySpan<byte> top, ReadOnlySpan<byte> left, byte corner, bool hasTop, bool hasLeft)
    {
        switch (mode)
        {
            case DcPred: Vp8Prediction.FillDc(dst, stride, size, top, left, hasTop, hasLeft); break;
            case VPred: Vp8Prediction.FillVertical(dst, stride, size, top); break;
            case HPred: Vp8Prediction.FillHorizontal(dst, stride, size, left); break;
            default: Vp8Prediction.FillTrueMotion(dst, stride, size, top, left, corner); break;
        }
    }

    private static long Sse(byte[] src, int px, int py, int stride, int size, ReadOnlySpan<byte> pred)
    {
        long sse = 0;
        for (var r = 0; r < size; r++)
        for (var c = 0; c < size; c++)
        {
            int d = src[(py + r) * stride + px + c] - pred[r * size + c];
            sse += (long)d * d;
        }
        return sse;
    }

    private byte SampleRecY(int row, int col)
    {
        if (row < 0) return 127;
        if (col < 0) return 129;
        return _recY[row * _yStride + col];
    }

    private byte SampleRecC(byte[] plane, int row, int col)
    {
        if (row < 0) return 127;
        if (col < 0) return 129;
        return plane[row * _uvStride + col];
    }

    private static short Quantize(int coeff, int step)
    {
        var a = coeff < 0 ? -coeff : coeff;
        var level = (a + (step >> 1)) / step;
        if (level > MaxLevel) level = MaxLevel;
        return (short)(coeff < 0 ? -level : level);
    }

    // ---- Bitstream assembly ----

    private byte[] Assemble()
    {
        var hdr = _header.Finish();
        var tok = _tokens.Finish();
        var partLen = hdr.Length;

        var outBuf = new byte[10 + hdr.Length + tok.Length];
        var frameTag = (1 << 4) | (partLen << 5); // key frame, profile 0, show, partition length
        outBuf[0] = (byte)frameTag;
        outBuf[1] = (byte)(frameTag >> 8);
        outBuf[2] = (byte)(frameTag >> 16);
        outBuf[3] = 0x9D;
        outBuf[4] = 0x01;
        outBuf[5] = 0x2A;
        outBuf[6] = (byte)_width;
        outBuf[7] = (byte)((_width >> 8) & 0x3F);
        outBuf[8] = (byte)_height;
        outBuf[9] = (byte)((_height >> 8) & 0x3F);

        Array.Copy(hdr, 0, outBuf, 10, hdr.Length);
        Array.Copy(tok, 0, outBuf, 10 + hdr.Length, tok.Length);
        return outBuf;
    }
}

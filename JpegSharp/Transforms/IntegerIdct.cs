namespace JpegSharp.Transforms;

/// <summary>
/// Fixed-point (integer) inverse 8x8 DCT modelled on libjpeg-turbo's "islow" transform
/// (<c>jidctint.c</c>). Uses 13-bit constants with a two-pass Loeffler-style factorization:
/// the column pass keeps 2 guard bits (<see cref="Pass1Bits"/>) and the row pass performs the
/// final descale. The output is the centred spatial value (still requiring the +128 level shift),
/// rounded to the nearest integer.
/// </summary>
/// <remarks>
/// This path is used for 8-bit baseline and progressive decode. It matches the double-precision
/// reference <see cref="Dct"/>/<see cref="FastDct"/> to within ~1 least-significant bit per pixel.
/// The double transforms remain the correctness oracle.
/// </remarks>
internal static class IntegerIdct
{
    private const int N = 8;
    private const int BlockSize = N * N;

    private const int ConstBits = 13;
    private const int Pass1Bits = 2;

    // FIX(x) = round(x * 2^ConstBits). Constants transcribed from jidctint.c.
    private const int Fix_0_298631336 = 2446;
    private const int Fix_0_390180644 = 3196;
    private const int Fix_0_541196100 = 4433;
    private const int Fix_0_765366865 = 6270;
    private const int Fix_0_899976223 = 7373;
    private const int Fix_1_175875602 = 9633;
    private const int Fix_1_501321110 = 12299;
    private const int Fix_1_847759065 = 15137;
    private const int Fix_1_961570560 = 16069;
    private const int Fix_2_053119869 = 16819;
    private const int Fix_2_562915447 = 20995;
    private const int Fix_3_072711026 = 25172;

    /// <summary>
    /// Applies the inverse 2D DCT to a block of already-dequantized coefficients.
    /// </summary>
    /// <param name="coefficients">64 dequantized coefficients in natural (row-major) order.</param>
    /// <param name="output">Receives 64 centred spatial samples in natural order (add 128 to level-shift).</param>
    /// <exception cref="ArgumentException">A span is not exactly 64 elements long.</exception>
    public static void Inverse(ReadOnlySpan<int> coefficients, Span<int> output)
    {
        if (coefficients.Length != BlockSize)
            throw new ArgumentException($"Block must contain exactly {BlockSize} values.", nameof(coefficients));
        if (output.Length != BlockSize)
            throw new ArgumentException($"Block must contain exactly {BlockSize} values.", nameof(output));

        Span<int> ws = stackalloc int[BlockSize];

        // Pass 1: columns. Descale by ConstBits - Pass1Bits, leaving Pass1Bits guard bits.
        for (var c = 0; c < N; c++)
        {
            var i0 = coefficients[c + N * 0];
            var i1 = coefficients[c + N * 1];
            var i2 = coefficients[c + N * 2];
            var i3 = coefficients[c + N * 3];
            var i4 = coefficients[c + N * 4];
            var i5 = coefficients[c + N * 5];
            var i6 = coefficients[c + N * 6];
            var i7 = coefficients[c + N * 7];

            // AC-all-zero shortcut: the whole column collapses to the (scaled) DC term.
            if ((i1 | i2 | i3 | i4 | i5 | i6 | i7) == 0)
            {
                var dc = i0 << Pass1Bits;
                ws[c + N * 0] = dc; ws[c + N * 1] = dc; ws[c + N * 2] = dc; ws[c + N * 3] = dc;
                ws[c + N * 4] = dc; ws[c + N * 5] = dc; ws[c + N * 6] = dc; ws[c + N * 7] = dc;
                continue;
            }

            // Even part.
            var z2 = i2;
            var z3 = i6;
            var z1 = (z2 + z3) * Fix_0_541196100;
            var tmp2 = z1 + z3 * -Fix_1_847759065;
            var tmp3 = z1 + z2 * Fix_0_765366865;

            z2 = i0;
            z3 = i4;
            var tmp0 = (z2 + z3) << ConstBits;
            var tmp1 = (z2 - z3) << ConstBits;

            var tmp10 = tmp0 + tmp3;
            var tmp13 = tmp0 - tmp3;
            var tmp11 = tmp1 + tmp2;
            var tmp12 = tmp1 - tmp2;

            // Odd part.
            tmp0 = i7;
            tmp1 = i5;
            tmp2 = i3;
            tmp3 = i1;

            z1 = tmp0 + tmp3;
            z2 = tmp1 + tmp2;
            z3 = tmp0 + tmp2;
            var z4 = tmp1 + tmp3;
            var z5 = (z3 + z4) * Fix_1_175875602;

            tmp0 *= Fix_0_298631336;
            tmp1 *= Fix_2_053119869;
            tmp2 *= Fix_3_072711026;
            tmp3 *= Fix_1_501321110;
            z1 *= -Fix_0_899976223;
            z2 *= -Fix_2_562915447;
            z3 *= -Fix_1_961570560;
            z4 *= -Fix_0_390180644;

            z3 += z5;
            z4 += z5;

            tmp0 += z1 + z3;
            tmp1 += z2 + z4;
            tmp2 += z2 + z3;
            tmp3 += z1 + z4;

            ws[c + N * 0] = Descale(tmp10 + tmp3, ConstBits - Pass1Bits);
            ws[c + N * 7] = Descale(tmp10 - tmp3, ConstBits - Pass1Bits);
            ws[c + N * 1] = Descale(tmp11 + tmp2, ConstBits - Pass1Bits);
            ws[c + N * 6] = Descale(tmp11 - tmp2, ConstBits - Pass1Bits);
            ws[c + N * 2] = Descale(tmp12 + tmp1, ConstBits - Pass1Bits);
            ws[c + N * 5] = Descale(tmp12 - tmp1, ConstBits - Pass1Bits);
            ws[c + N * 3] = Descale(tmp13 + tmp0, ConstBits - Pass1Bits);
            ws[c + N * 4] = Descale(tmp13 - tmp0, ConstBits - Pass1Bits);
        }

        // Pass 2: rows. Final descale by ConstBits + Pass1Bits + 3 (the +3 absorbs the 1/8 IDCT norm).
        for (var r = 0; r < N; r++)
        {
            var o = r * N;
            var w0 = ws[o + 0];
            var w1 = ws[o + 1];
            var w2 = ws[o + 2];
            var w3 = ws[o + 3];
            var w4 = ws[o + 4];
            var w5 = ws[o + 5];
            var w6 = ws[o + 6];
            var w7 = ws[o + 7];

            if ((w1 | w2 | w3 | w4 | w5 | w6 | w7) == 0)
            {
                var dc = Descale(w0, Pass1Bits + 3);
                output[o + 0] = dc; output[o + 1] = dc; output[o + 2] = dc; output[o + 3] = dc;
                output[o + 4] = dc; output[o + 5] = dc; output[o + 6] = dc; output[o + 7] = dc;
                continue;
            }

            // Even part.
            var z2 = w2;
            var z3 = w6;
            var z1 = (z2 + z3) * Fix_0_541196100;
            var tmp2 = z1 + z3 * -Fix_1_847759065;
            var tmp3 = z1 + z2 * Fix_0_765366865;

            var tmp0 = (w0 + w4) << ConstBits;
            var tmp1 = (w0 - w4) << ConstBits;

            var tmp10 = tmp0 + tmp3;
            var tmp13 = tmp0 - tmp3;
            var tmp11 = tmp1 + tmp2;
            var tmp12 = tmp1 - tmp2;

            // Odd part.
            tmp0 = w7;
            tmp1 = w5;
            tmp2 = w3;
            tmp3 = w1;

            z1 = tmp0 + tmp3;
            z2 = tmp1 + tmp2;
            z3 = tmp0 + tmp2;
            var z4 = tmp1 + tmp3;
            var z5 = (z3 + z4) * Fix_1_175875602;

            tmp0 *= Fix_0_298631336;
            tmp1 *= Fix_2_053119869;
            tmp2 *= Fix_3_072711026;
            tmp3 *= Fix_1_501321110;
            z1 *= -Fix_0_899976223;
            z2 *= -Fix_2_562915447;
            z3 *= -Fix_1_961570560;
            z4 *= -Fix_0_390180644;

            z3 += z5;
            z4 += z5;

            tmp0 += z1 + z3;
            tmp1 += z2 + z4;
            tmp2 += z2 + z3;
            tmp3 += z1 + z4;

            output[o + 0] = Descale(tmp10 + tmp3, ConstBits + Pass1Bits + 3);
            output[o + 7] = Descale(tmp10 - tmp3, ConstBits + Pass1Bits + 3);
            output[o + 1] = Descale(tmp11 + tmp2, ConstBits + Pass1Bits + 3);
            output[o + 6] = Descale(tmp11 - tmp2, ConstBits + Pass1Bits + 3);
            output[o + 2] = Descale(tmp12 + tmp1, ConstBits + Pass1Bits + 3);
            output[o + 5] = Descale(tmp12 - tmp1, ConstBits + Pass1Bits + 3);
            output[o + 3] = Descale(tmp13 + tmp0, ConstBits + Pass1Bits + 3);
            output[o + 4] = Descale(tmp13 - tmp0, ConstBits + Pass1Bits + 3);
        }
    }

    // Rounding right-shift: DESCALE(x, n) = round(x / 2^n), ties toward +infinity.
    private static int Descale(int x, int n) => (x + (1 << (n - 1))) >> n;
}

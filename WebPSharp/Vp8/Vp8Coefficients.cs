namespace WebPSharp.Vp8;

/// <summary>
/// Decodes VP8 DCT coefficient tokens for a single 4x4 block from a boolean partition, following the
/// RFC 6386 / libwebp <c>GetCoeffs</c> / <c>GetLargeValue</c> token loop. Coefficients are written
/// in raster order (de-zig-zagged) and dequantized by the supplied DC/AC step sizes.
/// </summary>
internal static class Vp8Coefficients
{
    /// <summary>
    /// Decodes one block's coefficients into <paramref name="output"/> at <paramref name="offset"/>.
    /// </summary>
    /// <param name="br">The coefficient partition reader.</param>
    /// <param name="coeffProbs">The flat coefficient probability table.</param>
    /// <param name="type">The block coefficient type (0..3).</param>
    /// <param name="ctx">The initial context (0..2) from neighboring non-zero flags.</param>
    /// <param name="dcQuant">The DC dequantization step.</param>
    /// <param name="acQuant">The AC dequantization step.</param>
    /// <param name="firstCoeff">The first coefficient index (1 when the DC comes from the WHT, else 0).</param>
    /// <param name="output">The destination coefficient buffer.</param>
    /// <param name="offset">The offset of this block within <paramref name="output"/>.</param>
    /// <returns>The position of the last non-zero coefficient plus one (0..16).</returns>
    public static int GetCoeffs(Vp8BooleanDecoder br, byte[] coeffProbs, int type, int ctx,
        int dcQuant, int acQuant, int firstCoeff, short[] output, int offset)
    {
        var p = Vp8Tables.CoeffProbIndex(type, Vp8Tables.Bands[firstCoeff], ctx, 0);

        for (var n = firstCoeff; n < 16; n++)
        {
            if (br.GetBit(coeffProbs[p]) == 0)
                return n; // end of block

            while (br.GetBit(coeffProbs[p + 1]) == 0) // run of zero coefficients
            {
                n++;
                if (n == 16)
                    return 16;
                p = Vp8Tables.CoeffProbIndex(type, Vp8Tables.Bands[n], 0, 0);
            }

            // Non-zero coefficient.
            int v;
            if (br.GetBit(coeffProbs[p + 2]) == 0)
            {
                v = 1;
                p = Vp8Tables.CoeffProbIndex(type, Vp8Tables.Bands[n + 1], 1, 0);
            }
            else
            {
                v = GetLargeValue(br, coeffProbs, p);
                p = Vp8Tables.CoeffProbIndex(type, Vp8Tables.Bands[n + 1], 2, 0);
            }

            var value = br.ApplySign(v) * (n > 0 ? acQuant : dcQuant);
            output[offset + Vp8Tables.Zigzag[n]] = (short)value;
        }

        return 16;
    }

    private static int GetLargeValue(Vp8BooleanDecoder br, byte[] cp, int p)
    {
        int v;
        if (br.GetBit(cp[p + 3]) == 0)
        {
            v = br.GetBit(cp[p + 4]) == 0 ? 2 : 3 + br.GetBit(cp[p + 5]);
        }
        else if (br.GetBit(cp[p + 6]) == 0)
        {
            if (br.GetBit(cp[p + 7]) == 0)
                v = 5 + br.GetBit(159);
            else
                v = 7 + 2 * br.GetBit(165) + br.GetBit(145);
        }
        else
        {
            var bit1 = br.GetBit(cp[p + 8]);
            var bit0 = br.GetBit(cp[p + 9 + bit1]);
            var cat = 2 * bit1 + bit0;
            var tab = cat switch
            {
                0 => Vp8Tables.Cat3,
                1 => Vp8Tables.Cat4,
                2 => Vp8Tables.Cat5,
                _ => Vp8Tables.Cat6,
            };
            v = 0;
            for (var i = 0; tab[i] != 0; i++)
                v = v + v + br.GetBit(tab[i]);
            v += 3 + (8 << cat);
        }
        return v;
    }
}

namespace WebPSharp.Vp8L.Transforms;

/// <summary>
/// The VP8L color-indexing (palette) transform. The image is coded as palette indices rather than
/// full colors. When the palette is small the indices are further "bundled" — 2, 4, or 8 per pixel
/// packed into the green channel — which reduces the entropy-coded image width. The palette itself
/// is delta-coded (each entry stored as the difference from the previous) and recovered by a
/// per-channel prefix sum.
/// </summary>
internal static class ColorIndexingTransform
{
    private const uint OpaqueBlack = 0xFF000000u;

    /// <summary>Returns the bundling bit count for a palette of the given size.</summary>
    /// <param name="numColors">The palette size (1..256).</param>
    /// <returns>The bundling bits: 3 (≤2 colors), 2 (≤4), 1 (≤16), or 0 (≤256).</returns>
    public static int BitsForColorCount(int numColors)
        => numColors > 16 ? 0 : numColors > 4 ? 1 : numColors > 2 ? 2 : 3;

    /// <summary>Recovers the full palette from its delta-coded, decoded form.</summary>
    /// <param name="rawPalette">The decoded palette entries (deltas), length <paramref name="numColors"/>.</param>
    /// <param name="numColors">The palette size.</param>
    /// <param name="bits">The bundling bits.</param>
    /// <returns>The expanded palette padded to the addressable index range.</returns>
    public static uint[] ExpandPalette(uint[] rawPalette, int numColors, int bits)
    {
        var finalColors = 1 << (8 >> bits);
        var palette = new uint[finalColors];
        palette[0] = rawPalette[0];
        for (var i = 1; i < numColors; i++)
            palette[i] = Vp8LPredictors.AddPixels(rawPalette[i], palette[i - 1]);
        // Remaining entries are already zero (padding).
        return palette;
    }

    /// <summary>Delta-codes a palette so the decoder's prefix sum recovers it.</summary>
    /// <param name="palette">The palette colors.</param>
    /// <returns>The delta-coded palette.</returns>
    public static uint[] DeltaEncodePalette(ReadOnlySpan<uint> palette)
    {
        var raw = new uint[palette.Length];
        raw[0] = palette[0];
        for (var i = 1; i < palette.Length; i++)
            raw[i] = Vp8LPredictors.SubtractPixels(palette[i], palette[i - 1]);
        return raw;
    }

    /// <summary>Packs palette indices into a bundled ARGB image (encoder side).</summary>
    /// <param name="indices">One palette index per pixel, row-major.</param>
    /// <param name="width">The full image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bits">The bundling bits.</param>
    /// <returns>The bundled image of width <c>ceil(width / 2^bits)</c>.</returns>
    public static uint[] Bundle(byte[] indices, int width, int height, int bits)
    {
        var bundledWidth = Vp8LSubSample.Size(width, bits);
        var bitDepth = 8 >> bits;
        var subMask = (1 << bits) - 1;
        var result = new uint[bundledWidth * height];

        for (var y = 0; y < height; y++)
        {
            var code = OpaqueBlack;
            for (var x = 0; x < width; x++)
            {
                var xsub = x & subMask;
                if (xsub == 0)
                    code = OpaqueBlack;
                code |= (uint)indices[y * width + x] << (8 + bitDepth * xsub);
                result[y * bundledWidth + (x >> bits)] = code;
            }
        }

        return result;
    }

    /// <summary>Expands a bundled index image back to full-color pixels (decoder side).</summary>
    /// <param name="bundled">The bundled image.</param>
    /// <param name="bundledWidth">The bundled image width.</param>
    /// <param name="fullWidth">The output (full) image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="palette">The expanded palette (addressable size).</param>
    /// <param name="bits">The bundling bits.</param>
    /// <returns>The full-color image of width <paramref name="fullWidth"/>.</returns>
    public static uint[] Inverse(uint[] bundled, int bundledWidth, int fullWidth, int height, uint[] palette, int bits)
    {
        var bitDepth = 8 >> bits;
        var pixelMask = (1 << bitDepth) - 1;
        var perBundle = 1 << bits;
        var result = new uint[fullWidth * height];

        for (var y = 0; y < height; y++)
        {
            for (var xb = 0; xb < bundledWidth; xb++)
            {
                var packed = (bundled[y * bundledWidth + xb] >> 8) & 0xFF;
                for (var p = 0; p < perBundle; p++)
                {
                    var x = (xb << bits) + p;
                    if (x >= fullWidth)
                        break;
                    var index = (int)((packed >> (bitDepth * p)) & pixelMask);
                    result[y * fullWidth + x] = palette[index];
                }
            }
        }

        return result;
    }
}

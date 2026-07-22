namespace JpegSharp.Quantization;

/// <summary>
/// The example luminance and chrominance quantization tables from ITU-T T.81 Annex K.1,
/// stored in natural (row-major) 8x8 order. These are the base tables from which
/// quality-scaled tables are derived using the IJG scaling algorithm.
/// </summary>
internal static class StandardQuantizationTables
{
    /// <summary>Annex K.1 luminance quantization table (natural order).</summary>
    public static ReadOnlySpan<ushort> Luminance =>
    [
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    ];

    /// <summary>Annex K.2 chrominance quantization table (natural order).</summary>
    public static ReadOnlySpan<ushort> Chrominance =>
    [
        17, 18, 24, 47, 99, 99, 99, 99,
        18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99,
        47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99
    ];

    /// <summary>
    /// Converts an IJG-style quality factor (1..100) into the linear scale percentage
    /// applied to the base table values.
    /// </summary>
    /// <param name="quality">Requested quality; clamped to the range 1..100.</param>
    /// <returns>The scale percentage where 100 reproduces the base table.</returns>
    public static int QualityToScale(int quality)
    {
        if (quality < 1)
            quality = 1;
        else if (quality > 100)
            quality = 100;

        return quality < 50 ? 5000 / quality : 200 - quality * 2;
    }

    /// <summary>
    /// Scales a single base quantization value by the given scale percentage and clamps the
    /// result to the target element range: [1, 255] for an 8-bit table (Pq=0) or [1, 65535]
    /// for a high-precision table (Pq=1) when <paramref name="samplePrecision"/> exceeds 8.
    /// </summary>
    /// <param name="baseValue">The unscaled Annex K table entry.</param>
    /// <param name="scale">The scale percentage from <see cref="QualityToScale"/>.</param>
    /// <param name="samplePrecision">Target sample precision in bits; >8 permits 16-bit steps.</param>
    /// <returns>The scaled, clamped quantization step.</returns>
    public static ushort ScaleValue(ushort baseValue, int scale, int samplePrecision = 8)
    {
        var max = samplePrecision > 8 ? 65535 : 255;
        var scaled = (baseValue * scale + 50) / 100;
        if (scaled < 1)
            scaled = 1;
        else if (scaled > max)
            scaled = max;
        return (ushort)scaled;
    }
}

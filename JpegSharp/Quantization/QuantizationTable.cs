using JpegSharp.Transforms;

namespace JpegSharp.Quantization;

/// <summary>
/// An immutable 8x8 JPEG quantization table holding 64 divisor values in natural
/// (row-major) order. Instances are produced from the standard Annex K tables at a given
/// quality, from custom values, or parsed from a DQT marker segment.
/// </summary>
public sealed class QuantizationTable
{
    private readonly ushort[] _values;

    /// <summary>The number of entries in a quantization table.</summary>
    public const int Size = 64;

    /// <summary>
    /// Creates a quantization table from 64 values given in natural (row-major) order.
    /// </summary>
    /// <param name="valuesNaturalOrder">Exactly 64 non-zero quantization steps.</param>
    /// <exception cref="ArgumentException">
    /// The span is not exactly 64 elements, or contains a zero divisor.
    /// </exception>
    public QuantizationTable(ReadOnlySpan<ushort> valuesNaturalOrder)
    {
        if (valuesNaturalOrder.Length != Size)
            throw new ArgumentException($"A quantization table must contain exactly {Size} values.", nameof(valuesNaturalOrder));

        _values = new ushort[Size];
        for (var i = 0; i < Size; i++)
        {
            if (valuesNaturalOrder[i] == 0)
                throw new ArgumentException("Quantization values must be non-zero.", nameof(valuesNaturalOrder));
            _values[i] = valuesNaturalOrder[i];
        }
    }

    /// <summary>Gets the quantization step at the given natural-order index (0..63).</summary>
    /// <param name="index">Natural-order coefficient index.</param>
    public ushort this[int index] => _values[index];

    /// <summary>Gets the table values in natural (row-major) order.</summary>
    /// <returns>A read-only view over the 64 quantization steps.</returns>
    public ReadOnlySpan<ushort> AsSpan() => _values;

    /// <summary>
    /// Builds a luminance quantization table for the given quality factor using the IJG
    /// scaling of the Annex K.1 base table.
    /// </summary>
    /// <param name="quality">Quality factor; clamped to 1..100. 100 yields all-ones.</param>
    /// <returns>The scaled luminance table.</returns>
    public static QuantizationTable Luminance(int quality) =>
        Scaled(StandardQuantizationTables.Luminance, quality);

    /// <summary>
    /// Builds a chrominance quantization table for the given quality factor using the IJG
    /// scaling of the Annex K.2 base table.
    /// </summary>
    /// <param name="quality">Quality factor; clamped to 1..100. 100 yields all-ones.</param>
    /// <returns>The scaled chrominance table.</returns>
    public static QuantizationTable Chrominance(int quality) =>
        Scaled(StandardQuantizationTables.Chrominance, quality);

    /// <summary>
    /// Creates a quantization table from 64 values given in zig-zag order, as they appear
    /// in a DQT marker segment.
    /// </summary>
    /// <param name="valuesZigZag">Exactly 64 non-zero quantization steps in zig-zag order.</param>
    /// <returns>The table with values converted to natural order.</returns>
    /// <exception cref="ArgumentException">The span is not exactly 64 elements.</exception>
    public static QuantizationTable FromZigZag(ReadOnlySpan<ushort> valuesZigZag)
    {
        if (valuesZigZag.Length != Size)
            throw new ArgumentException($"A quantization table must contain exactly {Size} values.", nameof(valuesZigZag));

        Span<ushort> natural = stackalloc ushort[Size];
        var order = ZigZag.Order;
        for (var k = 0; k < Size; k++)
            natural[order[k]] = valuesZigZag[k];
        return new QuantizationTable(natural);
    }

    /// <summary>
    /// Copies the table values into the destination span in zig-zag order, ready to be
    /// written into a DQT marker segment.
    /// </summary>
    /// <param name="destination">A span of at least 64 elements.</param>
    /// <exception cref="ArgumentException">The destination is shorter than 64 elements.</exception>
    public void CopyToZigZag(Span<ushort> destination)
    {
        if (destination.Length < Size)
            throw new ArgumentException($"Destination must hold at least {Size} values.", nameof(destination));

        var order = ZigZag.Order;
        for (var k = 0; k < Size; k++)
            destination[k] = _values[order[k]];
    }

    private static QuantizationTable Scaled(ReadOnlySpan<ushort> baseTable, int quality)
    {
        var scale = StandardQuantizationTables.QualityToScale(quality);
        Span<ushort> scaled = stackalloc ushort[Size];
        for (var i = 0; i < Size; i++)
            scaled[i] = StandardQuantizationTables.ScaleValue(baseTable[i], scale);
        return new QuantizationTable(scaled);
    }
}

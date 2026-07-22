using JpegSharp.Transforms;

namespace JpegSharp.Quantization;

/// <summary>
/// Applies and reverses quantization on 8x8 blocks of DCT coefficients. Both operations
/// work on blocks in natural (row-major) order and are allocation-free.
/// </summary>
internal static class Quantizer
{
    private const int BlockSize = 64;

    /// <summary>
    /// Divides each DCT coefficient by its quantization step, rounding to the nearest
    /// integer (ties away from zero).
    /// </summary>
    /// <param name="coefficients">64 DCT coefficients in natural order.</param>
    /// <param name="table">64 quantization steps in natural order.</param>
    /// <param name="output">Receives the 64 quantized coefficients.</param>
    public static void Quantize(ReadOnlySpan<double> coefficients, ReadOnlySpan<ushort> table, Span<short> output)
    {
        EnsureBlock(coefficients.Length, nameof(coefficients));
        EnsureBlock(table.Length, nameof(table));
        EnsureBlock(output.Length, nameof(output));

        for (var i = 0; i < BlockSize; i++)
        {
            var value = coefficients[i] / table[i];
            var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
            output[i] = (short)Math.Clamp(rounded, short.MinValue, short.MaxValue);
        }
    }

    /// <summary>
    /// Multiplies each quantized coefficient by its quantization step to recover the
    /// (lossy) DCT coefficient.
    /// </summary>
    /// <param name="quantized">64 quantized coefficients in natural order.</param>
    /// <param name="table">64 quantization steps in natural order.</param>
    /// <param name="output">Receives the 64 dequantized coefficients.</param>
    public static void Dequantize(ReadOnlySpan<short> quantized, ReadOnlySpan<ushort> table, Span<double> output)
    {
        EnsureBlock(quantized.Length, nameof(quantized));
        EnsureBlock(table.Length, nameof(table));
        EnsureBlock(output.Length, nameof(output));

        for (var i = 0; i < BlockSize; i++)
            output[i] = quantized[i] * (double)table[i];
    }

    /// <summary>
    /// Quantizes DCT coefficients and emits the results directly in zig-zag order, fusing the
    /// quantization and zig-zag reordering into a single pass. Bit-identical to
    /// <see cref="Quantize"/> followed by <see cref="Transforms.ZigZag.FromNatural"/>.
    /// </summary>
    /// <param name="coefficients">64 DCT coefficients in natural order.</param>
    /// <param name="zigzagTable">64 quantization steps permuted into zig-zag order.</param>
    /// <param name="zigzagOutput">Receives the 64 quantized coefficients in zig-zag order.</param>
    public static void QuantizeToZigZag(ReadOnlySpan<double> coefficients, ReadOnlySpan<ushort> zigzagTable, Span<short> zigzagOutput)
    {
        EnsureBlock(coefficients.Length, nameof(coefficients));
        EnsureBlock(zigzagTable.Length, nameof(zigzagTable));
        EnsureBlock(zigzagOutput.Length, nameof(zigzagOutput));

        var order = ZigZag.Order;
        for (var k = 0; k < BlockSize; k++)
        {
            var value = coefficients[order[k]] / zigzagTable[k];
            var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
            zigzagOutput[k] = (short)Math.Clamp(rounded, short.MinValue, short.MaxValue);
        }
    }

    /// <summary>
    /// Dequantizes coefficients read in zig-zag order, scattering the results into a natural-order
    /// buffer, fusing the dequantization and zig-zag reordering into a single pass. Bit-identical
    /// to <see cref="Transforms.ZigZag.ToNatural"/> followed by <see cref="Dequantize"/>.
    /// </summary>
    /// <param name="zigzagQuantized">64 quantized coefficients in zig-zag order.</param>
    /// <param name="zigzagTable">64 quantization steps permuted into zig-zag order.</param>
    /// <param name="naturalOutput">Receives the 64 dequantized coefficients in natural order.</param>
    public static void DequantizeFromZigZag(ReadOnlySpan<short> zigzagQuantized, ReadOnlySpan<ushort> zigzagTable, Span<double> naturalOutput)
    {
        EnsureBlock(zigzagQuantized.Length, nameof(zigzagQuantized));
        EnsureBlock(zigzagTable.Length, nameof(zigzagTable));
        EnsureBlock(naturalOutput.Length, nameof(naturalOutput));

        var order = ZigZag.Order;
        for (var k = 0; k < BlockSize; k++)
            naturalOutput[order[k]] = zigzagQuantized[k] * (double)zigzagTable[k];
    }

    private static void EnsureBlock(int length, string name)
    {
        if (length != BlockSize)
            throw new ArgumentException($"Block must contain exactly {BlockSize} values.", name);
    }
}

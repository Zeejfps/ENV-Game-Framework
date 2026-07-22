using System.Numerics;
using System.Runtime.CompilerServices;
using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using JpegSharp.Huffman;

namespace JpegSharp.Coding;

/// <summary>
/// Encodes and decodes a single 8x8 block of quantized coefficients (in zig-zag order) for
/// baseline sequential JPEG, following ITU-T T.81 §F.1.2: DC differential prediction plus
/// AC run-length/magnitude Huffman coding.
/// </summary>
internal static class BlockScanCoder
{
    private const int BlockSize = 64;
    private const int ZeroRunLength = 0xF0; // ZRL: 16 zeros
    private const int EndOfBlock = 0x00;    // EOB

    /// <summary>
    /// Returns the JPEG magnitude category (SSSS) for a coefficient value: the number of
    /// bits required to represent its magnitude, or 0 for zero.
    /// </summary>
    /// <param name="value">The coefficient value.</param>
    /// <returns>The magnitude category (0..).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MagnitudeCategory(int value)
    {
        var magnitude = (uint)Math.Abs(value);
        return magnitude == 0 ? 0 : BitOperations.Log2(magnitude) + 1;
    }

    /// <summary>
    /// Encodes one block and returns the DC value to use as the predictor for the next block
    /// of the same component.
    /// </summary>
    /// <param name="writer">The entropy bit writer.</param>
    /// <param name="block">64 quantized coefficients in zig-zag order.</param>
    /// <param name="dcPredictor">The previous DC value for this component.</param>
    /// <param name="dc">The DC Huffman table.</param>
    /// <param name="ac">The AC Huffman table.</param>
    /// <returns>The DC value of this block (the next predictor).</returns>
    public static int EncodeBlock(BitWriter writer, ReadOnlySpan<short> block, int dcPredictor, HuffmanTable dc, HuffmanTable ac)
    {
        var dcValue = block[0];
        var diff = dcValue - dcPredictor;
        var dcCategory = MagnitudeCategory(diff);
        dc.Encode(writer, dcCategory);
        if (dcCategory > 0)
            writer.WriteBits(Mantissa(diff, dcCategory), dcCategory);

        var run = 0;
        for (var k = 1; k < BlockSize; k++)
        {
            int coeff = block[k];
            if (coeff == 0)
            {
                run++;
                continue;
            }

            while (run > 15)
            {
                ac.Encode(writer, ZeroRunLength);
                run -= 16;
            }

            var category = MagnitudeCategory(coeff);
            ac.Encode(writer, (run << 4) | category);
            writer.WriteBits(Mantissa(coeff, category), category);
            run = 0;
        }

        if (run > 0)
            ac.Encode(writer, EndOfBlock);

        return dcValue;
    }

    /// <summary>
    /// Decodes one block into zig-zag order and returns the DC value to use as the predictor
    /// for the next block of the same component.
    /// </summary>
    /// <param name="reader">The entropy bit reader.</param>
    /// <param name="block">Destination for 64 coefficients in zig-zag order; cleared first.</param>
    /// <param name="dcPredictor">The previous DC value for this component.</param>
    /// <param name="dc">The DC Huffman table.</param>
    /// <param name="ac">The AC Huffman table.</param>
    /// <returns>The DC value of this block (the next predictor).</returns>
    /// <exception cref="JpegFormatException">The entropy data is malformed.</exception>
    public static int DecodeBlock(ref BitReader reader, scoped Span<short> block, int dcPredictor, HuffmanTable dc, HuffmanTable ac)
    {
        block.Clear();

        var dcCategory = dc.DecodeSymbol(ref reader);
        if (dcCategory is < 0 or > 16)
            throw new JpegCorruptException($"Invalid DC magnitude category {dcCategory}.");
        var diff = dcCategory == 0 ? 0 : BitReader.Extend(reader.ReadBits(dcCategory), dcCategory);
        var dcValue = dcPredictor + diff;
        if (dcValue is < short.MinValue or > short.MaxValue)
            throw new JpegCorruptException("DC coefficient out of range; corrupt entropy data.");
        block[0] = (short)dcValue;

        var k = 1;
        while (k < BlockSize)
        {
            var rs = ac.DecodeSymbol(ref reader);
            var run = rs >> 4;
            var category = rs & 0x0F;

            if (category == 0)
            {
                if (run == 15)
                {
                    k += 16;
                    if (k > BlockSize)
                        throw new JpegCorruptException("Zero-run extends past the end of the block.");
                    continue;
                }

                break; // EOB
            }

            k += run;
            if (k >= BlockSize)
                throw new JpegCorruptException("AC coefficient index out of range.");

            block[k] = (short)BitReader.Extend(reader.ReadBits(category), category);
            k++;
        }

        return dcValue;
    }

    /// <summary>
    /// Accumulates the Huffman symbol frequencies this block would emit, without writing bits.
    /// Used by the two-pass optimized-Huffman encoder.
    /// </summary>
    /// <param name="block">64 quantized coefficients in zig-zag order.</param>
    /// <param name="dcPredictor">The previous DC value for this component.</param>
    /// <param name="dcFrequencies">DC symbol frequency accumulator (length 256).</param>
    /// <param name="acFrequencies">AC symbol frequency accumulator (length 256).</param>
    /// <returns>The DC value of this block (the next predictor).</returns>
    public static int GatherBlockFrequencies(ReadOnlySpan<short> block, int dcPredictor, Span<int> dcFrequencies, Span<int> acFrequencies)
    {
        var dcValue = block[0];
        var diff = dcValue - dcPredictor;
        dcFrequencies[MagnitudeCategory(diff)]++;

        var run = 0;
        for (var k = 1; k < BlockSize; k++)
        {
            int coeff = block[k];
            if (coeff == 0)
            {
                run++;
                continue;
            }

            while (run > 15)
            {
                acFrequencies[ZeroRunLength]++;
                run -= 16;
            }

            acFrequencies[(run << 4) | MagnitudeCategory(coeff)]++;
            run = 0;
        }

        if (run > 0)
            acFrequencies[EndOfBlock]++;

        return dcValue;
    }

    /// <summary>
    /// Computes the magnitude bits transmitted for a nonzero coefficient value: the low
    /// <paramref name="category"/> bits of the value, with negative values stored as
    /// (value - 1).
    /// </summary>
    /// <param name="value">The coefficient value.</param>
    /// <param name="category">The magnitude category (bit length).</param>
    /// <returns>The magnitude bits to transmit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Mantissa(int value, int category)
    {
        // Negative values are stored as (value - 1) in the low `category` bits.
        var adjusted = value < 0 ? value - 1 : value;
        return adjusted & ((1 << category) - 1);
    }
}

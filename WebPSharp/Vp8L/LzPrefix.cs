using System.Numerics;

namespace WebPSharp.Vp8L;

/// <summary>
/// The VP8L prefix-value coding used for LZ77 copy lengths and distances. A value ≥ 1 is split
/// into a small prefix code (transmitted through a Huffman code) and a number of raw "extra" bits.
/// This is distinct from the Huffman prefix codes themselves: here the prefix code selects a
/// magnitude bucket and the extra bits pick the value within it.
/// </summary>
internal static class LzPrefix
{
    /// <summary>Decodes a length or distance value from its prefix code and extra bits.</summary>
    /// <param name="prefixCode">The prefix code (the Huffman symbol, offset already removed).</param>
    /// <param name="reader">The bit reader positioned at the extra bits.</param>
    /// <returns>The decoded value (≥ 1).</returns>
    public static int Decode(int prefixCode, ref Vp8LBitReader reader)
    {
        if (prefixCode < 4)
            return prefixCode + 1;

        var extraBits = (prefixCode - 2) >> 1;
        var offset = (2 + (prefixCode & 1)) << extraBits;
        return offset + (int)reader.ReadBits(extraBits) + 1;
    }

    /// <summary>Encodes a length or distance value into a prefix code and extra bits.</summary>
    /// <param name="value">The value to encode (≥ 1).</param>
    /// <param name="prefixCode">Receives the prefix code (the Huffman symbol).</param>
    /// <param name="extraBits">Receives the number of extra bits.</param>
    /// <param name="extraValue">Receives the raw extra-bit payload.</param>
    public static void Encode(int value, out int prefixCode, out int extraBits, out int extraValue)
    {
        if (value <= 4)
        {
            prefixCode = value - 1;
            extraBits = 0;
            extraValue = 0;
            return;
        }

        var d = value - 1;
        var highestBit = BitOperations.Log2((uint)d);
        extraBits = highestBit - 1;
        var secondBit = (d >> extraBits) & 1;
        prefixCode = 2 * highestBit + secondBit;
        var offset = (2 + (prefixCode & 1)) << extraBits;
        extraValue = d - offset;
    }
}

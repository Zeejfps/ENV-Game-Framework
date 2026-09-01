using WebPSharp.Api.Exceptions;

namespace WebPSharp.Vp8L;

/// <summary>
/// Canonical prefix (Huffman) code construction shared by the VP8L decoder and encoder. Given a
/// per-symbol code-length table it assigns most-significant-bit-first canonical codes and
/// validates that the code is neither over- nor under-subscribed (the single-symbol degenerate
/// case aside).
/// </summary>
internal static class PrefixCode
{
    /// <summary>The maximum code length permitted for a VP8L prefix code.</summary>
    public const int MaxCodeLength = 15;

    /// <summary>
    /// Computes canonical codes for the given lengths.
    /// </summary>
    /// <param name="lengths">Per-symbol code lengths; zero means the symbol is unused.</param>
    /// <param name="singleSymbol">
    /// Receives the sole used symbol when exactly one symbol is used (a zero-bit code), or -1.
    /// </param>
    /// <returns>Per-symbol canonical codes (MSB-first); unused symbols hold zero.</returns>
    /// <exception cref="WebPCorruptException">
    /// A length is out of range, the alphabet is empty, or the code is over/under-subscribed.
    /// </exception>
    public static int[] BuildCanonicalCodes(ReadOnlySpan<int> lengths, out int singleSymbol)
    {
        Span<int> blCount = stackalloc int[MaxCodeLength + 1];
        var maxLength = 0;
        var usedCount = 0;
        singleSymbol = -1;

        for (var s = 0; s < lengths.Length; s++)
        {
            var len = lengths[s];
            if (len < 0 || len > MaxCodeLength)
                throw new WebPCorruptException($"Prefix code length {len} for symbol {s} is out of range (0..{MaxCodeLength}).");
            if (len > 0)
            {
                blCount[len]++;
                usedCount++;
                singleSymbol = s;
                if (len > maxLength)
                    maxLength = len;
            }
        }

        if (usedCount == 0)
            throw new WebPCorruptException("Prefix code has no used symbols.");

        if (usedCount != 1)
            singleSymbol = -1;

        // Kraft inequality check: track remaining code space at each length.
        var leftover = 1;
        for (var len = 1; len <= maxLength; len++)
        {
            leftover <<= 1;
            leftover -= blCount[len];
            if (leftover < 0)
                throw new WebPCorruptException("Prefix code is over-subscribed.");
        }

        if (leftover > 0 && usedCount != 1)
            throw new WebPCorruptException("Prefix code is incomplete (under-subscribed).");

        Span<int> nextCode = stackalloc int[MaxCodeLength + 1];
        var code = 0;
        for (var len = 1; len <= maxLength; len++)
        {
            code = (code + blCount[len - 1]) << 1;
            nextCode[len] = code;
        }

        var codes = new int[lengths.Length];
        for (var s = 0; s < lengths.Length; s++)
        {
            var len = lengths[s];
            if (len > 0)
                codes[s] = nextCode[len]++;
        }

        return codes;
    }
}

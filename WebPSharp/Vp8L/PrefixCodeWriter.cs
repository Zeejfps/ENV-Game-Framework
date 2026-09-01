namespace WebPSharp.Vp8L;

/// <summary>
/// Emits canonical VP8L prefix (Huffman) codes to a <see cref="Vp8LBitWriter"/>. Constructed from
/// the same per-symbol code-length table the decoder consumes, it writes each symbol's canonical
/// code most-significant-bit first so that <see cref="HuffmanTree.ReadSymbol"/> reads it back
/// exactly. A single-symbol code writes no bits.
/// </summary>
internal sealed class PrefixCodeWriter
{
    private readonly int[] _codes;
    private readonly int[] _lengths;
    private readonly int _singleSymbol;

    /// <summary>Builds a writer for the given code-length table.</summary>
    /// <param name="lengths">Per-symbol code lengths; zero means unused.</param>
    public PrefixCodeWriter(ReadOnlySpan<int> lengths)
    {
        _codes = PrefixCode.BuildCanonicalCodes(lengths, out _singleSymbol);
        _lengths = lengths.ToArray();
    }

    /// <summary>Whether this code writes no bits because the alphabet has a single symbol.</summary>
    public bool IsSingleSymbol => _singleSymbol >= 0;

    /// <summary>Writes the canonical code for <paramref name="symbol"/>.</summary>
    /// <param name="writer">The destination bit writer.</param>
    /// <param name="symbol">The symbol to emit.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="symbol"/> has no assigned code.</exception>
    public void WriteSymbol(Vp8LBitWriter writer, int symbol)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (_singleSymbol >= 0)
        {
            if (symbol != _singleSymbol)
                throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Symbol is not present in this single-symbol code.");
            return;
        }

        var len = _lengths[symbol];
        if (len == 0)
            throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Symbol has no assigned prefix code.");

        var code = _codes[symbol];
        // Emit most-significant bit first so the decoder descends the tree correctly.
        for (var bitIndex = len - 1; bitIndex >= 0; bitIndex--)
            writer.PutBit((uint)((code >> bitIndex) & 1));
    }
}

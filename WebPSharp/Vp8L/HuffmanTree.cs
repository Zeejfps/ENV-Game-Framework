using WebPSharp.Api.Exceptions;

namespace WebPSharp.Vp8L;

/// <summary>
/// A binary-tree decoder for a canonical VP8L prefix (Huffman) code. Symbols are decoded by
/// reading one stream bit at a time and descending from the root until a leaf is reached; the
/// first bit read corresponds to the most-significant bit of the canonical code. A degenerate
/// single-symbol alphabet decodes without consuming any bits.
/// </summary>
internal sealed class HuffmanTree
{
    // Parallel node arrays. For an internal node, Symbol is -1 and Left/Right index child nodes.
    // For a leaf, Symbol is the alphabet symbol and the children are unused.
    private readonly int[] _left;
    private readonly int[] _right;
    private readonly int[] _symbol;
    private readonly int _rootSymbol; // >= 0 for a zero-bit single-symbol tree, else -1.

    private HuffmanTree(int[] left, int[] right, int[] symbol, int rootSymbol)
    {
        _left = left;
        _right = right;
        _symbol = symbol;
        _rootSymbol = rootSymbol;
    }

    /// <summary>Builds a tree from a per-symbol code-length table.</summary>
    /// <param name="lengths">Per-symbol code lengths; zero means unused.</param>
    /// <returns>The decoder tree.</returns>
    /// <exception cref="WebPCorruptException">The lengths do not describe a valid prefix code.</exception>
    public static HuffmanTree FromCodeLengths(ReadOnlySpan<int> lengths)
    {
        var codes = PrefixCode.BuildCanonicalCodes(lengths, out var singleSymbol);
        if (singleSymbol >= 0)
            return FromSingleSymbol(singleSymbol, lengths.Length);

        // Upper bound on node count: one root plus two per used symbol is ample.
        var capacity = 1;
        for (var s = 0; s < lengths.Length; s++)
            if (lengths[s] > 0)
                capacity += 2 * lengths[s];

        var left = new int[capacity];
        var right = new int[capacity];
        var symbol = new int[capacity];
        Array.Fill(left, -1);
        Array.Fill(right, -1);
        Array.Fill(symbol, -1);

        var nodeCount = 1; // node 0 is the root
        for (var s = 0; s < lengths.Length; s++)
        {
            var len = lengths[s];
            if (len == 0)
                continue;

            var code = codes[s];
            var node = 0;
            for (var bitIndex = len - 1; bitIndex >= 0; bitIndex--)
            {
                var bit = (code >> bitIndex) & 1;
                ref var child = ref (bit == 0 ? ref left[node] : ref right[node]);
                if (bitIndex == 0)
                {
                    if (child != -1)
                        throw new WebPCorruptException("Prefix code assigns two symbols to the same code.");
                    child = nodeCount;
                    symbol[nodeCount] = s;
                    nodeCount++;
                }
                else
                {
                    if (child == -1)
                    {
                        child = nodeCount;
                        nodeCount++;
                    }
                    else if (symbol[child] != -1)
                    {
                        throw new WebPCorruptException("Prefix code is not prefix-free.");
                    }
                    node = child;
                }
            }
        }

        return new HuffmanTree(left, right, symbol, rootSymbol: -1);
    }

    /// <summary>Builds a zero-bit tree that always decodes to <paramref name="symbol"/>.</summary>
    /// <param name="symbol">The only symbol in the alphabet.</param>
    /// <param name="alphabetSize">The alphabet size (for validation).</param>
    /// <returns>The single-symbol decoder tree.</returns>
    /// <exception cref="WebPCorruptException"><paramref name="symbol"/> is outside the alphabet.</exception>
    public static HuffmanTree FromSingleSymbol(int symbol, int alphabetSize)
    {
        if ((uint)symbol >= (uint)alphabetSize)
            throw new WebPCorruptException($"Single prefix-code symbol {symbol} is outside the alphabet size {alphabetSize}.");
        return new HuffmanTree(new[] { -1 }, new[] { -1 }, new[] { symbol }, symbol);
    }

    /// <summary>Whether this tree decodes without consuming any bits.</summary>
    public bool IsSingleSymbol => _rootSymbol >= 0;

    /// <summary>Decodes the next symbol from the reader.</summary>
    /// <param name="reader">The bit reader positioned at a code.</param>
    /// <returns>The decoded alphabet symbol.</returns>
    /// <exception cref="WebPCorruptException">The bitstream walks off the code tree.</exception>
    public int ReadSymbol(ref Vp8LBitReader reader)
    {
        var node = 0;
        var sym = _symbol[node];
        while (sym < 0)
        {
            var bit = reader.ReadBit();
            node = bit == 0 ? _left[node] : _right[node];
            if (node < 0)
                throw new WebPCorruptException("VP8L bitstream does not match any prefix code.");
            sym = _symbol[node];
        }
        return sym;
    }
}

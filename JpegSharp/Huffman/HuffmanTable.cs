using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;

namespace JpegSharp.Huffman;

/// <summary>
/// A canonical JPEG Huffman table built from the DHT representation: 16 code-length counts
/// (BITS) and the ordered list of symbols (HUFFVAL). The table supports both decoding
/// (symbol lookup from a <see cref="BitReader"/>) and encoding (code/size lookup per symbol),
/// following ITU-T T.81 Annex C and Annex F.
/// </summary>
public sealed class HuffmanTable
{
    private const int MaxLength = 16;
    private const int MaxSymbols = 256;

    private readonly byte[] _counts;   // BITS: number of codes of each length 1..16
    private readonly byte[] _symbols;  // HUFFVAL in canonical order

    // Decoder acceleration tables (indexed by code length 1..16).
    private readonly int[] _minCode = new int[MaxLength + 1];
    private readonly int[] _maxCode = new int[MaxLength + 1];
    private readonly int[] _valPtr = new int[MaxLength + 1];

    // Encoder tables (indexed by symbol value 0..255).
    private readonly int[] _codes = new int[MaxSymbols];
    private readonly byte[] _sizes = new byte[MaxSymbols];

    // Decode lookahead: for any 8-bit prefix, the symbol and code length when the code fits
    // within LookaheadBits; otherwise size 0 (the slow canonical path handles longer codes).
    private const int LookaheadBits = 8;
    private readonly short[] _lookaheadSymbol = new short[1 << LookaheadBits];
    private readonly byte[] _lookaheadSize = new byte[1 << LookaheadBits];

    /// <summary>
    /// Builds a Huffman table from its DHT representation.
    /// </summary>
    /// <param name="counts">Exactly 16 entries: the number of codes of each length 1..16.</param>
    /// <param name="symbols">The symbols in canonical order; its length must equal the sum of <paramref name="counts"/>.</param>
    /// <exception cref="ArgumentException">
    /// The counts span is not 16 long, the symbol count does not match, or the table is
    /// oversubscribed (declares more codes of a length than that length can represent).
    /// </exception>
    public HuffmanTable(ReadOnlySpan<byte> counts, ReadOnlySpan<byte> symbols)
    {
        if (counts.Length != MaxLength)
            throw new ArgumentException($"Counts must contain exactly {MaxLength} entries.", nameof(counts));

        var total = 0;
        for (var i = 0; i < MaxLength; i++)
            total += counts[i];

        if (total != symbols.Length)
            throw new ArgumentException("The number of symbols must equal the sum of the code-length counts.", nameof(symbols));
        if (total == 0)
            throw new ArgumentException("A Huffman table must define at least one code.", nameof(symbols));

        _counts = counts.ToArray();
        _symbols = symbols.ToArray();

        Build();
    }

    /// <summary>
    /// Builds an entropy-optimal, length-limited (max 16-bit) canonical Huffman table from
    /// per-symbol frequencies, following the procedure in ITU-T T.81 Annex K.2.
    /// </summary>
    /// <param name="frequencies">
    /// Exactly 256 symbol occurrence counts. Symbols with zero frequency are omitted from
    /// the resulting table.
    /// </param>
    /// <returns>An optimized Huffman table for the given distribution.</returns>
    /// <exception cref="ArgumentException">The span is not exactly 256 elements.</exception>
    public static HuffmanTable BuildOptimized(ReadOnlySpan<int> frequencies)
    {
        if (frequencies.Length != MaxSymbols)
            throw new ArgumentException($"Frequencies must contain exactly {MaxSymbols} entries.", nameof(frequencies));

        // One reserved code point (index 256) guarantees the all-ones code is never assigned
        // to a real symbol, and that at least one combining step always occurs.
        const int size = MaxSymbols + 1;
        Span<long> freq = stackalloc long[size];
        for (var i = 0; i < MaxSymbols; i++)
            freq[i] = frequencies[i];
        freq[MaxSymbols] = 1;

        Span<int> codeSize = stackalloc int[size];
        Span<int> others = stackalloc int[size];
        others.Fill(-1);

        BuildCodeSizes(freq, codeSize, others);

        // Count codes of each length (may exceed 16 before limiting).
        Span<int> bits = stackalloc int[size + 1];
        for (var i = 0; i < size; i++)
            if (codeSize[i] > 0)
                bits[codeSize[i]]++;

        LimitCodeLengths(bits);

        // Remove the reserved code point from the longest used length.
        var last = MaxLength;
        while (bits[last] == 0)
            last--;
        bits[last]--;

        Span<byte> counts = stackalloc byte[MaxLength];
        var total = 0;
        for (var length = 1; length <= MaxLength; length++)
        {
            counts[length - 1] = (byte)bits[length];
            total += bits[length];
        }

        Span<byte> symbols = total <= 256 ? stackalloc byte[total] : new byte[total];
        var p = 0;
        for (var length = 1; length < size; length++)
            for (var symbol = 0; symbol < MaxSymbols; symbol++)
                if (codeSize[symbol] == length)
                    symbols[p++] = (byte)symbol;

        return new HuffmanTable(counts, symbols);
    }

    private static void BuildCodeSizes(Span<long> freq, Span<int> codeSize, Span<int> others)
    {
        while (true)
        {
            // c1 = least-frequency entry (largest index on ties).
            var c1 = -1;
            var v = long.MaxValue;
            for (var i = 0; i < freq.Length; i++)
                if (freq[i] > 0 && freq[i] <= v)
                {
                    v = freq[i];
                    c1 = i;
                }

            // c2 = next least-frequency entry, distinct from c1.
            var c2 = -1;
            v = long.MaxValue;
            for (var i = 0; i < freq.Length; i++)
                if (freq[i] > 0 && freq[i] <= v && i != c1)
                {
                    v = freq[i];
                    c2 = i;
                }

            if (c2 < 0)
                break;

            freq[c1] += freq[c2];
            freq[c2] = 0;

            codeSize[c1]++;
            while (others[c1] >= 0)
            {
                c1 = others[c1];
                codeSize[c1]++;
            }
            others[c1] = c2;

            codeSize[c2]++;
            while (others[c2] >= 0)
            {
                c2 = others[c2];
                codeSize[c2]++;
            }
        }
    }

    private static void LimitCodeLengths(Span<int> bits)
    {
        for (var i = bits.Length - 1; i > MaxLength; i--)
        {
            while (bits[i] > 0)
            {
                var j = i - 2;
                while (bits[j] == 0)
                    j--;

                bits[i] -= 2;
                bits[i - 1] += 1;
                bits[j + 1] += 2;
                bits[j] -= 1;
            }
        }
    }

    /// <summary>Gets the 16 code-length counts (BITS), for writing a DHT segment.</summary>
    public ReadOnlySpan<byte> Counts => _counts;

    /// <summary>Gets the ordered symbols (HUFFVAL), for writing a DHT segment.</summary>
    public ReadOnlySpan<byte> Symbols => _symbols;

    /// <summary>
    /// Decodes the next symbol from the bit reader using the canonical DECODE procedure
    /// (ITU-T T.81 Figure F.16).
    /// </summary>
    /// <param name="reader">The entropy bit reader.</param>
    /// <returns>The decoded symbol value.</returns>
    /// <exception cref="JpegFormatException">The bitstream does not contain a valid code.</exception>
    internal int DecodeSymbol(ref BitReader reader)
    {
        // Fast path: an 8-bit lookahead resolves the common short codes in one step.
        var peek = reader.PeekByte();
        var size = _lookaheadSize[peek];
        if (size != 0)
        {
            reader.SkipBits(size);
            return _lookaheadSymbol[peek];
        }

        // Slow path: codes longer than the lookahead window (canonical DECODE, F.16).
        var code = reader.ReadBits(1);
        var length = 1;
        while (code > _maxCode[length])
        {
            if (length >= MaxLength)
                throw new JpegCorruptException("Invalid Huffman code in entropy data.");
            code = (code << 1) | reader.ReadBits(1);
            length++;
        }

        var index = _valPtr[length] + (code - _minCode[length]);
        return _symbols[index];
    }

    /// <summary>
    /// Retrieves the canonical code and its bit length for the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol value (0..255).</param>
    /// <param name="code">The canonical Huffman code.</param>
    /// <param name="size">The number of bits in the code.</param>
    /// <exception cref="ArgumentException">The symbol is not present in the table.</exception>
    public void GetCode(int symbol, out int code, out int size)
    {
        if ((uint)symbol >= MaxSymbols || _sizes[symbol] == 0)
            throw new ArgumentException($"Symbol {symbol} is not present in this Huffman table.", nameof(symbol));
        code = _codes[symbol];
        size = _sizes[symbol];
    }

    /// <summary>Writes the canonical code for the given symbol to the bit writer.</summary>
    /// <param name="writer">The entropy bit writer.</param>
    /// <param name="symbol">The symbol value to encode.</param>
    internal void Encode(BitWriter writer, int symbol)
    {
        GetCode(symbol, out var code, out var size);
        writer.WriteBits(code, size);
    }

    private void Build()
    {
        // Generate HUFFSIZE / HUFFCODE (Annex C.2) and populate encode + decode tables.
        Span<byte> huffSize = _symbols.Length <= 256 ? stackalloc byte[_symbols.Length] : new byte[_symbols.Length];
        var k = 0;
        for (var length = 1; length <= MaxLength; length++)
            for (var i = 0; i < _counts[length - 1]; i++)
                huffSize[k++] = (byte)length;

        var code = 0;
        var si = huffSize.Length > 0 ? huffSize[0] : 0;
        var p = 0;
        while (p < huffSize.Length)
        {
            while (p < huffSize.Length && huffSize[p] == si)
            {
                if (code >= 1 << si)
                    throw new ArgumentException("Huffman table is oversubscribed.");
                var symbol = _symbols[p];
                _codes[symbol] = code;
                _sizes[symbol] = (byte)si;
                code++;
                p++;
            }
            code <<= 1;
            si++;
        }

        // Build decode acceleration tables (Annex F.2.2.3).
        p = 0;
        for (var length = 1; length <= MaxLength; length++)
        {
            if (_counts[length - 1] == 0)
            {
                _maxCode[length] = -1;
                continue;
            }

            _valPtr[length] = p;
            _minCode[length] = FirstCodeOfLength(length);
            p += _counts[length - 1];
            _maxCode[length] = _minCode[length] + _counts[length - 1] - 1;
        }

        BuildLookahead();
    }

    private void BuildLookahead()
    {
        for (var symbol = 0; symbol < MaxSymbols; symbol++)
        {
            var length = _sizes[symbol];
            if (length is 0 or > LookaheadBits)
                continue;

            // Every 8-bit prefix beginning with this code maps to this symbol.
            var shift = LookaheadBits - length;
            var start = _codes[symbol] << shift;
            var count = 1 << shift;
            for (var i = 0; i < count; i++)
            {
                _lookaheadSymbol[start + i] = (short)symbol;
                _lookaheadSize[start + i] = length;
            }
        }
    }

    private int FirstCodeOfLength(int length)
    {
        // Recompute the first canonical code for a given length from the counts.
        var code = 0;
        for (var l = 1; l < length; l++)
            code = (code + _counts[l - 1]) << 1;
        return code;
    }
}

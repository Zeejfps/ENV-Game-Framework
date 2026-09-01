using WebPSharp.Api.Exceptions;

namespace WebPSharp.Vp8L;

/// <summary>
/// Reads and writes a complete VP8L prefix-code description from/to the bitstream. This covers
/// both encodings defined by the specification: the "simple" form (one or two explicit symbols)
/// and the "normal" form, whose per-symbol code lengths are themselves compressed with a small
/// 19-symbol code-length code that supports run-length repeats.
/// </summary>
internal static class Vp8LHuffman
{
    /// <summary>The number of symbols in the code-length code alphabet.</summary>
    public const int CodeLengthCodes = 19;

    // The order in which code-length-code lengths are transmitted, most useful first.
    private static readonly int[] CodeLengthCodeOrder =
        { 17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

    // Repeat symbols 16/17/18 carry this many extra bits and this base repeat count.
    private static readonly int[] RepeatExtraBits = { 2, 3, 7 };
    private static readonly int[] RepeatOffsets = { 3, 3, 11 };

    private const int DefaultCodeLength = 8;

    /// <summary>Reads a prefix-code description and returns the decoder tree.</summary>
    /// <param name="reader">The bit reader positioned at the description.</param>
    /// <param name="alphabetSize">The number of symbols in the target alphabet.</param>
    /// <returns>The decoder tree for the described code.</returns>
    /// <exception cref="WebPCorruptException">The description is malformed.</exception>
    public static HuffmanTree ReadPrefixCode(ref Vp8LBitReader reader, int alphabetSize)
    {
        var isSimple = reader.ReadBit() != 0;
        return isSimple
            ? ReadSimpleCode(ref reader, alphabetSize)
            : ReadNormalCode(ref reader, alphabetSize);
    }

    private static HuffmanTree ReadSimpleCode(ref Vp8LBitReader reader, int alphabetSize)
    {
        var numSymbols = (int)reader.ReadBits(1) + 1;
        var isFirst8Bits = reader.ReadBits(1);
        var symbol0 = (int)reader.ReadBits(isFirst8Bits == 1 ? 8 : 1);
        if (symbol0 >= alphabetSize)
            throw new WebPCorruptException($"Simple prefix code symbol {symbol0} exceeds alphabet size {alphabetSize}.");

        if (numSymbols == 1)
            return HuffmanTree.FromSingleSymbol(symbol0, alphabetSize);

        var symbol1 = (int)reader.ReadBits(8);
        if (symbol1 >= alphabetSize)
            throw new WebPCorruptException($"Simple prefix code symbol {symbol1} exceeds alphabet size {alphabetSize}.");

        var lengths = new int[alphabetSize];
        lengths[symbol0] = 1;
        lengths[symbol1] = 1;
        return HuffmanTree.FromCodeLengths(lengths);
    }

    private static HuffmanTree ReadNormalCode(ref Vp8LBitReader reader, int alphabetSize)
    {
        var codeLengthCodeLengths = new int[CodeLengthCodes];
        var numCodes = (int)reader.ReadBits(4) + 4;
        if (numCodes > CodeLengthCodes)
            throw new WebPCorruptException($"Code-length-code count {numCodes} exceeds {CodeLengthCodes}.");

        for (var i = 0; i < numCodes; i++)
            codeLengthCodeLengths[CodeLengthCodeOrder[i]] = (int)reader.ReadBits(3);

        var codeLengthTree = HuffmanTree.FromCodeLengths(codeLengthCodeLengths);
        var lengths = ReadCodeLengths(ref reader, codeLengthTree, alphabetSize);
        return HuffmanTree.FromCodeLengths(lengths);
    }

    private static int[] ReadCodeLengths(ref Vp8LBitReader reader, HuffmanTree codeLengthTree, int alphabetSize)
    {
        var codeLengths = new int[alphabetSize];
        int maxSymbol;
        if (reader.ReadBit() != 0)
        {
            var lengthNBits = 2 + 2 * (int)reader.ReadBits(3);
            maxSymbol = 2 + (int)reader.ReadBits(lengthNBits);
            if (maxSymbol > alphabetSize)
                throw new WebPCorruptException($"Code-length max symbol {maxSymbol} exceeds alphabet size {alphabetSize}.");
        }
        else
        {
            maxSymbol = alphabetSize;
        }

        var symbol = 0;
        var prevCodeLength = DefaultCodeLength;
        while (symbol < alphabetSize)
        {
            if (maxSymbol-- == 0)
                break;

            var codeLength = codeLengthTree.ReadSymbol(ref reader);
            if (codeLength < 16)
            {
                codeLengths[symbol++] = codeLength;
                if (codeLength != 0)
                    prevCodeLength = codeLength;
            }
            else
            {
                var slot = codeLength - 16;
                var usePrevious = codeLength == 16;
                var repeat = (int)reader.ReadBits(RepeatExtraBits[slot]) + RepeatOffsets[slot];
                if (symbol + repeat > alphabetSize)
                    throw new WebPCorruptException("Code-length repeat overruns the alphabet.");

                var value = usePrevious ? prevCodeLength : 0;
                while (repeat-- > 0)
                    codeLengths[symbol++] = value;
            }
        }

        return codeLengths;
    }

    /// <summary>
    /// Writes a prefix-code description in the normal form. The code-length code is emitted as a
    /// fixed complete 16-symbol/length-4 code (covering every possible length value 0..15), and
    /// each symbol length is written literally without run-length repeats. This is fully valid and
    /// deterministic; denser code-length coding is an optional later optimization.
    /// </summary>
    /// <param name="writer">The destination bit writer.</param>
    /// <param name="lengths">The per-symbol code lengths of the target alphabet.</param>
    public static void WritePrefixCode(Vp8LBitWriter writer, ReadOnlySpan<int> lengths)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.PutBit(0); // normal (not simple) code

        // Fixed code-length code: values 0..15 each get length 4 (a complete 16-leaf code).
        Span<int> codeLengthCodeLengths = stackalloc int[CodeLengthCodes];
        for (var v = 0; v < 16; v++)
            codeLengthCodeLengths[v] = 4;

        writer.PutBits(CodeLengthCodes - 4, 4); // num_codes - 4, i.e. all 19 entries
        for (var i = 0; i < CodeLengthCodes; i++)
            writer.PutBits((uint)codeLengthCodeLengths[CodeLengthCodeOrder[i]], 3);

        var codeLengthWriter = new PrefixCodeWriter(codeLengthCodeLengths);

        writer.PutBit(0); // use_length == 0 -> read the full alphabet

        foreach (var length in lengths)
            codeLengthWriter.WriteSymbol(writer, length);
    }
}

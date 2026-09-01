namespace WebPSharp.Vp8;

/// <summary>
/// The VP8 boolean (binary arithmetic) entropy decoder from RFC 6386. Reads bits against 8-bit
/// probabilities from a compressed partition. Reads past the end of the partition yield zero bytes
/// so truncated partitions degrade gracefully rather than throwing mid-bit.
/// </summary>
/// <remarks>
/// A reference type so multiple partitions (macroblock-header partition plus one or more DCT
/// coefficient partitions) can be held alongside one another and advanced independently during the
/// macroblock loop.
/// </remarks>
public sealed class Vp8BooleanDecoder
{
    private readonly byte[] _input;
    private readonly int _end;
    private int _pos;
    private uint _value;
    private uint _range;
    private int _bitCount;
    private bool _eos;

    /// <summary>Creates a decoder over an entire partition buffer.</summary>
    /// <param name="data">The partition bytes.</param>
    public Vp8BooleanDecoder(byte[] data) : this(data, 0, (data ?? throw new ArgumentNullException(nameof(data))).Length)
    {
    }

    /// <summary>Creates a decoder over a sub-range of a buffer.</summary>
    /// <param name="data">The backing buffer.</param>
    /// <param name="start">The start offset of the partition.</param>
    /// <param name="length">The partition length in bytes.</param>
    public Vp8BooleanDecoder(byte[] data, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(data);
        _input = data;
        _pos = start;
        _end = start + length;
        _value = (uint)((NextByte() << 8) | NextByte());
        _range = 255;
        _bitCount = 0;
        _eos = false;
    }

    /// <summary>
    /// Whether a read has requested more bytes than the partition could supply. Once set it remains
    /// set for the lifetime of the decoder.
    /// </summary>
    public bool IsEndOfStream => _eos;

    private byte NextByte()
    {
        if (_pos < _end)
            return _input[_pos++];
        _eos = true;
        return 0;
    }

    /// <summary>Decodes a single boolean against the given probability.</summary>
    /// <param name="probability">The probability of a zero bit, 1..255.</param>
    /// <returns>The decoded bit, 0 or 1.</returns>
    public int GetBit(int probability)
    {
        var split = 1u + (((_range - 1) * (uint)probability) >> 8);
        var bigSplit = split << 8;

        int result;
        if (_value >= bigSplit)
        {
            result = 1;
            _range -= split;
            _value -= bigSplit;
        }
        else
        {
            result = 0;
            _range = split;
        }

        while (_range < 128)
        {
            _value <<= 1;
            _range <<= 1;
            if (++_bitCount == 8)
            {
                _bitCount = 0;
                _value |= NextByte();
            }
        }

        return result;
    }

    /// <summary>Decodes a single boolean with uniform (1/2) probability.</summary>
    /// <returns>The decoded bit, 0 or 1.</returns>
    public int GetBitUniform() => GetBit(128);

    /// <summary>Decodes an unsigned literal of <paramref name="bits"/> bits, most-significant first.</summary>
    /// <param name="bits">The number of bits (0..32).</param>
    /// <returns>The decoded value.</returns>
    public uint GetLiteral(int bits)
    {
        uint value = 0;
        while (bits-- > 0)
            value = (value << 1) | (uint)GetBit(128);
        return value;
    }

    /// <summary>Decodes a sign-magnitude signed literal: a magnitude then a sign bit.</summary>
    /// <param name="bits">The number of magnitude bits.</param>
    /// <returns>The decoded signed value.</returns>
    public int GetSigned(int bits)
    {
        var magnitude = (int)GetLiteral(bits);
        return GetBit(128) != 0 ? -magnitude : magnitude;
    }

    /// <summary>Applies a sign bit to an already-decoded magnitude (used for coefficient values).</summary>
    /// <param name="value">The non-negative magnitude.</param>
    /// <returns>The signed value.</returns>
    public int ApplySign(int value) => GetBit(128) != 0 ? -value : value;

    /// <summary>Reads an optionally-present signed value: a flag, then magnitude + sign if the flag is set.</summary>
    /// <param name="bits">The number of magnitude bits.</param>
    /// <returns>The value, or 0 when the presence flag is clear.</returns>
    public int GetOptionalSigned(int bits) => GetBit(128) != 0 ? GetSigned(bits) : 0;
}

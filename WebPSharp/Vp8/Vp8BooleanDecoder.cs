namespace WebPSharp.Vp8;

/// <summary>
/// The VP8 boolean (binary arithmetic) entropy decoder from RFC 6386. Reads bits against 8-bit
/// probabilities from a compressed partition. Reads past the end of the buffer yield zero bytes so
/// truncated partitions degrade gracefully rather than throwing mid-bit.
/// </summary>
/// <remarks>
/// A mutable <see langword="ref struct"/>: it holds a <see cref="ReadOnlySpan{T}"/> over the
/// partition and must not be copied mid-decode.
/// </remarks>
public ref struct Vp8BooleanDecoder
{
    private readonly ReadOnlySpan<byte> _input;
    private int _pos;
    private uint _value;
    private uint _range;
    private int _bitCount;

    /// <summary>Creates a decoder over a compressed partition.</summary>
    /// <param name="data">The partition bytes.</param>
    public Vp8BooleanDecoder(ReadOnlySpan<byte> data)
    {
        _input = data;
        _pos = 0;
        _value = (uint)((NextByte() << 8) | NextByte());
        _range = 255;
        _bitCount = 0;
    }

    private byte NextByte() => _pos < _input.Length ? _input[_pos++] : (byte)0;

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

    /// <summary>Reads an optionally-present signed value: a flag, then magnitude + sign if the flag is set.</summary>
    /// <param name="bits">The number of magnitude bits.</param>
    /// <returns>The value, or 0 when the presence flag is clear.</returns>
    public int GetOptionalSigned(int bits) => GetBit(128) != 0 ? GetSigned(bits) : 0;
}

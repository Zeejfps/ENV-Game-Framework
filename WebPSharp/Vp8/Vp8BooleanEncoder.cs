namespace WebPSharp.Vp8;

/// <summary>
/// The VP8 boolean (binary arithmetic) entropy encoder from RFC 6386. Writes bits against 8-bit
/// probabilities, producing a compressed partition that <see cref="Vp8BooleanDecoder"/> reads back
/// exactly. Carry propagation into already-emitted bytes is handled per the reference algorithm.
/// </summary>
public sealed class Vp8BooleanEncoder
{
    private byte[] _buffer;
    private int _pos;
    private uint _range;
    private uint _bottom;
    private int _bitCount;
    private bool _finished;

    /// <summary>Creates an encoder with an optional initial capacity hint.</summary>
    /// <param name="initialCapacity">The initial byte capacity.</param>
    public Vp8BooleanEncoder(int initialCapacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        _buffer = new byte[Math.Max(initialCapacity, 16)];
        _range = 255;
        _bottom = 0;
        _bitCount = 24;
    }

    /// <summary>Encodes a single boolean against the given probability.</summary>
    /// <param name="probability">The probability of a zero bit, 1..255.</param>
    /// <param name="bit">The bit to encode, 0 or 1.</param>
    public void PutBit(int probability, int bit)
    {
        if (_finished)
            throw new InvalidOperationException("The boolean encoder has already been finished.");

        var split = 1u + (((_range - 1) * (uint)probability) >> 8);
        if (bit != 0)
        {
            _bottom += split;
            _range -= split;
        }
        else
        {
            _range = split;
        }

        while (_range < 128)
        {
            _range <<= 1;
            if ((_bottom & 0x80000000u) != 0)
                AddCarry();
            _bottom <<= 1;
            if (--_bitCount == 0)
            {
                Append((byte)(_bottom >> 24));
                _bottom &= (1u << 24) - 1;
                _bitCount = 8;
            }
        }
    }

    /// <summary>Encodes a single boolean with uniform (1/2) probability.</summary>
    /// <param name="bit">The bit to encode, 0 or 1.</param>
    public void PutBitUniform(int bit) => PutBit(128, bit);

    /// <summary>Encodes an unsigned literal of <paramref name="bits"/> bits, most-significant first.</summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="bits">The number of bits (0..32).</param>
    public void PutLiteral(uint value, int bits)
    {
        for (var i = bits - 1; i >= 0; i--)
            PutBit(128, (int)((value >> i) & 1));
    }

    /// <summary>Encodes a sign-magnitude signed literal: a magnitude then a sign bit.</summary>
    /// <param name="value">The signed value.</param>
    /// <param name="bits">The number of magnitude bits.</param>
    public void PutSigned(int value, int bits)
    {
        var magnitude = value < 0 ? -value : value;
        PutLiteral((uint)magnitude, bits);
        PutBit(128, value < 0 ? 1 : 0);
    }

    /// <summary>Flushes remaining state and returns the encoded partition bytes.</summary>
    /// <returns>The compressed partition.</returns>
    public byte[] Finish()
    {
        if (!_finished)
        {
            _finished = true;
            var c = _bitCount;
            var v = _bottom;
            if ((v & (1u << (32 - c))) != 0)
                AddCarry();
            v <<= c;
            c = 32 - c;
            while (c > 0)
            {
                Append((byte)(v >> 24));
                v <<= 8;
                c -= 8;
            }
        }

        var result = new byte[_pos];
        Array.Copy(_buffer, result, _pos);
        return result;
    }

    private void AddCarry()
    {
        var q = _pos - 1;
        while (q >= 0 && _buffer[q] == 255)
            _buffer[q--] = 0;
        if (q >= 0)
            _buffer[q]++;
    }

    private void Append(byte b)
    {
        if (_pos == _buffer.Length)
            Array.Resize(ref _buffer, _buffer.Length * 2);
        _buffer[_pos++] = b;
    }
}

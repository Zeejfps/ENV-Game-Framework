namespace WebPSharp.Vp8L;

/// <summary>
/// A least-significant-bit-first bit writer producing a VP8L lossless bitstream. Bits accumulate
/// from the low end of the current byte and are flushed little-endian, exactly matching what
/// <see cref="Vp8LBitReader"/> consumes. The final partial byte is zero-padded.
/// </summary>
public sealed class Vp8LBitWriter
{
    private byte[] _buffer;
    private int _bytePos;
    private ulong _accumulator;
    private int _bitsUsed;

    /// <summary>Creates a writer with an optional initial capacity hint.</summary>
    /// <param name="initialCapacity">The initial byte capacity of the backing buffer.</param>
    public Vp8LBitWriter(int initialCapacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        _buffer = new byte[Math.Max(initialCapacity, 16)];
    }

    /// <summary>The total number of bits written so far.</summary>
    public long BitLength => (long)_bytePos * 8 + _bitsUsed;

    /// <summary>Appends <paramref name="count"/> low bits of <paramref name="value"/>.</summary>
    /// <param name="value">The value whose low <paramref name="count"/> bits are written.</param>
    /// <param name="count">The number of bits to write, from 0 to 32 inclusive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or greater than 32.</exception>
    public void PutBits(uint value, int count)
    {
        if ((uint)count > 32u)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Bit count must be between 0 and 32.");
        if (count == 0)
            return;

        var masked = count == 32 ? value : value & ((1u << count) - 1);
        _accumulator |= (ulong)masked << _bitsUsed;
        _bitsUsed += count;

        while (_bitsUsed >= 8)
        {
            EnsureCapacity(_bytePos + 1);
            _buffer[_bytePos++] = (byte)_accumulator;
            _accumulator >>= 8;
            _bitsUsed -= 8;
        }
    }

    /// <summary>Writes a single bit.</summary>
    /// <param name="bit">The bit value; any non-zero value writes a 1.</param>
    public void PutBit(uint bit) => PutBits(bit & 1, 1);

    /// <summary>
    /// Aligns the stream to the next byte boundary by padding with zero bits. Has no effect when
    /// already aligned.
    /// </summary>
    public void AlignToByte()
    {
        if (_bitsUsed != 0)
            PutBits(0, 8 - _bitsUsed);
    }

    /// <summary>Returns the written bytes, flushing any partial final byte with zero padding.</summary>
    /// <returns>A newly allocated array of the written bytes.</returns>
    public byte[] ToArray()
    {
        var totalBytes = _bytePos + (_bitsUsed > 0 ? 1 : 0);
        var result = new byte[totalBytes];
        Array.Copy(_buffer, result, _bytePos);
        if (_bitsUsed > 0)
            result[_bytePos] = (byte)_accumulator;
        return result;
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
            return;
        var newSize = _buffer.Length * 2;
        while (newSize < required)
            newSize *= 2;
        Array.Resize(ref _buffer, newSize);
    }
}

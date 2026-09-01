using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8L;

/// <summary>
/// A least-significant-bit-first bit reader for the VP8L lossless bitstream. Bits are consumed
/// from the low end of each byte and multi-bit values are little-endian, matching the WebP
/// lossless specification. Reading past the end of the buffer yields zero bits and latches
/// <see cref="IsEndOfStream"/> so the decoder can report truncated input.
/// </summary>
/// <remarks>
/// This is a mutable <see langword="ref struct"/>: it holds a <see cref="ReadOnlySpan{T}"/> over
/// the source and must not be boxed or copied mid-read. A 64-bit accumulator is refilled from the
/// source a byte at a time, so a typical read is a mask and a shift with no per-bit branching.
/// </remarks>
public ref struct Vp8LBitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _bytePos;
    private ulong _accumulator;
    private int _bitsAvailable;
    private bool _eos;

    /// <summary>Creates a reader over the given VP8L payload.</summary>
    /// <param name="data">The bytes to read.</param>
    public Vp8LBitReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _bytePos = 0;
        _accumulator = 0;
        _bitsAvailable = 0;
        _eos = false;
    }

    /// <summary>
    /// Whether a read has requested more bits than the buffer could supply. Once set it remains
    /// set for the lifetime of the reader.
    /// </summary>
    public readonly bool IsEndOfStream => _eos;

    /// <summary>The number of whole bytes consumed so far, rounded down.</summary>
    public readonly int BytesConsumed => _bytePos - (_bitsAvailable >> 3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Fill()
    {
        while (_bitsAvailable <= 56 && _bytePos < _data.Length)
        {
            _accumulator |= (ulong)_data[_bytePos++] << _bitsAvailable;
            _bitsAvailable += 8;
        }
    }

    /// <summary>Reads <paramref name="count"/> bits (0..32) and advances the position.</summary>
    /// <param name="count">The number of bits to read, from 0 to 32 inclusive.</param>
    /// <returns>The bits read, with the first bit read placed in the least-significant position.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or greater than 32.</exception>
    public uint ReadBits(int count)
    {
        if ((uint)count > 32u)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Bit count must be between 0 and 32.");
        if (count == 0)
            return 0;

        if (_bitsAvailable < count)
            Fill();

        if (_bitsAvailable < count)
        {
            // Not enough bits remain; return what we have with the missing high bits as zero.
            _eos = true;
            var partial = (uint)(_accumulator & Mask(_bitsAvailable));
            _accumulator = 0;
            _bitsAvailable = 0;
            return partial;
        }

        var result = (uint)(_accumulator & Mask(count));
        _accumulator >>= count;
        _bitsAvailable -= count;
        return result;
    }

    /// <summary>Reads a single bit.</summary>
    /// <returns>The bit value, 0 or 1.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadBit() => ReadBits(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mask(int bits) => bits == 64 ? ulong.MaxValue : (1UL << bits) - 1;
}

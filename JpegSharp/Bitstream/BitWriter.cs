namespace JpegSharp.Bitstream;

/// <summary>
/// An MSB-first bit writer for JPEG entropy-coded segments. Emits bytes to the underlying
/// stream and automatically inserts a <c>0x00</c> stuffing byte after every <c>0xFF</c>
/// data byte so that entropy data can never be mistaken for a marker.
/// </summary>
internal sealed class BitWriter
{
    private const int BufferSize = 8192;

    private readonly Stream _stream;
    private readonly byte[] _buffer = new byte[BufferSize];
    private int _count;
    private uint _accumulator;
    private int _bitCount;

    /// <summary>Creates a writer that appends entropy data to the given stream.</summary>
    /// <param name="stream">The destination stream.</param>
    public BitWriter(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Writes the low <paramref name="count"/> bits of <paramref name="value"/> MSB-first.
    /// </summary>
    /// <param name="value">The value whose low bits are written.</param>
    /// <param name="count">Number of bits to write (0..24).</param>
    public void WriteBits(int value, int count)
    {
        if (count == 0)
            return;

        var mask = (1u << count) - 1;
        _accumulator = (_accumulator << count) | ((uint)value & mask);
        _bitCount += count;

        while (_bitCount >= 8)
        {
            _bitCount -= 8;
            var b = (byte)(_accumulator >> _bitCount);
            if (_count >= BufferSize - 1)
                DrainBuffer();
            _buffer[_count++] = b;
            if (b == 0xFF)
                _buffer[_count++] = 0x00;
        }
    }

    private void DrainBuffer()
    {
        if (_count > 0)
        {
            _stream.Write(_buffer, 0, _count);
            _count = 0;
        }
    }

    /// <summary>
    /// Flushes any buffered partial byte, padding the remaining bits with <c>1</c>s as
    /// required by the JPEG specification.
    /// </summary>
    public void Flush()
    {
        if (_bitCount > 0)
        {
            var pad = 8 - _bitCount;
            WriteBits((1 << pad) - 1, pad);
        }

        DrainBuffer();
    }
}

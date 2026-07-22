using JpegSharp.Api.Exceptions;

namespace JpegSharp.Bitstream;

/// <summary>
/// An MSB-first bit reader over a JPEG entropy-coded segment. Transparently removes
/// <c>0x00</c> stuffing bytes that follow a <c>0xFF</c> data byte and detects marker codes
/// (a <c>0xFF</c> followed by a non-zero, non-fill byte), at which point further reads
/// return padding <c>1</c> bits and <see cref="MarkerReached"/> becomes <see langword="true"/>.
/// </summary>
/// <remarks>
/// This is a <see langword="ref"/> struct so it can wrap a <see cref="ReadOnlySpan{T}"/> of
/// the compressed data with zero allocation and be mutated in place by the entropy decoder.
/// </remarks>
internal ref struct BitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _pos;
    private uint _buffer;
    private int _count;
    private bool _marker;
    private byte _markerCode;

    /// <summary>Creates a reader positioned at the start of the given entropy data.</summary>
    /// <param name="data">The compressed entropy-coded bytes (may include markers).</param>
    public BitReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _pos = 0;
        _buffer = 0;
        _count = 0;
        _marker = false;
        _markerCode = 0;
    }

    /// <summary>Gets a value indicating whether a marker has been encountered.</summary>
    public readonly bool MarkerReached => _marker;

    /// <summary>Gets the marker code (the byte after <c>0xFF</c>) once one is reached.</summary>
    public readonly byte Marker => _markerCode;

    /// <summary>Gets the current byte offset into the source data.</summary>
    public readonly int BytePosition => _pos;

    /// <summary>
    /// Reads <paramref name="count"/> bits MSB-first and returns them right-aligned.
    /// If a marker or the end of data is reached, missing bits are supplied as <c>1</c>.
    /// </summary>
    /// <param name="count">Number of bits to read (0..24).</param>
    /// <returns>The bits read, right-aligned in the low bits of the result.</returns>
    public int ReadBits(int count)
    {
        if (count == 0)
            return 0;

        while (_count < count)
        {
            if (!FillByte())
            {
                // Marker or end of data: pad with 1 bits so the caller can finish gracefully.
                _buffer = (_buffer << 8) | 0xFF;
                _count += 8;
            }
        }

        _count -= count;
        return (int)((_buffer >> _count) & ((1u << count) - 1));
    }

    /// <summary>Reads a single bit MSB-first.</summary>
    /// <returns>The next bit as 0 or 1.</returns>
    public int ReadBit() => ReadBits(1);

    /// <summary>
    /// Returns the next 8 bits MSB-first <em>without consuming them</em>, padding with
    /// <c>1</c> bits at a marker or the end of data. Used for Huffman lookahead.
    /// </summary>
    /// <returns>The next 8 bits, right-aligned (0..255).</returns>
    public int PeekByte()
    {
        while (_count < 8)
        {
            if (!FillByte())
            {
                _buffer = (_buffer << 8) | 0xFF;
                _count += 8;
            }
        }

        return (int)((_buffer >> (_count - 8)) & 0xFF);
    }

    /// <summary>
    /// Consumes <paramref name="count"/> bits that are already buffered (for example after a
    /// successful <see cref="PeekByte"/> lookahead).
    /// </summary>
    /// <param name="count">Number of buffered bits to consume (must not exceed the buffer).</param>
    public void SkipBits(int count) => _count -= count;

    /// <summary>Discards buffered bits back to the next byte boundary.</summary>
    public void AlignToByte() => _count -= _count & 7;

    /// <summary>
    /// Clears a reached restart marker (RST0–RST7), advances past it, and resets the bit
    /// buffer so decoding can resume with the following entropy data.
    /// </summary>
    public void ResetForRestart()
    {
        _pos += 2; // skip 0xFF and the RSTn code
        _marker = false;
        _markerCode = 0;
        _buffer = 0;
        _count = 0;
    }

    /// <summary>
    /// Discards any buffered bits, locates the next restart marker (RST0–RST7) in the
    /// underlying byte stream, and advances just past it. Used at restart-interval
    /// boundaries during entropy decoding.
    /// </summary>
    /// <exception cref="JpegFormatException">No restart marker is found where expected.</exception>
    public void SkipRestartMarker()
    {
        _buffer = 0;
        _count = 0;
        _marker = false;
        _markerCode = 0;

        while (_pos < _data.Length && _data[_pos] != 0xFF)
            _pos++;
        while (_pos < _data.Length && _data[_pos] == 0xFF)
            _pos++;

        if (_pos >= _data.Length || _data[_pos] is < 0xD0 or > 0xD7)
            throw new JpegCorruptException("Expected a restart marker in the entropy stream.");

        _pos++;
    }

    /// <summary>
    /// Sign-extends a <paramref name="magnitude"/>-bit received value into the signed
    /// coefficient it represents (ITU-T T.81 Figure F.12, EXTEND).
    /// </summary>
    /// <param name="value">The raw received bits.</param>
    /// <param name="magnitude">The magnitude category (bit length).</param>
    /// <returns>The sign-extended value.</returns>
    public static int Extend(int value, int magnitude)
    {
        if (magnitude == 0)
            return 0;
        var threshold = 1 << (magnitude - 1);
        return value < threshold ? value - (1 << magnitude) + 1 : value;
    }

    private bool FillByte()
    {
        if (_marker || _pos >= _data.Length)
            return false;

        var b = _data[_pos++];
        if (b == 0xFF)
        {
            // Skip fill bytes (runs of 0xFF are legal padding before a marker).
            while (_pos < _data.Length && _data[_pos] == 0xFF)
                _pos++;

            var next = _pos < _data.Length ? _data[_pos] : (byte)0xD9;
            if (next == 0x00)
            {
                _pos++; // consume the stuffing byte
                _buffer = (_buffer << 8) | 0xFF;
                _count += 8;
                return true;
            }

            _marker = true;
            _markerCode = next;
            _pos--; // step back so BytePosition points at the 0xFF preceding the code
            return false;
        }

        _buffer = (_buffer << 8) | b;
        _count += 8;
        return true;
    }
}

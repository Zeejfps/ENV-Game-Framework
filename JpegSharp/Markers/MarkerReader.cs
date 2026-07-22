using JpegSharp.Api.Exceptions;

namespace JpegSharp.Markers;

/// <summary>
/// Reads JPEG markers and their length-prefixed segment payloads from a stream. Entropy-coded
/// scan data (which follows an SOS segment) is not consumed here; the decoder reads it
/// separately via a bit reader.
/// </summary>
internal sealed class MarkerReader
{
    private readonly Stream _stream;

    /// <summary>Creates a reader over the given source stream.</summary>
    /// <param name="stream">The stream positioned at (or before) a marker.</param>
    public MarkerReader(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Reads the next marker code, requiring a <c>0xFF</c> prefix and skipping any fill
    /// <c>0xFF</c> bytes that precede the code.
    /// </summary>
    /// <returns>The marker code byte.</returns>
    /// <exception cref="JpegFormatException">
    /// The next byte is not a marker prefix, or the stream ended unexpectedly.
    /// </exception>
    public byte ReadMarker()
    {
        var first = _stream.ReadByte();
        if (first < 0)
            throw new JpegFormatException("Expected a marker but reached the end of the stream.");
        if (first != 0xFF)
            throw new JpegFormatException($"Expected a marker prefix (0xFF) but found 0x{first:X2}.");

        int code;
        do
        {
            code = _stream.ReadByte();
            if (code < 0)
                throw new JpegFormatException("Stream ended while reading a marker code.");
        }
        while (code == 0xFF); // skip fill bytes

        return (byte)code;
    }

    /// <summary>Reads a big-endian unsigned 16-bit value.</summary>
    /// <returns>The value read.</returns>
    /// <exception cref="JpegFormatException">The stream ended unexpectedly.</exception>
    public ushort ReadUInt16()
    {
        var hi = _stream.ReadByte();
        var lo = _stream.ReadByte();
        if (lo < 0)
            throw new JpegFormatException("Stream ended while reading a 16-bit value.");
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// Reads a length-prefixed segment payload (the two-byte length includes itself, so the
    /// payload is <c>length - 2</c> bytes).
    /// </summary>
    /// <returns>The payload bytes.</returns>
    /// <exception cref="JpegFormatException">The length is invalid or the payload is truncated.</exception>
    public byte[] ReadSegment()
    {
        var length = ReadUInt16();
        if (length < 2)
            throw new JpegFormatException($"Invalid segment length {length}; must be at least 2.");

        var payload = new byte[length - 2];
        ReadExact(payload);
        return payload;
    }

    /// <summary>Reads exactly <paramref name="buffer"/>.Length bytes or throws.</summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <exception cref="JpegFormatException">The stream ended before the buffer was filled.</exception>
    public void ReadExact(Span<byte> buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = _stream.Read(buffer[offset..]);
            if (read <= 0)
                throw new JpegFormatException("Unexpected end of stream while reading segment data.");
            offset += read;
        }
    }
}

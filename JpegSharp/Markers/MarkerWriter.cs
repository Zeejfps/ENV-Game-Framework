using JpegSharp.Api.Exceptions;

namespace JpegSharp.Markers;

/// <summary>
/// Writes JPEG markers and length-prefixed segments to a stream.
/// </summary>
internal sealed class MarkerWriter
{
    private readonly Stream _stream;

    /// <summary>Creates a writer over the given destination stream.</summary>
    /// <param name="stream">The stream to write markers to.</param>
    public MarkerWriter(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>Writes a marker (the <c>0xFF</c> prefix followed by the code).</summary>
    /// <param name="code">The marker code.</param>
    public void WriteMarker(byte code)
    {
        _stream.WriteByte(0xFF);
        _stream.WriteByte(code);
    }

    /// <summary>Writes a big-endian unsigned 16-bit value.</summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt16(ushort value)
    {
        _stream.WriteByte((byte)(value >> 8));
        _stream.WriteByte((byte)(value & 0xFF));
    }

    /// <summary>
    /// Writes a marker followed by its length-prefixed payload. The written length field is
    /// <c>payload.Length + 2</c>.
    /// </summary>
    /// <param name="code">The marker code.</param>
    /// <param name="payload">The segment payload.</param>
    /// <exception cref="JpegFormatException">The payload exceeds the 65533-byte segment limit.</exception>
    public void WriteSegment(byte code, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > ushort.MaxValue - 2)
            throw new JpegFormatException($"Segment payload of {payload.Length} bytes exceeds the maximum of {ushort.MaxValue - 2}.");

        WriteMarker(code);
        WriteUInt16((ushort)(payload.Length + 2));
        _stream.Write(payload);
    }

    /// <summary>Writes raw bytes directly to the stream (e.g. entropy-coded scan data).</summary>
    /// <param name="bytes">The bytes to write.</param>
    public void WriteRaw(ReadOnlySpan<byte> bytes) => _stream.Write(bytes);
}

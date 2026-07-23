using System.Buffers.Binary;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Container;

/// <summary>
/// Writes a WebP RIFF container to a seekable stream. The <c>RIFF</c> wrapper size is written as a
/// placeholder up front and back-patched on <see cref="Complete"/>, so chunk payloads stream
/// directly to the destination without being buffered in full.
/// </summary>
public sealed class RiffWriter
{
    private readonly Stream _stream;
    private readonly long _origin;
    private bool _completed;

    /// <summary>Begins a WebP container by writing the <c>RIFF</c>/<c>WEBP</c> header.</summary>
    /// <param name="stream">The destination stream; must be writable and seekable.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">The stream is not writable or not seekable.</exception>
    public RiffWriter(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be writable.", nameof(stream));
        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable so the RIFF size can be back-patched.", nameof(stream));

        _stream = stream;
        _origin = stream.Position;

        Span<byte> header = stackalloc byte[12];
        WebPChunkIds.Riff.Write(header);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4, 4), 0); // placeholder size
        WebPChunkIds.WebP.Write(header.Slice(8, 4));
        _stream.Write(header);
    }

    /// <summary>Writes a complete chunk (header, payload, and pad byte if needed).</summary>
    /// <param name="id">The chunk identifier.</param>
    /// <param name="payload">The chunk payload.</param>
    /// <exception cref="InvalidOperationException">The container has already been completed.</exception>
    public void WriteChunk(FourCc id, ReadOnlySpan<byte> payload)
    {
        EnsureNotCompleted();
        WriteChunkTo(_stream, id, payload);
    }

    /// <summary>
    /// Writes a single RIFF chunk (header, payload, and pad byte if the payload length is odd) to an
    /// arbitrary stream. Used for nested chunks such as the image inside an animation frame, which
    /// are not part of the outer RIFF size accounting.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="id">The chunk identifier.</param>
    /// <param name="payload">The chunk payload.</param>
    public static void WriteChunkTo(Stream stream, FourCc id, ReadOnlySpan<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> header = stackalloc byte[RiffReader.HeaderSize];
        id.Write(header);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4, 4), (uint)payload.Length);
        stream.Write(header);
        stream.Write(payload);
        if ((payload.Length & 1) != 0)
            stream.WriteByte(0);
    }

    /// <summary>
    /// Finalizes the container by back-patching the <c>RIFF</c> size field. Must be called exactly
    /// once after all chunks are written.
    /// </summary>
    /// <exception cref="InvalidOperationException">The container has already been completed.</exception>
    /// <exception cref="WebPFormatException">The resulting container exceeds the 4 GiB RIFF size limit.</exception>
    public void Complete()
    {
        EnsureNotCompleted();
        _completed = true;

        var end = _stream.Position;
        var riffSize = end - _origin - RiffReader.HeaderSize;
        if (riffSize > uint.MaxValue)
            throw new WebPFormatException($"WebP container size {riffSize} exceeds the 4 GiB RIFF limit.");

        _stream.Seek(_origin + 4, SeekOrigin.Begin);
        Span<byte> size = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(size, (uint)riffSize);
        _stream.Write(size);
        _stream.Seek(end, SeekOrigin.Begin);
    }

    private void EnsureNotCompleted()
    {
        if (_completed)
            throw new InvalidOperationException("The RIFF container has already been completed.");
    }
}

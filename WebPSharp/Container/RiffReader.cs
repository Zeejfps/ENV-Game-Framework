using System.Buffers.Binary;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Container;

/// <summary>
/// A forward-only reader over the chunks of a WebP RIFF container. Validates the outer
/// <c>RIFF....WEBP</c> wrapper on construction, then enumerates the inner chunks without copying
/// their payloads. Each chunk payload is exposed as a <see cref="ReadOnlyMemory{T}"/> view over
/// the source buffer.
/// </summary>
/// <remarks>
/// The reader is a mutable struct used as an enumerator; do not copy it mid-enumeration. Obtain
/// one from <see cref="Create"/> and drive it with <see cref="MoveNext"/> / <see cref="Current"/>.
/// </remarks>
public struct RiffReader
{
    /// <summary>The size of a RIFF/chunk header: a 4-byte FourCC plus a 4-byte little-endian size.</summary>
    public const int HeaderSize = 8;

    private readonly ReadOnlyMemory<byte> _body;
    private int _position;
    private RiffChunk _current;

    private RiffReader(ReadOnlyMemory<byte> body)
    {
        _body = body;
        _position = 0;
        _current = default;
    }

    /// <summary>The chunk yielded by the most recent successful <see cref="MoveNext"/> call.</summary>
    public readonly RiffChunk Current => _current;

    /// <summary>
    /// The declared RIFF payload size (the value following the <c>RIFF</c> tag), as validated at
    /// construction.
    /// </summary>
    public int RiffSize { get; private set; }

    /// <summary>
    /// Validates the RIFF container header of <paramref name="source"/> and returns a reader
    /// positioned at the first chunk.
    /// </summary>
    /// <param name="source">The complete WebP byte stream.</param>
    /// <returns>A reader over the container's chunks.</returns>
    /// <exception cref="WebPFormatException">
    /// The buffer is too small, or the <c>RIFF</c> / <c>WEBP</c> signature is missing or malformed.
    /// </exception>
    public static RiffReader Create(ReadOnlyMemory<byte> source)
    {
        var span = source.Span;
        if (span.Length < 12)
            throw new WebPFormatException($"WebP stream is too short: {span.Length} bytes, need at least 12 for a RIFF header.");

        if (FourCc.Read(span) != WebPChunkIds.Riff)
            throw new WebPFormatException("Missing 'RIFF' signature; not a RIFF container.");

        var riffSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4, 4));
        if (FourCc.Read(span.Slice(8, 4)) != WebPChunkIds.WebP)
            throw new WebPFormatException("Missing 'WEBP' form type; not a WebP file.");

        // The RIFF size counts everything after the 8-byte RIFF header, i.e. the 'WEBP' tag plus
        // all chunks. The available body must be at least large enough to hold what it declares,
        // but we tolerate trailing bytes beyond the declared size (some encoders pad).
        var available = span.Length - 8;
        if (riffSize < 4)
            throw new WebPFormatException($"RIFF size {riffSize} is too small to contain the 'WEBP' form type.");
        if (riffSize > available)
            throw new WebPFormatException($"RIFF size {riffSize} exceeds the available {available} bytes.");

        // The chunk region begins after 'RIFF', the size field, and the 'WEBP' tag (offset 12).
        // Bound it by the smaller of the declared size and the physical buffer.
        var bodyLength = (int)riffSize - 4;
        var reader = new RiffReader(source.Slice(12, bodyLength)) { RiffSize = (int)riffSize };
        return reader;
    }

    /// <summary>Advances to the next chunk.</summary>
    /// <returns><see langword="true"/> if a chunk was read; <see langword="false"/> at the end of the container.</returns>
    /// <exception cref="WebPFormatException">A chunk header or payload is truncated or malformed.</exception>
    public bool MoveNext()
    {
        var span = _body.Span;
        if (_position == span.Length)
            return false;

        if (_position + HeaderSize > span.Length)
            throw new WebPFormatException($"Truncated chunk header at offset {_position + 12}: {span.Length - _position} bytes remain, need {HeaderSize}.");

        var id = FourCc.Read(span.Slice(_position, 4));
        var size = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(_position + 4, 4));
        var payloadStart = _position + HeaderSize;

        if (size > (uint)(span.Length - payloadStart))
            throw new WebPFormatException($"Chunk '{id}' declares {size} payload bytes but only {span.Length - payloadStart} remain.");

        _current = new RiffChunk(id, _body.Slice(payloadStart, (int)size));

        // Chunks are padded to an even size; skip the pad byte when the payload length is odd.
        var advance = HeaderSize + (int)size;
        if ((size & 1) != 0)
        {
            advance++;
            // A trailing odd-sized chunk may legally omit the final pad byte at end of file.
            if (payloadStart + (int)size > span.Length - 1 && payloadStart + (int)size != span.Length)
                throw new WebPFormatException($"Chunk '{id}' is missing its pad byte.");
        }

        _position += advance;
        if (_position > span.Length)
            _position = span.Length;
        return true;
    }
}

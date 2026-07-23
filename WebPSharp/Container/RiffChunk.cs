namespace WebPSharp.Container;

/// <summary>
/// A single RIFF chunk: a four-character identifier and its payload. The payload is a view over
/// the source buffer and excludes the 8-byte chunk header and any trailing pad byte.
/// </summary>
public readonly struct RiffChunk
{
    /// <summary>Creates a chunk.</summary>
    /// <param name="id">The chunk's four-character identifier.</param>
    /// <param name="payload">The chunk payload (without header or padding).</param>
    public RiffChunk(FourCc id, ReadOnlyMemory<byte> payload)
    {
        Id = id;
        Payload = payload;
    }

    /// <summary>The chunk's four-character identifier.</summary>
    public FourCc Id { get; }

    /// <summary>The chunk payload, excluding the 8-byte header and any pad byte.</summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>The payload length in bytes.</summary>
    public int Length => Payload.Length;
}

namespace WebPSharp.Api;

/// <summary>
/// Optional metadata carried alongside a WebP image: an ICC color profile, Exif and XMP payloads,
/// and any unrecognized chunks preserved verbatim for round-tripping. Any field may be null or
/// empty.
/// </summary>
public sealed class WebPMetadata
{
    /// <summary>The ICC color profile from the <c>ICCP</c> chunk, if present.</summary>
    public byte[]? IccProfile { get; set; }

    /// <summary>The raw Exif payload from the <c>EXIF</c> chunk, if present.</summary>
    public byte[]? Exif { get; set; }

    /// <summary>The raw XMP payload from the <c>XMP&#160;</c> chunk, if present.</summary>
    public byte[]? Xmp { get; set; }

    /// <summary>
    /// Chunks the codec does not interpret, preserved in file order so they survive a
    /// decode/encode round-trip.
    /// </summary>
    public IList<WebPUnknownChunk> UnknownChunks { get; } = new List<WebPUnknownChunk>();
}

/// <summary>
/// An unrecognized RIFF chunk preserved verbatim.
/// </summary>
/// <param name="Id">The chunk's four-character identifier.</param>
/// <param name="Data">The chunk payload (excluding header and padding).</param>
public readonly record struct WebPUnknownChunk(string Id, byte[] Data);

namespace JpegSharp.Api;

/// <summary>
/// Options controlling JPEG decoding.
/// </summary>
public sealed class JpegDecoderOptions
{
    /// <summary>
    /// The maximum number of pixels (width × height) the decoder will accept before throwing.
    /// Guards against maliciously large dimensions causing excessive allocation. Defaults to
    /// 500 million.
    /// </summary>
    public long MaxPixels { get; set; } = 500_000_000;

    /// <summary>
    /// When true (the default), the decoder collects metadata (JFIF, Exif, ICC, Adobe, COM)
    /// and exposes it via <see cref="JpegImage.Metadata"/>. When false, metadata is skipped
    /// and <see cref="JpegImage.Metadata"/> is left null.
    /// </summary>
    public bool ReadMetadata { get; set; } = true;

    /// <summary>
    /// Controls how restart markers (RST0–RST7) are validated at restart-interval boundaries.
    /// When false (the default), a missing or out-of-sequence restart marker is handled
    /// leniently: the decoder resyncs, resets its DC predictors and continues, matching its
    /// general tolerance of truncated or corrupt entropy data. When true, a missing or
    /// misordered restart marker throws <see cref="Exceptions.JpegCorruptException"/>.
    /// </summary>
    public bool StrictRestartMarkers { get; set; } = false;
}

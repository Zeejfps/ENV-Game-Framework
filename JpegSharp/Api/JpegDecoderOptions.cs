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
    /// The maximum estimated PEAK decode allocation, in bytes, the decoder will accept before
    /// throwing. Unlike <see cref="MaxPixels"/>, this bounds actual memory: it accounts for the
    /// component sample planes (2 bytes/sample at 12-bit precision, 1 byte at 8-bit), the
    /// per-component coefficient buffers a progressive (SOF2) decode additionally allocates, the
    /// full-size chroma-upsampling scratch plane each subsampled component allocates during
    /// assembly, and the final output image buffer. A pixel count alone does not bound memory, since a
    /// multi-component, 12-bit, and/or progressive image consumes far more bytes per pixel than a
    /// single-component 8-bit baseline image. Enforced ALONGSIDE <see cref="MaxPixels"/> (whichever
    /// limit trips first), and independently of the internal int-overflow guards. Defaults to
    /// 1 GiB (1,073,741,824 bytes).
    /// </summary>
    public long MaxDecodedBytes { get; set; } = 1L << 30;

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

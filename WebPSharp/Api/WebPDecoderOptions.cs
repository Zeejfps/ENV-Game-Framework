namespace WebPSharp.Api;

/// <summary>
/// Options controlling WebP decoding.
/// </summary>
public sealed class WebPDecoderOptions
{
    /// <summary>
    /// The maximum number of pixels (width × height) the decoder will accept before throwing.
    /// Guards against maliciously large dimensions causing excessive allocation. Defaults to
    /// 500 million.
    /// </summary>
    public long MaxPixels { get; set; } = 500_000_000;

    /// <summary>
    /// When true (the default), the decoder extracts metadata (ICC, EXIF, XMP) and preserves
    /// unknown chunks, exposing them via <see cref="WebPImage.Metadata"/>. When false, metadata is
    /// skipped and <see cref="WebPImage.Metadata"/> is left null.
    /// </summary>
    public bool ReadMetadata { get; set; } = true;
}

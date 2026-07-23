namespace WebPSharp.Api;

/// <summary>
/// Options controlling WebP encoding.
/// </summary>
public sealed class WebPEncoderOptions
{
    /// <summary>
    /// When true (the default), the image is encoded losslessly (VP8L). When false, the image is
    /// encoded lossily as a VP8 intra key frame at <see cref="Quality"/>.
    /// </summary>
    public bool Lossless { get; set; } = true;

    /// <summary>
    /// Target quality for lossy encoding, 0 (smallest) to 100 (best). Ignored by lossless
    /// encoding. Controls the VP8 base quantizer.
    /// </summary>
    public int Quality { get; set; } = 75;

    /// <summary>
    /// Compression effort, 0 (fastest) to 9 (smallest). Higher values spend more time searching
    /// for a better encoding. Reserved for future use by the entropy and back-reference search.
    /// </summary>
    public int Effort { get; set; } = 4;
}

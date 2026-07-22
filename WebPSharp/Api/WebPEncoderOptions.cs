namespace WebPSharp.Api;

/// <summary>
/// Options controlling WebP encoding.
/// </summary>
public sealed class WebPEncoderOptions
{
    /// <summary>
    /// When true (the default), the image is encoded losslessly (VP8L). Lossy (VP8) encoding is
    /// being implemented incrementally and is not yet available.
    /// </summary>
    public bool Lossless { get; set; } = true;

    /// <summary>
    /// Target quality for lossy encoding, 0 (smallest) to 100 (best). Ignored by lossless
    /// encoding. Reserved for the forthcoming VP8 encoder.
    /// </summary>
    public int Quality { get; set; } = 75;

    /// <summary>
    /// Compression effort, 0 (fastest) to 9 (smallest). Higher values spend more time searching
    /// for a better encoding. Reserved for future use by the entropy and back-reference search.
    /// </summary>
    public int Effort { get; set; } = 4;
}

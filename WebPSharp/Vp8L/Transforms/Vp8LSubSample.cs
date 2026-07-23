namespace WebPSharp.Vp8L.Transforms;

/// <summary>
/// Helpers for the sub-sampled images (predictor mode image, color transform image) that some
/// VP8L transforms carry. Each such image stores one value per tile of <c>2^bits × 2^bits</c>
/// pixels.
/// </summary>
internal static class Vp8LSubSample
{
    /// <summary>Returns the sub-sampled dimension for a full dimension and tile-bit count.</summary>
    /// <param name="size">The full image dimension in pixels.</param>
    /// <param name="bits">The tile size in bits (tile edge is <c>2^bits</c>).</param>
    /// <returns>The number of tiles spanning the dimension.</returns>
    public static int Size(int size, int bits) => (size + (1 << bits) - 1) >> bits;
}

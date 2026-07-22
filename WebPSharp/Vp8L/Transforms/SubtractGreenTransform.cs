namespace WebPSharp.Vp8L.Transforms;

/// <summary>
/// The VP8L subtract-green transform. The encoder subtracts the green channel from the red and
/// blue channels (which are often correlated with green), improving compressibility; the decoder
/// adds it back. The transform carries no side data — only its presence is signaled in the
/// bitstream. All arithmetic is modulo 256 per channel.
/// </summary>
internal static class SubtractGreenTransform
{
    /// <summary>Applies the forward transform in place (encoder side).</summary>
    /// <param name="argb">The ARGB pixels to transform.</param>
    public static void Forward(Span<uint> argb)
    {
        for (var i = 0; i < argb.Length; i++)
        {
            var pixel = argb[i];
            var green = (pixel >> 8) & 0xFF;
            var red = ((pixel >> 16) - green) & 0xFF;
            var blue = (pixel - green) & 0xFF;
            argb[i] = (pixel & 0xFF00FF00u) | (red << 16) | blue;
        }
    }

    /// <summary>Reverses the transform in place (decoder side).</summary>
    /// <param name="argb">The ARGB pixels to restore.</param>
    public static void Inverse(Span<uint> argb)
    {
        for (var i = 0; i < argb.Length; i++)
        {
            var pixel = argb[i];
            var green = (pixel >> 8) & 0xFF;
            var red = ((pixel >> 16) + green) & 0xFF;
            var blue = (pixel + green) & 0xFF;
            argb[i] = (pixel & 0xFF00FF00u) | (red << 16) | blue;
        }
    }
}

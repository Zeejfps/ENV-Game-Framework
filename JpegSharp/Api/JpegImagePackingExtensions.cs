namespace JpegSharp.Api;

/// <summary>
/// Convenience wrappers over <see cref="JpegImage.ToPackedPixels(PackedPixelFormat)"/>, one per
/// <see cref="PackedPixelFormat"/>, for callers who prefer a named method to passing the enum.
/// Each has an allocating overload that returns a new array and a zero-allocation overload that
/// fills a caller-supplied span.
/// </summary>
public static class JpegImagePackingExtensions
{
    /// <summary>Packs the image as RGBA (R in the high byte, A in the low byte).</summary>
    public static int[] ToRgba8888(this JpegImage image) =>
        image.ToPackedPixels(PackedPixelFormat.Rgba8888);

    /// <summary>Packs the image as RGBA into <paramref name="destination"/>.</summary>
    public static void ToRgba8888(this JpegImage image, Span<int> destination) =>
        image.ToPackedPixels(destination, PackedPixelFormat.Rgba8888);

    /// <summary>Packs the image as ARGB (A in the high byte, B in the low byte).</summary>
    public static int[] ToArgb8888(this JpegImage image) =>
        image.ToPackedPixels(PackedPixelFormat.Argb8888);

    /// <summary>Packs the image as ARGB into <paramref name="destination"/>.</summary>
    public static void ToArgb8888(this JpegImage image, Span<int> destination) =>
        image.ToPackedPixels(destination, PackedPixelFormat.Argb8888);

    /// <summary>Packs the image as BGRA (B in the high byte, A in the low byte).</summary>
    public static int[] ToBgra8888(this JpegImage image) =>
        image.ToPackedPixels(PackedPixelFormat.Bgra8888);

    /// <summary>Packs the image as BGRA into <paramref name="destination"/>.</summary>
    public static void ToBgra8888(this JpegImage image, Span<int> destination) =>
        image.ToPackedPixels(destination, PackedPixelFormat.Bgra8888);

    /// <summary>Packs the image as ABGR (A in the high byte, R in the low byte).</summary>
    public static int[] ToAbgr8888(this JpegImage image) =>
        image.ToPackedPixels(PackedPixelFormat.Abgr8888);

    /// <summary>Packs the image as ABGR into <paramref name="destination"/>.</summary>
    public static void ToAbgr8888(this JpegImage image, Span<int> destination) =>
        image.ToPackedPixels(destination, PackedPixelFormat.Abgr8888);
}

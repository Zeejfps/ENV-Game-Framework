namespace JpegSharp.Api;

/// <summary>
/// Convenience wrappers over <see cref="JpegImage16.ToPackedPixels64(PackedPixelFormat64)"/>, one
/// per <see cref="PackedPixelFormat64"/>, for callers who prefer a named method to passing the
/// enum. Each has an allocating overload that returns a new array and a zero-allocation overload
/// that fills a caller-supplied span.
/// </summary>
public static class JpegImage16PackingExtensions
{
    /// <summary>Packs the image as 16-bit RGBA (R in the high lane).</summary>
    public static long[] ToRgba16161616(this JpegImage16 image) =>
        image.ToPackedPixels64(PackedPixelFormat64.Rgba16161616);

    /// <summary>Packs the image as 16-bit RGBA into <paramref name="destination"/>.</summary>
    public static void ToRgba16161616(this JpegImage16 image, Span<long> destination) =>
        image.ToPackedPixels64(destination, PackedPixelFormat64.Rgba16161616);

    /// <summary>Packs the image as 16-bit ARGB (A in the high lane).</summary>
    public static long[] ToArgb16161616(this JpegImage16 image) =>
        image.ToPackedPixels64(PackedPixelFormat64.Argb16161616);

    /// <summary>Packs the image as 16-bit ARGB into <paramref name="destination"/>.</summary>
    public static void ToArgb16161616(this JpegImage16 image, Span<long> destination) =>
        image.ToPackedPixels64(destination, PackedPixelFormat64.Argb16161616);

    /// <summary>Packs the image as 16-bit BGRA (B in the high lane).</summary>
    public static long[] ToBgra16161616(this JpegImage16 image) =>
        image.ToPackedPixels64(PackedPixelFormat64.Bgra16161616);

    /// <summary>Packs the image as 16-bit BGRA into <paramref name="destination"/>.</summary>
    public static void ToBgra16161616(this JpegImage16 image, Span<long> destination) =>
        image.ToPackedPixels64(destination, PackedPixelFormat64.Bgra16161616);

    /// <summary>Packs the image as 16-bit ABGR (A in the high lane).</summary>
    public static long[] ToAbgr16161616(this JpegImage16 image) =>
        image.ToPackedPixels64(PackedPixelFormat64.Abgr16161616);

    /// <summary>Packs the image as 16-bit ABGR into <paramref name="destination"/>.</summary>
    public static void ToAbgr16161616(this JpegImage16 image, Span<long> destination) =>
        image.ToPackedPixels64(destination, PackedPixelFormat64.Abgr16161616);
}

namespace JpegSharp.Api;

/// <summary>
/// The common shape of a decoded image regardless of sample precision. Both the 8-bit
/// <see cref="JpegImage"/> and the high-precision <see cref="JpegImage16"/> implement this, so
/// display and pipeline code that only needs dimensions, color space and an 8-bit preview can be
/// written once against the interface. Use <see cref="Precision"/> to detect which concrete type
/// you have and reach for its native sample buffer when full precision matters.
/// </summary>
public interface IJpegImage
{
    /// <summary>The image width in pixels.</summary>
    int Width { get; }

    /// <summary>The image height in pixels.</summary>
    int Height { get; }

    /// <summary>The color space of the sample data.</summary>
    JpegColorSpace ColorSpace { get; }

    /// <summary>The number of interleaved channels per pixel (1 grayscale, 3 RGB, 4 CMYK).</summary>
    int ComponentCount { get; }

    /// <summary>The sample bit precision (8 for <see cref="JpegImage"/>, 9–16 for <see cref="JpegImage16"/>).</summary>
    int Precision { get; }

    /// <summary>Metadata associated with the image, or null.</summary>
    JpegMetadata? Metadata { get; }

    /// <summary>
    /// Produces an 8-bit RGBA preview (one packed pixel per <see cref="int"/>), tone-mapping
    /// higher-precision samples down to 8 bits. This is the precision-agnostic display bridge;
    /// for full-precision access use the concrete type's native buffer or packing helpers.
    /// </summary>
    /// <returns>An array of <c>Width * Height</c> RGBA-packed pixels.</returns>
    int[] ToRgba8888();
}

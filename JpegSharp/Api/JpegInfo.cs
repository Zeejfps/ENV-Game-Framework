namespace JpegSharp.Api;

/// <summary>
/// Lightweight structural information about a JPEG stream, obtained by parsing only its
/// headers (up to and including the frame header) without decoding pixel data.
/// </summary>
public sealed class JpegInfo
{
    /// <summary>Creates a new info record.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="componentCount">The number of color components.</param>
    /// <param name="colorSpace">The inferred output color space.</param>
    /// <param name="precision">The sample bit precision.</param>
    /// <param name="isProgressive">Whether the frame is a progressive DCT frame.</param>
    public JpegInfo(int width, int height, int componentCount, JpegColorSpace colorSpace, int precision, bool isProgressive)
    {
        Width = width;
        Height = height;
        ComponentCount = componentCount;
        ColorSpace = colorSpace;
        Precision = precision;
        IsProgressive = isProgressive;
    }

    /// <summary>The image width in pixels.</summary>
    public int Width { get; }

    /// <summary>The image height in pixels.</summary>
    public int Height { get; }

    /// <summary>The number of color components in the frame.</summary>
    public int ComponentCount { get; }

    /// <summary>The color space that a full decode would produce.</summary>
    public JpegColorSpace ColorSpace { get; }

    /// <summary>The sample bit precision (typically 8).</summary>
    public int Precision { get; }

    /// <summary>Whether the frame uses progressive DCT coding.</summary>
    public bool IsProgressive { get; }
}

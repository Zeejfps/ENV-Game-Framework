namespace JpegSharp.Api;

/// <summary>
/// The color space of a decoded or to-be-encoded <see cref="JpegImage"/>.
/// </summary>
public enum JpegColorSpace
{
    /// <summary>Single-channel luminance.</summary>
    Grayscale,

    /// <summary>Three interleaved 8-bit channels in R, G, B order.</summary>
    Rgb,

    /// <summary>Four interleaved 8-bit channels in C, M, Y, K order.</summary>
    Cmyk,
}

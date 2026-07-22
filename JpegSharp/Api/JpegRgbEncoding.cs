namespace JpegSharp.Api;

/// <summary>
/// Selects the color representation used when encoding an RGB image.
/// </summary>
public enum JpegRgbEncoding
{
    /// <summary>
    /// Apply the YCbCr color transform (the JFIF default). Enables chroma subsampling and
    /// produces smaller files.
    /// </summary>
    YCbCr,

    /// <summary>
    /// Store the R, G, B channels directly with no color transform (Adobe APP14 transform 0).
    /// Larger files, but free of any color-conversion loss.
    /// </summary>
    Rgb,
}

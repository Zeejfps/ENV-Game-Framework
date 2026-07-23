namespace WebPSharp.Api;

/// <summary>
/// The interleaved channel layout of a <see cref="WebPImage"/>'s sample data.
/// </summary>
public enum WebPColorFormat
{
    /// <summary>Three interleaved channels: red, green, blue.</summary>
    Rgb = 3,

    /// <summary>Four interleaved channels: red, green, blue, alpha.</summary>
    Rgba = 4,
}

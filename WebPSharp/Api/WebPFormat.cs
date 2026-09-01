namespace WebPSharp.Api;

/// <summary>
/// The bitstream flavor of a WebP file as identified from its RIFF container.
/// </summary>
public enum WebPFormat
{
    /// <summary>A simple lossy file: a single <c>VP8&#160;</c> chunk.</summary>
    Lossy,

    /// <summary>A simple lossless file: a single <c>VP8L</c> chunk.</summary>
    Lossless,

    /// <summary>An extended file: a <c>VP8X</c> chunk followed by feature chunks.</summary>
    Extended,
}

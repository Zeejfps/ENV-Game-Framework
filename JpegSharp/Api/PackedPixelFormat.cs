namespace JpegSharp.Api;

/// <summary>
/// The channel ordering of a pixel packed into a 32-bit <see cref="int"/>. Each name lists the
/// channels from the most-significant byte to the least-significant, so the value is unambiguous
/// regardless of platform endianness. For example <see cref="Rgba8888"/> packs a pixel as
/// <c>(R &lt;&lt; 24) | (G &lt;&lt; 16) | (B &lt;&lt; 8) | A</c>.
/// </summary>
public enum PackedPixelFormat
{
    /// <summary>Red in the high byte, then green, blue, alpha in the low byte.</summary>
    Rgba8888,

    /// <summary>Alpha in the high byte, then red, green, blue in the low byte. Matches the int layout used by System.Drawing.</summary>
    Argb8888,

    /// <summary>Blue in the high byte, then green, red, alpha in the low byte.</summary>
    Bgra8888,

    /// <summary>Alpha in the high byte, then blue, green, red in the low byte.</summary>
    Abgr8888,
}

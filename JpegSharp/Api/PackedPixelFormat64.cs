namespace JpegSharp.Api;

/// <summary>
/// The channel ordering of a high-precision pixel packed into a 64-bit <see cref="long"/>, with
/// each channel occupying 16 bits. Named from the most-significant 16-bit lane to the least, so
/// the value is unambiguous regardless of endianness — for example <see cref="Rgba16161616"/>
/// packs a pixel as <c>(R &lt;&lt; 48) | (G &lt;&lt; 32) | (B &lt;&lt; 16) | A</c>.
/// </summary>
/// <remarks>
/// Channel values are the image's native, right-aligned samples in <c>[0, MaxSampleValue]</c>
/// (e.g. 0–4095 for 12-bit) — they are <em>not</em> rescaled to the full 16-bit range. Consumers
/// that expect full-range 16-bit channels should scale by the image's precision first.
/// </remarks>
public enum PackedPixelFormat64
{
    /// <summary>Red in the high lane, then green, blue, alpha in the low lane.</summary>
    Rgba16161616,

    /// <summary>Alpha in the high lane, then red, green, blue in the low lane.</summary>
    Argb16161616,

    /// <summary>Blue in the high lane, then green, red, alpha in the low lane.</summary>
    Bgra16161616,

    /// <summary>Alpha in the high lane, then blue, green, red in the low lane.</summary>
    Abgr16161616,
}

namespace WebPSharp.Container;

/// <summary>
/// The well-known FourCC identifiers used by the WebP RIFF container.
/// </summary>
public static class WebPChunkIds
{
    /// <summary>The outer RIFF form type.</summary>
    public static readonly FourCc Riff = new("RIFF");

    /// <summary>The WebP form identifier following the RIFF size.</summary>
    public static readonly FourCc WebP = new("WEBP");

    /// <summary>Simple lossy bitstream chunk (note the trailing space).</summary>
    public static readonly FourCc Vp8 = new("VP8 ");

    /// <summary>Simple lossless bitstream chunk.</summary>
    public static readonly FourCc Vp8L = new("VP8L");

    /// <summary>Extended-format header chunk.</summary>
    public static readonly FourCc Vp8X = new("VP8X");

    /// <summary>Alpha data chunk.</summary>
    public static readonly FourCc Alph = new("ALPH");

    /// <summary>Animation global parameters chunk.</summary>
    public static readonly FourCc Anim = new("ANIM");

    /// <summary>Animation frame chunk.</summary>
    public static readonly FourCc Anmf = new("ANMF");

    /// <summary>ICC color profile chunk.</summary>
    public static readonly FourCc Iccp = new("ICCP");

    /// <summary>EXIF metadata chunk.</summary>
    public static readonly FourCc Exif = new("EXIF");

    /// <summary>XMP metadata chunk (note the trailing space).</summary>
    public static readonly FourCc Xmp = new("XMP ");
}

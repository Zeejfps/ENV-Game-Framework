namespace JpegSharp.Api;

/// <summary>
/// The chroma subsampling layout applied to the two chrominance components when encoding a
/// color image. The luminance component always keeps full resolution.
/// </summary>
public enum ChromaSubsampling
{
    /// <summary>4:4:4 — no chroma subsampling (luma sampling 1x1).</summary>
    Samp444,

    /// <summary>4:2:2 — horizontal chroma halved (luma sampling 2x1).</summary>
    Samp422,

    /// <summary>4:2:0 — horizontal and vertical chroma halved (luma sampling 2x2).</summary>
    Samp420,

    /// <summary>4:1:1 — horizontal chroma quartered (luma sampling 4x1).</summary>
    Samp411,
}

/// <summary>Helpers for mapping <see cref="ChromaSubsampling"/> to luma sampling factors.</summary>
internal static class ChromaSubsamplingExtensions
{
    /// <summary>Returns the luminance horizontal and vertical sampling factors for a layout.</summary>
    /// <param name="subsampling">The subsampling layout.</param>
    /// <returns>The (horizontal, vertical) luma sampling factors.</returns>
    public static (int H, int V) LumaFactors(this ChromaSubsampling subsampling) => subsampling switch
    {
        ChromaSubsampling.Samp444 => (1, 1),
        ChromaSubsampling.Samp422 => (2, 1),
        ChromaSubsampling.Samp420 => (2, 2),
        ChromaSubsampling.Samp411 => (4, 1),
        _ => (1, 1),
    };
}

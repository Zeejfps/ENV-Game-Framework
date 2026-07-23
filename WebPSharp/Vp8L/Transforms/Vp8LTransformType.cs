namespace WebPSharp.Vp8L.Transforms;

/// <summary>
/// The four VP8L transforms, identified by the 2-bit type code in the bitstream.
/// </summary>
internal enum Vp8LTransformType
{
    /// <summary>Spatial predictor transform.</summary>
    Predictor = 0,

    /// <summary>Cross-color (green-to-red/blue) transform.</summary>
    CrossColor = 1,

    /// <summary>Subtract-green transform.</summary>
    SubtractGreen = 2,

    /// <summary>Color-indexing (palette) transform.</summary>
    ColorIndexing = 3,
}

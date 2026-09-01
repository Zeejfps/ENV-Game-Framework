namespace WebPSharp.Vp8L;

/// <summary>
/// Internal knobs controlling how <see cref="Vp8LEncoder"/> forms a VP8L bitstream. These select
/// which transforms to apply; entropy coding is always single-group, all-literal for now.
/// </summary>
internal sealed class Vp8LEncodeSettings
{
    /// <summary>Whether to apply the subtract-green transform.</summary>
    public bool SubtractGreen { get; init; }

    /// <summary>Whether to apply the predictor transform.</summary>
    public bool Predictor { get; init; }

    /// <summary>The uniform predictor mode (0..13) used when <see cref="Predictor"/> is set.</summary>
    public int PredictorMode { get; init; } = 2;

    /// <summary>The predictor tile size in bits (2..9) used when <see cref="Predictor"/> is set.</summary>
    public int PredictorBits { get; init; } = 4;

    /// <summary>Whether to apply the cross-color transform.</summary>
    public bool CrossColor { get; init; }

    /// <summary>The uniform green→red multiplier used when <see cref="CrossColor"/> is set.</summary>
    public byte CrossColorGreenToRed { get; init; }

    /// <summary>The uniform green→blue multiplier used when <see cref="CrossColor"/> is set.</summary>
    public byte CrossColorGreenToBlue { get; init; }

    /// <summary>The uniform red→blue multiplier used when <see cref="CrossColor"/> is set.</summary>
    public byte CrossColorRedToBlue { get; init; }

    /// <summary>The cross-color tile size in bits (2..9) used when <see cref="CrossColor"/> is set.</summary>
    public int CrossColorBits { get; init; } = 4;

    /// <summary>
    /// Whether to apply the color-indexing (palette) transform. Requires the image to contain at
    /// most 256 distinct colors; when set, it is used exclusively of the other transforms.
    /// </summary>
    public bool Palette { get; init; }

    /// <summary>
    /// Whether to use LZ77 back-references for the main image. Off produces an all-literal stream;
    /// on emits copy tokens, substantially shrinking repetitive content.
    /// </summary>
    public bool Lz77 { get; init; }

    /// <summary>
    /// Whether to use meta-Huffman (multiple Huffman groups selected per tile). When set, the main
    /// image is coded all-literal with per-tile group assignment. Mutually exclusive with the
    /// palette transform.
    /// </summary>
    public bool MetaHuffman { get; init; }

    /// <summary>The meta-Huffman tile size in bits (2..9).</summary>
    public int MetaHuffmanBits { get; init; } = 3;

    /// <summary>The number of Huffman groups to distribute across tiles when <see cref="MetaHuffman"/> is set.</summary>
    public int MetaHuffmanGroups { get; init; } = 4;

    /// <summary>
    /// The color-cache index bits (1..11), or 0 to disable the color cache. When enabled, pixels
    /// currently present in the hashed cache are coded as a compact index. Ignored by the
    /// meta-Huffman path.
    /// </summary>
    public int ColorCacheBits { get; init; }
}

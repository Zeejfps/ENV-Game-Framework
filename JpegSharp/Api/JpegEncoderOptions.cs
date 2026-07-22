using JpegSharp.Huffman;
using JpegSharp.Quantization;

namespace JpegSharp.Api;

/// <summary>
/// Options controlling JPEG encoding.
/// </summary>
public sealed class JpegEncoderOptions
{
    private int _quality = 75;

    /// <summary>
    /// The quality factor from 1 (smallest, lowest quality) to 100 (largest, highest quality).
    /// Defaults to 75. Values outside the range are clamped.
    /// </summary>
    public int Quality
    {
        get => _quality;
        set => _quality = Math.Clamp(value, 1, 100);
    }

    /// <summary>
    /// The chroma subsampling layout for color images. Defaults to 4:2:0. Ignored for
    /// grayscale images.
    /// </summary>
    public ChromaSubsampling Subsampling { get; set; } = ChromaSubsampling.Samp420;

    /// <summary>
    /// When true, the encoder performs a second pass to build entropy-optimal Huffman tables
    /// instead of using the standard Annex K tables. Produces smaller files at some CPU cost.
    /// </summary>
    public bool OptimizeHuffman { get; set; }

    /// <summary>
    /// When true, the encoder produces a progressive (SOF2) JPEG that separates the DC and AC
    /// coefficient bands into multiple scans. When false (the default), a baseline (SOF0)
    /// sequential JPEG is produced.
    /// </summary>
    public bool Progressive { get; set; }

    /// <summary>
    /// The number of MCUs between restart markers, or 0 to disable restart intervals.
    /// </summary>
    public int RestartInterval { get; set; }

    /// <summary>
    /// For RGB images, selects whether the encoder applies the YCbCr color transform (the
    /// default, smaller files) or stores the R, G, B channels directly without a transform
    /// (larger, but avoids any color-conversion loss). Ignored for non-RGB images.
    /// </summary>
    public JpegRgbEncoding RgbEncoding { get; set; } = JpegRgbEncoding.YCbCr;

    /// <summary>
    /// When true, CMYK images are encoded using the Adobe YCCK color transform (a YCbCr
    /// transform applied to the inverted CMY channels, APP14 transform 2) instead of storing
    /// CMYK directly (transform 0). Ignored for non-CMYK images.
    /// </summary>
    public bool CmykAsYcck { get; set; }

    /// <summary>
    /// A custom quantization table for the luminance component, overriding the quality-derived
    /// table. When null, the standard table scaled by <see cref="Quality"/> is used.
    /// </summary>
    public QuantizationTable? LuminanceQuantizationTable { get; set; }

    /// <summary>
    /// A custom quantization table for the chrominance components, overriding the
    /// quality-derived table. When null, the standard table scaled by <see cref="Quality"/> is used.
    /// </summary>
    public QuantizationTable? ChrominanceQuantizationTable { get; set; }

    /// <summary>Custom DC Huffman table for luminance. Ignored when <see cref="OptimizeHuffman"/> is true.</summary>
    public HuffmanTable? LuminanceDcHuffmanTable { get; set; }

    /// <summary>Custom AC Huffman table for luminance. Ignored when <see cref="OptimizeHuffman"/> is true.</summary>
    public HuffmanTable? LuminanceAcHuffmanTable { get; set; }

    /// <summary>Custom DC Huffman table for chrominance. Ignored when <see cref="OptimizeHuffman"/> is true.</summary>
    public HuffmanTable? ChrominanceDcHuffmanTable { get; set; }

    /// <summary>Custom AC Huffman table for chrominance. Ignored when <see cref="OptimizeHuffman"/> is true.</summary>
    public HuffmanTable? ChrominanceAcHuffmanTable { get; set; }

    /// <summary>
    /// Optional metadata (JFIF density, Exif, ICC profile, comments) to embed in the output.
    /// </summary>
    public JpegMetadata? Metadata { get; set; }
}

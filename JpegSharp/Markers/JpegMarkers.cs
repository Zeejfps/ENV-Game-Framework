namespace JpegSharp.Markers;

/// <summary>
/// The JPEG marker codes (the byte following the <c>0xFF</c> marker prefix) and helpers to
/// classify them, as defined in ITU-T T.81 Table B.1.
/// </summary>
internal static class JpegMarkers
{
    /// <summary>Start of image.</summary>
    public const byte StartOfImage = 0xD8;

    /// <summary>End of image.</summary>
    public const byte EndOfImage = 0xD9;

    /// <summary>Baseline sequential DCT frame (SOF0).</summary>
    public const byte StartOfFrameBaseline = 0xC0;

    /// <summary>Extended sequential DCT, Huffman coding (SOF1).</summary>
    public const byte StartOfFrameExtendedSequential = 0xC1;

    /// <summary>Progressive DCT, Huffman coding (SOF2).</summary>
    public const byte StartOfFrameProgressive = 0xC2;

    /// <summary>Lossless (sequential), Huffman coding (SOF3).</summary>
    public const byte StartOfFrameLossless = 0xC3;

    /// <summary>Define Huffman table(s).</summary>
    public const byte DefineHuffmanTables = 0xC4;

    /// <summary>Define arithmetic coding conditioning(s).</summary>
    public const byte DefineArithmeticConditioning = 0xCC;

    /// <summary>Define quantization table(s).</summary>
    public const byte DefineQuantizationTables = 0xDB;

    /// <summary>Define restart interval.</summary>
    public const byte DefineRestartInterval = 0xDD;

    /// <summary>Define number of lines.</summary>
    public const byte DefineNumberOfLines = 0xDC;

    /// <summary>Start of scan.</summary>
    public const byte StartOfScan = 0xDA;

    /// <summary>Comment.</summary>
    public const byte Comment = 0xFE;

    /// <summary>Temporary private use in arithmetic coding.</summary>
    public const byte Temporary = 0x01;

    /// <summary>First restart marker (RST0).</summary>
    public const byte Restart0 = 0xD0;

    /// <summary>Last restart marker (RST7).</summary>
    public const byte Restart7 = 0xD7;

    /// <summary>First application segment (APP0, JFIF).</summary>
    public const byte App0 = 0xE0;

    /// <summary>APP1 application segment (Exif / XMP).</summary>
    public const byte App1 = 0xE1;

    /// <summary>APP2 application segment (ICC profile).</summary>
    public const byte App2 = 0xE2;

    /// <summary>APP14 application segment (Adobe).</summary>
    public const byte App14 = 0xEE;

    /// <summary>Last application segment (APP15).</summary>
    public const byte App15 = 0xEF;

    /// <summary>Returns true if the marker is a restart marker (RST0–RST7).</summary>
    /// <param name="code">The marker code.</param>
    public static bool IsRestartMarker(byte code) => code is >= Restart0 and <= Restart7;

    /// <summary>Returns true if the marker is an application segment (APP0–APP15).</summary>
    /// <param name="code">The marker code.</param>
    public static bool IsAppMarker(byte code) => code is >= App0 and <= App15;

    /// <summary>
    /// Returns true if the marker begins a frame header (any Start-of-Frame variant, but not
    /// the DHT/DAC/DNL/JPGn markers that also fall in the 0xC0–0xCF range).
    /// </summary>
    /// <param name="code">The marker code.</param>
    public static bool IsStartOfFrame(byte code) =>
        code is (>= 0xC0 and <= 0xC3) or (>= 0xC5 and <= 0xC7) or (>= 0xC9 and <= 0xCB) or (>= 0xCD and <= 0xCF);

    /// <summary>
    /// Returns true if the marker carries a two-byte length field and payload. Standalone
    /// markers (SOI, EOI, RSTn, TEM) do not.
    /// </summary>
    /// <param name="code">The marker code.</param>
    public static bool HasLengthField(byte code)
    {
        if (IsRestartMarker(code))
            return false;
        return code is not (StartOfImage or EndOfImage or Temporary);
    }
}

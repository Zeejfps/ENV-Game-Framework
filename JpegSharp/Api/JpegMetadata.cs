namespace JpegSharp.Api;

/// <summary>
/// The unit of the JFIF pixel density fields.
/// </summary>
public enum JpegDensityUnit
{
    /// <summary>No units; the density values express only a pixel aspect ratio.</summary>
    None = 0,

    /// <summary>Pixels per inch.</summary>
    DotsPerInch = 1,

    /// <summary>Pixels per centimeter.</summary>
    DotsPerCentimeter = 2,
}

/// <summary>
/// The JFIF pixel density (APP0) describing physical resolution or aspect ratio.
/// </summary>
/// <param name="Unit">The density unit.</param>
/// <param name="X">Horizontal density.</param>
/// <param name="Y">Vertical density.</param>
public readonly record struct JfifDensity(JpegDensityUnit Unit, int X, int Y);

/// <summary>
/// A raw application (APPn) or otherwise unrecognized segment preserved verbatim so it can
/// round-trip through decode and encode.
/// </summary>
/// <param name="MarkerCode">The marker code (0xE0–0xEF for APP0–APP15).</param>
/// <param name="Data">The segment payload (excluding the marker and length bytes).</param>
public readonly record struct JpegApplicationSegment(byte MarkerCode, byte[] Data);

/// <summary>The kind of a header metadata segment recorded in the encounter-order manifest.</summary>
internal enum HeaderSegmentKind
{
    /// <summary>JFIF density (APP0).</summary>
    Jfif,

    /// <summary>Adobe color transform (APP14).</summary>
    Adobe,

    /// <summary>Exif (APP1).</summary>
    Exif,

    /// <summary>ICC profile (APP2); recorded once at the position of its first chunk.</summary>
    Icc,

    /// <summary>A comment (COM); <see cref="HeaderSegmentRef.Index"/> selects which comment.</summary>
    Comment,

    /// <summary>A preserved application segment; <see cref="HeaderSegmentRef.Index"/> selects which entry.</summary>
    App,
}

/// <summary>
/// A reference to one header metadata segment in on-disk encounter order, used to replay the
/// original segment ordering on re-encode.
/// </summary>
/// <param name="Kind">The segment kind.</param>
/// <param name="Index">For <see cref="HeaderSegmentKind.Comment"/> the index into the comment lists,
/// for <see cref="HeaderSegmentKind.App"/> the index into <see cref="JpegMetadata.ApplicationSegments"/>;
/// otherwise unused.</param>
internal readonly record struct HeaderSegmentRef(HeaderSegmentKind Kind, int Index);

/// <summary>
/// Optional metadata carried alongside a JPEG image: JFIF density, Exif, ICC profile, the
/// Adobe color transform, comment segments, and any preserved unrecognized application
/// segments. Any field may be null or empty.
/// </summary>
public sealed class JpegMetadata
{
    /// <summary>The JFIF pixel density from the APP0 segment, if present.</summary>
    public JfifDensity? Density { get; set; }

    /// <summary>The raw Exif payload (the bytes after the "Exif\0\0" APP1 identifier), if present.</summary>
    public byte[]? Exif { get; set; }

    /// <summary>The complete ICC profile reassembled from APP2 segments, if present.</summary>
    public byte[]? IccProfile { get; set; }

    /// <summary>The Adobe color transform from the APP14 segment (0, 1, or 2), if present.</summary>
    public int? AdobeColorTransform { get; set; }

    /// <summary>The comment (COM) segments in file order.</summary>
    public IList<string> Comments { get; } = new List<string>();

    /// <summary>
    /// The raw comment (COM) segment payloads in file order. COM data is defined as arbitrary
    /// bytes (T.81 B.2.4.5), so this is the lossless source of truth that round-trips binary or
    /// non-UTF-8 comments exactly. When non-empty it takes precedence over <see cref="Comments"/>
    /// on encode; <see cref="Comments"/> is a best-effort UTF-8 string view of the same data.
    /// </summary>
    public IList<byte[]> CommentBytes { get; } = new List<byte[]>();

    /// <summary>
    /// Application segments the codec does not interpret (any APPn that is not a recognized
    /// JFIF/Exif/ICC/Adobe segment), preserved in file order for round-tripping.
    /// </summary>
    public IList<JpegApplicationSegment> ApplicationSegments { get; } = new List<JpegApplicationSegment>();

    /// <summary>
    /// The on-disk encounter order of the header metadata segments, populated by the decoder so the
    /// encoder can replay the original ordering. Empty for user-constructed metadata, in which case
    /// the encoder falls back to its fixed segment order.
    /// </summary>
    internal IList<HeaderSegmentRef> HeaderSegmentOrder { get; } = new List<HeaderSegmentRef>();
}

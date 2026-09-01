namespace WebPSharp.Api;

/// <summary>
/// Lightweight structural information about a WebP stream, obtained by parsing only its RIFF
/// container and bitstream headers without decoding pixel data.
/// </summary>
public sealed class WebPInfo
{
    /// <summary>Creates a new info record.</summary>
    /// <param name="width">Canvas width in pixels.</param>
    /// <param name="height">Canvas height in pixels.</param>
    /// <param name="format">The container format (lossy, lossless, or extended).</param>
    /// <param name="hasAlpha">Whether the image carries an alpha channel.</param>
    /// <param name="hasAnimation">Whether the file contains animation frames.</param>
    /// <param name="hasIccProfile">Whether an ICC profile chunk is present.</param>
    /// <param name="hasExif">Whether an EXIF metadata chunk is present.</param>
    /// <param name="hasXmp">Whether an XMP metadata chunk is present.</param>
    /// <param name="frameCount">The number of animation frames (0 when not animated).</param>
    /// <param name="loopCount">The animation loop count (0 means infinite; 0 when not animated).</param>
    public WebPInfo(int width, int height, WebPFormat format, bool hasAlpha, bool hasAnimation,
        bool hasIccProfile = false, bool hasExif = false, bool hasXmp = false,
        int frameCount = 0, int loopCount = 0)
    {
        Width = width;
        Height = height;
        Format = format;
        HasAlpha = hasAlpha;
        HasAnimation = hasAnimation;
        HasIccProfile = hasIccProfile;
        HasExif = hasExif;
        HasXmp = hasXmp;
        FrameCount = frameCount;
        LoopCount = loopCount;
    }

    /// <summary>The canvas width in pixels.</summary>
    public int Width { get; }

    /// <summary>The canvas height in pixels.</summary>
    public int Height { get; }

    /// <summary>The container format of the file.</summary>
    public WebPFormat Format { get; }

    /// <summary>Whether the image carries an alpha channel.</summary>
    public bool HasAlpha { get; }

    /// <summary>Whether the file contains animation frames.</summary>
    public bool HasAnimation { get; }

    /// <summary>Whether an ICC color profile is present.</summary>
    public bool HasIccProfile { get; }

    /// <summary>Whether EXIF metadata is present.</summary>
    public bool HasExif { get; }

    /// <summary>Whether XMP metadata is present.</summary>
    public bool HasXmp { get; }

    /// <summary>The number of animation frames, or 0 when not animated.</summary>
    public int FrameCount { get; }

    /// <summary>The animation loop count (0 means infinite), or 0 when not animated.</summary>
    public int LoopCount { get; }

    /// <summary>Whether the primary image is coded losslessly.</summary>
    public bool IsLossless => Format == WebPFormat.Lossless;
}

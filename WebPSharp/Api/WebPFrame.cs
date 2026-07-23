namespace WebPSharp.Api;

/// <summary>
/// A single animation frame: an image placed at an even-aligned canvas offset, with a display
/// duration and blend/disposal behavior.
/// </summary>
public sealed class WebPFrame
{
    /// <summary>Creates an animation frame.</summary>
    /// <param name="image">The frame image.</param>
    /// <param name="x">The horizontal canvas offset (must be even and non-negative).</param>
    /// <param name="y">The vertical canvas offset (must be even and non-negative).</param>
    /// <param name="durationMs">The display duration in milliseconds.</param>
    /// <param name="blend">How the frame combines with the canvas.</param>
    /// <param name="disposal">What happens to the frame rectangle afterward.</param>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ArgumentException">An offset is negative or odd.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="durationMs"/> is negative.</exception>
    public WebPFrame(WebPImage image, int x = 0, int y = 0, int durationMs = 100,
        WebPBlendMethod blend = WebPBlendMethod.Over, WebPDisposalMethod disposal = WebPDisposalMethod.None)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (x < 0 || (x & 1) != 0)
            throw new ArgumentException("Frame X offset must be even and non-negative.", nameof(x));
        if (y < 0 || (y & 1) != 0)
            throw new ArgumentException("Frame Y offset must be even and non-negative.", nameof(y));
        ArgumentOutOfRangeException.ThrowIfNegative(durationMs);

        Image = image;
        X = x;
        Y = y;
        DurationMs = durationMs;
        Blend = blend;
        Disposal = disposal;
    }

    /// <summary>The frame image.</summary>
    public WebPImage Image { get; }

    /// <summary>The horizontal canvas offset in pixels.</summary>
    public int X { get; }

    /// <summary>The vertical canvas offset in pixels.</summary>
    public int Y { get; }

    /// <summary>The display duration in milliseconds.</summary>
    public int DurationMs { get; }

    /// <summary>How the frame combines with the canvas.</summary>
    public WebPBlendMethod Blend { get; }

    /// <summary>What happens to the frame rectangle after display.</summary>
    public WebPDisposalMethod Disposal { get; }

    /// <summary>The frame width in pixels.</summary>
    public int Width => Image.Width;

    /// <summary>The frame height in pixels.</summary>
    public int Height => Image.Height;
}

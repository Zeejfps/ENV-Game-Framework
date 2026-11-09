namespace ZGF.Gui.Testing.Gpu;

/// <summary>A render target's logical size in points, its DPI scale, and the physical pixel size that
/// follows from them, rounded as the canvas rounds its framebuffer.</summary>
public sealed record TargetSize
{
    private TargetSize(int width, int height, float dpiScale, int pixelWidth, int pixelHeight)
    {
        Width = width;
        Height = height;
        DpiScale = dpiScale;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        ByteCount = pixelWidth * pixelHeight * 4;
    }

    public int Width { get; }
    public int Height { get; }
    public float DpiScale { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }

    /// <summary>The size of the target's pixels as RGBA8.</summary>
    public int ByteCount { get; }

    /// <exception cref="ArgumentOutOfRangeException">A size is not positive, the scale is not a positive finite
    /// number, or the RGBA8 pixels would not fit in one array.</exception>
    public static TargetSize Of(int width, int height, float dpiScale = 1f)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (!float.IsFinite(dpiScale) || dpiScale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(dpiScale), dpiScale, "The DPI scale must be a positive finite number.");

        var pixelWidth = PixelLength(width, dpiScale, nameof(width));
        var pixelHeight = PixelLength(height, dpiScale, nameof(height));
        var pixels = (long)pixelWidth * pixelHeight;
        if (pixels > Array.MaxLength / 4)
            throw new ArgumentOutOfRangeException(nameof(width),
                $"{pixelWidth}x{pixelHeight} pixels of RGBA8 are more than one array holds ({Array.MaxLength} bytes).");
        return new TargetSize(width, height, dpiScale, pixelWidth, pixelHeight);
    }

    private static int PixelLength(int logical, float dpiScale, string name)
    {
        var pixels = MathF.Round(logical * dpiScale);
        if (pixels >= int.MaxValue)
            throw new ArgumentOutOfRangeException(name, $"{logical} points at scale {dpiScale} is more pixels than fit in an int.");
        return Math.Max(1, (int)pixels);
    }
}

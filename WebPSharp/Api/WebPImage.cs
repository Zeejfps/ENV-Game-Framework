namespace WebPSharp.Api;

/// <summary>
/// An in-memory raster image exchanged with the WebP encoder and decoder. Samples are 8-bit
/// and stored interleaved in row-major order with <see cref="ComponentCount"/> channels per
/// pixel (RGB or RGBA).
/// </summary>
public sealed class WebPImage
{
    /// <summary>Creates an image from raw interleaved pixel data.</summary>
    /// <param name="width">Image width in pixels (must be positive).</param>
    /// <param name="height">Image height in pixels (must be positive).</param>
    /// <param name="format">The channel layout of the sample data.</param>
    /// <param name="pixelData">
    /// Interleaved samples, length <c>width * height * ComponentCount</c>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="pixelData"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive.</exception>
    /// <exception cref="ArgumentException">The pixel buffer length does not match the dimensions.</exception>
    public WebPImage(int width, int height, WebPColorFormat format, byte[] pixelData)
    {
        ArgumentNullException.ThrowIfNull(pixelData);
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

        var components = ComponentsFor(format);
        var expected = (long)width * height * components;
        if (pixelData.Length != expected)
            throw new ArgumentException($"Pixel data length {pixelData.Length} does not match {width}x{height}x{components} = {expected}.", nameof(pixelData));

        Width = width;
        Height = height;
        Format = format;
        ComponentCount = components;
        PixelData = pixelData;
    }

    /// <summary>The image width in pixels.</summary>
    public int Width { get; }

    /// <summary>The image height in pixels.</summary>
    public int Height { get; }

    /// <summary>The channel layout of the sample data.</summary>
    public WebPColorFormat Format { get; }

    /// <summary>The number of interleaved channels per pixel (3 for RGB, 4 for RGBA).</summary>
    public int ComponentCount { get; }

    /// <summary>The interleaved sample data in row-major order.</summary>
    public byte[] PixelData { get; }

    /// <summary>Whether the image carries an alpha channel.</summary>
    public bool HasAlpha => Format == WebPColorFormat.Rgba;

    /// <summary>The number of bytes per pixel row (<c>Width * ComponentCount</c>).</summary>
    public int Stride => Width * ComponentCount;

    /// <summary>
    /// Metadata associated with the image. Populated by the decoder; may be set before
    /// encoding via <see cref="WebPImage.Metadata"/> or supplied through encoder options.
    /// </summary>
    public WebPMetadata? Metadata { get; set; }

    /// <summary>Creates a three-channel RGB image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="pixels">Interleaved R,G,B samples.</param>
    /// <returns>The RGB image.</returns>
    public static WebPImage CreateRgb(int width, int height, byte[] pixels) =>
        new(width, height, WebPColorFormat.Rgb, pixels);

    /// <summary>Creates a four-channel RGBA image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="pixels">Interleaved R,G,B,A samples.</param>
    /// <returns>The RGBA image.</returns>
    public static WebPImage CreateRgba(int width, int height, byte[] pixels) =>
        new(width, height, WebPColorFormat.Rgba, pixels);

    /// <summary>Returns the number of channels for a color format.</summary>
    /// <param name="format">The color format.</param>
    /// <returns>The channel count.</returns>
    public static int ComponentsFor(WebPColorFormat format) => format switch
    {
        WebPColorFormat.Rgb => 3,
        WebPColorFormat.Rgba => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
}

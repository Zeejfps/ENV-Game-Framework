namespace JpegSharp.Api;

/// <summary>
/// An in-memory raster image exchanged with the JPEG encoder and decoder. Samples are 8-bit
/// and stored interleaved in row-major order with <see cref="ComponentCount"/> channels per
/// pixel.
/// </summary>
public sealed class JpegImage
{
    /// <summary>Creates an image from raw interleaved pixel data.</summary>
    /// <param name="width">Image width in pixels (must be positive).</param>
    /// <param name="height">Image height in pixels (must be positive).</param>
    /// <param name="colorSpace">The color space of the sample data.</param>
    /// <param name="pixelData">
    /// Interleaved samples, length <c>width * height * ComponentCount</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive.</exception>
    /// <exception cref="ArgumentException">The pixel buffer length does not match the dimensions.</exception>
    public JpegImage(int width, int height, JpegColorSpace colorSpace, byte[] pixelData)
    {
        ArgumentNullException.ThrowIfNull(pixelData);
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

        var components = ComponentsFor(colorSpace);
        var expected = (long)width * height * components;
        if (pixelData.Length != expected)
            throw new ArgumentException($"Pixel data length {pixelData.Length} does not match {width}x{height}x{components} = {expected}.", nameof(pixelData));

        Width = width;
        Height = height;
        ColorSpace = colorSpace;
        ComponentCount = components;
        PixelData = pixelData;
    }

    /// <summary>The image width in pixels.</summary>
    public int Width { get; }

    /// <summary>The image height in pixels.</summary>
    public int Height { get; }

    /// <summary>The color space of the sample data.</summary>
    public JpegColorSpace ColorSpace { get; }

    /// <summary>The number of interleaved channels per pixel (1 grayscale, 3 RGB, 4 CMYK).</summary>
    public int ComponentCount { get; }

    /// <summary>The interleaved sample data in row-major order.</summary>
    public byte[] PixelData { get; }

    /// <summary>
    /// Metadata associated with the image. Populated by the decoder; may be set before
    /// encoding via <see cref="JpegEncoderOptions.Metadata"/>.
    /// </summary>
    public JpegMetadata? Metadata { get; set; }

    /// <summary>Creates a single-channel grayscale image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="pixels">Grayscale samples, one byte per pixel.</param>
    /// <returns>The grayscale image.</returns>
    public static JpegImage CreateGrayscale(int width, int height, byte[] pixels) =>
        new(width, height, JpegColorSpace.Grayscale, pixels);

    /// <summary>Creates a three-channel RGB image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="pixels">Interleaved R,G,B samples.</param>
    /// <returns>The RGB image.</returns>
    public static JpegImage CreateRgb(int width, int height, byte[] pixels) =>
        new(width, height, JpegColorSpace.Rgb, pixels);

    /// <summary>Creates a four-channel CMYK image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="pixels">Interleaved C,M,Y,K samples.</param>
    /// <returns>The CMYK image.</returns>
    public static JpegImage CreateCmyk(int width, int height, byte[] pixels) =>
        new(width, height, JpegColorSpace.Cmyk, pixels);

    /// <summary>Returns the number of channels for a color space.</summary>
    /// <param name="colorSpace">The color space.</param>
    /// <returns>The channel count.</returns>
    public static int ComponentsFor(JpegColorSpace colorSpace) => colorSpace switch
    {
        JpegColorSpace.Grayscale => 1,
        JpegColorSpace.Rgb => 3,
        JpegColorSpace.Cmyk => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(colorSpace)),
    };
}

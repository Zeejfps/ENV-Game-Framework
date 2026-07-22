using JpegSharp.Color;

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

    /// <summary>
    /// Returns an RGB copy of this image. Grayscale luminance is replicated across R, G and B,
    /// and CMYK is converted with the standard multiplicative model (the decoder has already
    /// undone any Adobe inversion / YCCK transform). An image that is already RGB is copied so
    /// the result never shares its <see cref="PixelData"/> with the original. <see cref="Metadata"/>
    /// is carried over to the result by reference.
    /// </summary>
    /// <returns>A new RGB <see cref="JpegImage"/> of the same dimensions.</returns>
    public JpegImage ToRgb()
    {
        var pixelCount = Width * Height;
        var rgb = new byte[pixelCount * 3];
        var src = PixelData;

        switch (ColorSpace)
        {
            case JpegColorSpace.Rgb:
                Array.Copy(src, rgb, rgb.Length);
                break;

            case JpegColorSpace.Grayscale:
                for (var i = 0; i < pixelCount; i++)
                {
                    var v = src[i];
                    var o = i * 3;
                    rgb[o] = v;
                    rgb[o + 1] = v;
                    rgb[o + 2] = v;
                }
                break;

            case JpegColorSpace.Cmyk:
                for (var i = 0; i < pixelCount; i++)
                {
                    var s = i * 4;
                    var o = i * 3;
                    ColorConverter.CmykToRgb(src[s], src[s + 1], src[s + 2], src[s + 3], out rgb[o], out rgb[o + 1], out rgb[o + 2]);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(ColorSpace));
        }

        return new JpegImage(Width, Height, JpegColorSpace.Rgb, rgb) { Metadata = Metadata };
    }

    /// <summary>
    /// Converts the image to one packed 32-bit color per pixel, in row-major order. Grayscale
    /// samples are replicated across R, G and B, and CMYK samples are converted to RGB with the
    /// standard multiplicative model. Alpha is always fully opaque (255) since JPEG carries no
    /// alpha channel.
    /// </summary>
    /// <param name="format">The channel ordering of each packed pixel.</param>
    /// <returns>An array of <c>Width * Height</c> packed pixels.</returns>
    public int[] ToPackedPixels(PackedPixelFormat format)
    {
        var destination = new int[Width * Height];
        ToPackedPixels(destination, format);
        return destination;
    }

    /// <summary>
    /// Converts the image to one packed 32-bit color per pixel, writing into a caller-supplied
    /// span to avoid an allocation. See <see cref="ToPackedPixels(PackedPixelFormat)"/> for the
    /// channel, color-space and alpha semantics.
    /// </summary>
    /// <param name="destination">
    /// The buffer to fill, in row-major order. Must be at least <c>Width * Height</c> long; any
    /// extra elements are left untouched.
    /// </param>
    /// <param name="format">The channel ordering of each packed pixel.</param>
    /// <exception cref="ArgumentException">The destination is too small to hold every pixel.</exception>
    public void ToPackedPixels(Span<int> destination, PackedPixelFormat format)
    {
        var pixelCount = Width * Height;
        if (destination.Length < pixelCount)
            throw new ArgumentException($"Destination length {destination.Length} is smaller than the pixel count {pixelCount}.", nameof(destination));

        // Resolve the per-channel bit offsets once so the packing loop is a handful of shifts.
        var (rShift, gShift, bShift, aShift) = ShiftsFor(format);
        var alpha = 255 << aShift;
        var src = PixelData;

        switch (ColorSpace)
        {
            case JpegColorSpace.Grayscale:
                for (var i = 0; i < pixelCount; i++)
                {
                    int v = src[i];
                    destination[i] = (v << rShift) | (v << gShift) | (v << bShift) | alpha;
                }
                break;

            case JpegColorSpace.Rgb:
                for (var i = 0; i < pixelCount; i++)
                {
                    var o = i * 3;
                    int r = src[o], g = src[o + 1], b = src[o + 2];
                    destination[i] = (r << rShift) | (g << gShift) | (b << bShift) | alpha;
                }
                break;

            case JpegColorSpace.Cmyk:
                // PixelData holds normalized CMYK (the decoder has already undone any Adobe
                // inversion / YCCK transform), so the standard multiplicative model applies.
                for (var i = 0; i < pixelCount; i++)
                {
                    var o = i * 4;
                    ColorConverter.CmykToRgb(src[o], src[o + 1], src[o + 2], src[o + 3], out var r, out var g, out var b);
                    destination[i] = (r << rShift) | (g << gShift) | (b << bShift) | alpha;
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(ColorSpace));
        }
    }

    // Bit offset of each channel's byte within the packed int for a given format.
    private static (int R, int G, int B, int A) ShiftsFor(PackedPixelFormat format) => format switch
    {
        PackedPixelFormat.Rgba8888 => (24, 16, 8, 0),
        PackedPixelFormat.Argb8888 => (16, 8, 0, 24),
        PackedPixelFormat.Bgra8888 => (8, 16, 24, 0),
        PackedPixelFormat.Abgr8888 => (0, 8, 16, 24),
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
}

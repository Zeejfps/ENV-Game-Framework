using JpegSharp.Color;

namespace JpegSharp.Api;

/// <summary>
/// An in-memory raster image with 9–16 bit samples (typically 12-bit), stored interleaved in
/// row-major order as <see cref="ushort"/> values. Samples are right-aligned in
/// <c>[0, MaxSampleValue]</c> — the literal values from the JPEG file, not rescaled to the full
/// 16-bit range. For the common 8-bit case use <see cref="JpegImage"/>; call <see cref="To8Bit"/>
/// to bridge to it (and thereby to every 8-bit helper such as encoding, <c>ToRgb</c>, or the
/// 32-bit packing helpers).
/// </summary>
public sealed class JpegImage16 : IJpegImage
{
    /// <summary>Creates a high-precision image from raw interleaved samples.</summary>
    /// <param name="width">Image width in pixels (must be positive).</param>
    /// <param name="height">Image height in pixels (must be positive).</param>
    /// <param name="colorSpace">The color space of the sample data.</param>
    /// <param name="precision">The sample bit precision, 9–16.</param>
    /// <param name="pixelData">Interleaved samples, length <c>width * height * ComponentCount</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive, or the precision is outside 9–16.</exception>
    /// <exception cref="ArgumentException">The pixel buffer length does not match the dimensions.</exception>
    public JpegImage16(int width, int height, JpegColorSpace colorSpace, int precision, ushort[] pixelData)
    {
        ArgumentNullException.ThrowIfNull(pixelData);
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        if (precision is < 9 or > 16)
            throw new ArgumentOutOfRangeException(nameof(precision), "JpegImage16 is for 9–16 bit samples; use JpegImage for 8-bit.");

        var components = JpegImage.ComponentsFor(colorSpace);
        var expected = (long)width * height * components;
        if (pixelData.Length != expected)
            throw new ArgumentException($"Pixel data length {pixelData.Length} does not match {width}x{height}x{components} = {expected}.", nameof(pixelData));

        Width = width;
        Height = height;
        ColorSpace = colorSpace;
        ComponentCount = components;
        Precision = precision;
        PixelData = pixelData;
    }

    /// <inheritdoc />
    public int Width { get; }

    /// <inheritdoc />
    public int Height { get; }

    /// <inheritdoc />
    public JpegColorSpace ColorSpace { get; }

    /// <inheritdoc />
    public int ComponentCount { get; }

    /// <inheritdoc />
    public int Precision { get; }

    /// <summary>The largest valid sample value, <c>(1 &lt;&lt; Precision) - 1</c> (e.g. 4095 for 12-bit).</summary>
    public int MaxSampleValue => (1 << Precision) - 1;

    /// <summary>The interleaved sample data in row-major order; values in <c>[0, MaxSampleValue]</c>.</summary>
    public ushort[] PixelData { get; }

    /// <inheritdoc cref="JpegImage.Metadata" />
    public JpegMetadata? Metadata { get; set; }

    /// <summary>Creates a single-channel high-precision grayscale image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="precision">The sample bit precision, 9–16.</param>
    /// <param name="pixels">Grayscale samples, one value per pixel.</param>
    /// <returns>The grayscale image.</returns>
    public static JpegImage16 CreateGrayscale(int width, int height, int precision, ushort[] pixels) =>
        new(width, height, JpegColorSpace.Grayscale, precision, pixels);

    /// <summary>Creates a three-channel high-precision RGB image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="precision">The sample bit precision, 9–16.</param>
    /// <param name="pixels">Interleaved R,G,B samples.</param>
    /// <returns>The RGB image.</returns>
    public static JpegImage16 CreateRgb(int width, int height, int precision, ushort[] pixels) =>
        new(width, height, JpegColorSpace.Rgb, precision, pixels);

    /// <summary>Creates a four-channel high-precision CMYK image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="precision">The sample bit precision, 9–16.</param>
    /// <param name="pixels">Interleaved C,M,Y,K samples.</param>
    /// <returns>The CMYK image.</returns>
    public static JpegImage16 CreateCmyk(int width, int height, int precision, ushort[] pixels) =>
        new(width, height, JpegColorSpace.Cmyk, precision, pixels);

    /// <summary>
    /// Down-samples to an 8-bit <see cref="JpegImage"/> by right-shifting each sample by
    /// <c>Precision - 8</c>. This is the bridge to every 8-bit API: encoding, <c>ToRgb</c>, and
    /// the 32-bit packing helpers. Color space and <see cref="Metadata"/> are preserved.
    /// </summary>
    /// <returns>An 8-bit image of the same dimensions and color space.</returns>
    public JpegImage To8Bit()
    {
        var shift = Precision - 8;
        var src = PixelData;
        var dst = new byte[src.Length];
        for (var i = 0; i < src.Length; i++)
            dst[i] = (byte)(src[i] >> shift);

        return new JpegImage(Width, Height, ColorSpace, dst) { Metadata = Metadata };
    }

    /// <summary>
    /// Produces an 8-bit RGBA preview by tone-mapping via <see cref="To8Bit"/>. Grayscale and
    /// CMYK are handled through the 8-bit conversion. For full-precision output use
    /// <see cref="ToPackedPixels64(PackedPixelFormat64)"/>.
    /// </summary>
    /// <returns>An array of <c>Width * Height</c> RGBA-packed 8-bit pixels.</returns>
    public int[] ToRgba8888() => To8Bit().ToPackedPixels(PackedPixelFormat.Rgba8888);

    /// <summary>
    /// Converts the image to one packed 64-bit color per pixel (16 bits per channel), in
    /// row-major order, preserving full precision. Channel values are the native right-aligned
    /// samples in <c>[0, MaxSampleValue]</c>; grayscale is replicated across R, G and B, CMYK is
    /// converted to RGB with the standard multiplicative model, and alpha is set to
    /// <see cref="MaxSampleValue"/> (fully opaque in the sample scale).
    /// </summary>
    /// <param name="format">The channel ordering of each packed pixel.</param>
    /// <returns>An array of <c>Width * Height</c> packed pixels.</returns>
    public long[] ToPackedPixels64(PackedPixelFormat64 format)
    {
        var destination = new long[Width * Height];
        ToPackedPixels64(destination, format);
        return destination;
    }

    /// <summary>
    /// Converts the image to one packed 64-bit color per pixel, writing into a caller-supplied
    /// span to avoid an allocation. See <see cref="ToPackedPixels64(PackedPixelFormat64)"/> for
    /// the channel and alpha semantics.
    /// </summary>
    /// <param name="destination">The buffer to fill, at least <c>Width * Height</c> long; extra elements are left untouched.</param>
    /// <param name="format">The channel ordering of each packed pixel.</param>
    /// <exception cref="ArgumentException">The destination is too small to hold every pixel.</exception>
    public void ToPackedPixels64(Span<long> destination, PackedPixelFormat64 format)
    {
        var pixelCount = Width * Height;
        if (destination.Length < pixelCount)
            throw new ArgumentException($"Destination length {destination.Length} is smaller than the pixel count {pixelCount}.", nameof(destination));

        var (rShift, gShift, bShift, aShift) = ShiftsFor(format);
        var alpha = (long)MaxSampleValue << aShift;
        var src = PixelData;

        switch (ColorSpace)
        {
            case JpegColorSpace.Grayscale:
                for (var i = 0; i < pixelCount; i++)
                {
                    long v = src[i];
                    destination[i] = (v << rShift) | (v << gShift) | (v << bShift) | alpha;
                }
                break;

            case JpegColorSpace.Rgb:
                for (var i = 0; i < pixelCount; i++)
                {
                    var o = i * 3;
                    long r = src[o], g = src[o + 1], b = src[o + 2];
                    destination[i] = (r << rShift) | (g << gShift) | (b << bShift) | alpha;
                }
                break;

            case JpegColorSpace.Cmyk:
                var maxValue = MaxSampleValue;
                for (var i = 0; i < pixelCount; i++)
                {
                    var o = i * 4;
                    ColorConverter.CmykToRgb(src[o], src[o + 1], src[o + 2], src[o + 3], maxValue, out var r, out var g, out var b);
                    destination[i] = ((long)r << rShift) | ((long)g << gShift) | ((long)b << bShift) | alpha;
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(ColorSpace));
        }
    }

    /// <summary>
    /// Creates a high-precision RGB image from one packed 64-bit color per pixel. This is the
    /// inverse of <see cref="ToPackedPixels64(PackedPixelFormat64)"/>. Alpha is discarded, since
    /// JPEG stores no alpha; channel values are taken as native samples in <c>[0, MaxSampleValue]</c>.
    /// </summary>
    /// <param name="width">Image width in pixels (must be positive).</param>
    /// <param name="height">Image height in pixels (must be positive).</param>
    /// <param name="precision">The sample bit precision, 9–16.</param>
    /// <param name="pixels">The packed pixels, exactly <c>width * height</c> long.</param>
    /// <param name="format">The channel ordering of each packed pixel.</param>
    /// <returns>The RGB image.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive, or the precision is outside 9–16.</exception>
    /// <exception cref="ArgumentException">The pixel count does not match the dimensions.</exception>
    public static JpegImage16 CreateFromPackedPixels64(int width, int height, int precision, ReadOnlySpan<long> pixels, PackedPixelFormat64 format)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

        var pixelCount = width * height;
        if (pixels.Length != pixelCount)
            throw new ArgumentException($"Pixel count {pixels.Length} does not match {width}x{height} = {pixelCount}.", nameof(pixels));

        var (rShift, gShift, bShift, _) = ShiftsFor(format);
        var rgb = new ushort[pixelCount * 3];
        for (var i = 0; i < pixelCount; i++)
        {
            var px = pixels[i];
            var o = i * 3;
            rgb[o] = (ushort)(px >> rShift);
            rgb[o + 1] = (ushort)(px >> gShift);
            rgb[o + 2] = (ushort)(px >> bShift);
        }

        return new JpegImage16(width, height, JpegColorSpace.Rgb, precision, rgb);
    }

    /// <summary>Creates a high-precision RGB image from RGBA-packed pixels. Alpha is discarded.</summary>
    public static JpegImage16 CreateFromRgba16161616(int width, int height, int precision, ReadOnlySpan<long> pixels) =>
        CreateFromPackedPixels64(width, height, precision, pixels, PackedPixelFormat64.Rgba16161616);

    /// <summary>Creates a high-precision RGB image from ARGB-packed pixels. Alpha is discarded.</summary>
    public static JpegImage16 CreateFromArgb16161616(int width, int height, int precision, ReadOnlySpan<long> pixels) =>
        CreateFromPackedPixels64(width, height, precision, pixels, PackedPixelFormat64.Argb16161616);

    /// <summary>Creates a high-precision RGB image from BGRA-packed pixels. Alpha is discarded.</summary>
    public static JpegImage16 CreateFromBgra16161616(int width, int height, int precision, ReadOnlySpan<long> pixels) =>
        CreateFromPackedPixels64(width, height, precision, pixels, PackedPixelFormat64.Bgra16161616);

    /// <summary>Creates a high-precision RGB image from ABGR-packed pixels. Alpha is discarded.</summary>
    public static JpegImage16 CreateFromAbgr16161616(int width, int height, int precision, ReadOnlySpan<long> pixels) =>
        CreateFromPackedPixels64(width, height, precision, pixels, PackedPixelFormat64.Abgr16161616);

    // Bit offset of each channel's 16-bit lane within the packed long. The 8-bit ShiftsFor
    // offsets, doubled: each channel is 16 bits wide instead of 8.
    private static (int R, int G, int B, int A) ShiftsFor(PackedPixelFormat64 format) => format switch
    {
        PackedPixelFormat64.Rgba16161616 => (48, 32, 16, 0),
        PackedPixelFormat64.Argb16161616 => (32, 16, 0, 48),
        PackedPixelFormat64.Bgra16161616 => (16, 32, 48, 0),
        PackedPixelFormat64.Abgr16161616 => (0, 16, 32, 48),
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
}

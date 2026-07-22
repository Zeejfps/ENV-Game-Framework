using System.Runtime.CompilerServices;

namespace JpegSharp.Color;

/// <summary>
/// Converts between the color spaces used by JPEG: RGB, JFIF YCbCr (ITU-R BT.601),
/// CMYK, and Adobe YCCK. All transforms use 16-bit fixed-point arithmetic for determinism
/// and speed, matching the coefficients used by libjpeg.
/// </summary>
internal static class ColorConverter
{
    private const int ScaleBits = 16;
    private const int Half = 1 << (ScaleBits - 1);

    // RGB -> YCbCr forward coefficients (value * 65536).
    private const int YR = 19595, YG = 38470, YB = 7471;      // 0.29900, 0.58700, 0.11400
    private const int CbR = -11059, CbG = -21709, CbB = 32768; // -0.16874, -0.33126, 0.50000
    private const int CrR = 32768, CrG = -27439, CrB = -5329;  // 0.50000, -0.41869, -0.08131

    // YCbCr -> RGB inverse coefficients (value * 65536).
    private const int CrToR = 91881;  // 1.40200
    private const int CbToG = 22554;  // 0.34414
    private const int CrToG = 46802;  // 0.71414
    private const int CbToB = 116130; // 1.77200

    /// <summary>Converts a single RGB pixel to JFIF YCbCr.</summary>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    /// <param name="y">Resulting luma.</param>
    /// <param name="cb">Resulting blue-difference chroma.</param>
    /// <param name="cr">Resulting red-difference chroma.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RgbToYCbCr(byte r, byte g, byte b, out byte y, out byte cb, out byte cr)
    {
        y = Clamp((YR * r + YG * g + YB * b + Half) >> ScaleBits);
        cb = Clamp((CbR * r + CbG * g + CbB * b + (128 << ScaleBits) + Half) >> ScaleBits);
        cr = Clamp((CrR * r + CrG * g + CrB * b + (128 << ScaleBits) + Half) >> ScaleBits);
    }

    /// <summary>Converts a single JFIF YCbCr pixel to RGB.</summary>
    /// <param name="y">Luma.</param>
    /// <param name="cb">Blue-difference chroma.</param>
    /// <param name="cr">Red-difference chroma.</param>
    /// <param name="r">Resulting red channel.</param>
    /// <param name="g">Resulting green channel.</param>
    /// <param name="b">Resulting blue channel.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void YCbCrToRgb(byte y, byte cb, byte cr, out byte r, out byte g, out byte b)
    {
        int c = cr - 128;
        int d = cb - 128;
        r = Clamp(y + ((CrToR * c + Half) >> ScaleBits));
        g = Clamp(y - ((CbToG * d + CrToG * c + Half) >> ScaleBits));
        b = Clamp(y + ((CbToB * d + Half) >> ScaleBits));
    }

    /// <summary>
    /// Converts a single high-precision JFIF YCbCr pixel to RGB. The chroma center and output
    /// clamp scale with <paramref name="maxValue"/> (<c>(1 &lt;&lt; precision) - 1</c>); the
    /// coefficients themselves are precision-independent.
    /// </summary>
    /// <param name="y">Luma.</param>
    /// <param name="cb">Blue-difference chroma.</param>
    /// <param name="cr">Red-difference chroma.</param>
    /// <param name="maxValue">The maximum sample value, e.g. 4095 for 12-bit.</param>
    /// <param name="r">Resulting red channel.</param>
    /// <param name="g">Resulting green channel.</param>
    /// <param name="b">Resulting blue channel.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void YCbCrToRgb(int y, int cb, int cr, int maxValue, out ushort r, out ushort g, out ushort b)
    {
        var center = (maxValue + 1) >> 1;
        int c = cr - center;
        int d = cb - center;
        r = ClampTo(y + ((CrToR * c + Half) >> ScaleBits), maxValue);
        g = ClampTo(y - ((CbToG * d + CrToG * c + Half) >> ScaleBits), maxValue);
        b = ClampTo(y + ((CbToB * d + Half) >> ScaleBits), maxValue);
    }

    /// <summary>Converts a single high-precision RGB pixel to JFIF YCbCr.</summary>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    /// <param name="maxValue">The maximum sample value, e.g. 4095 for 12-bit.</param>
    /// <param name="y">Resulting luma.</param>
    /// <param name="cb">Resulting blue-difference chroma.</param>
    /// <param name="cr">Resulting red-difference chroma.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RgbToYCbCr(int r, int g, int b, int maxValue, out ushort y, out ushort cb, out ushort cr)
    {
        var center = (maxValue + 1) >> 1;
        y = ClampTo((YR * r + YG * g + YB * b + Half) >> ScaleBits, maxValue);
        cb = ClampTo((CbR * r + CbG * g + CbB * b + (center << ScaleBits) + Half) >> ScaleBits, maxValue);
        cr = ClampTo((CrR * r + CrG * g + CrB * b + (center << ScaleBits) + Half) >> ScaleBits, maxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ClampTo(int value, int max) => (ushort)(value < 0 ? 0 : value > max ? max : value);

    /// <summary>Converts interleaved RGB samples to three YCbCr planes.</summary>
    /// <param name="rgb">Interleaved R,G,B triples.</param>
    /// <param name="y">Destination luma plane.</param>
    /// <param name="cb">Destination Cb plane.</param>
    /// <param name="cr">Destination Cr plane.</param>
    public static void RgbToYCbCr(ReadOnlySpan<byte> rgb, Span<byte> y, Span<byte> cb, Span<byte> cr)
    {
        var n = y.Length;
        for (var i = 0; i < n; i++)
            RgbToYCbCr(rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2], out y[i], out cb[i], out cr[i]);
    }

    /// <summary>Converts three YCbCr planes to interleaved RGB samples.</summary>
    /// <param name="y">Luma plane.</param>
    /// <param name="cb">Cb plane.</param>
    /// <param name="cr">Cr plane.</param>
    /// <param name="rgb">Destination interleaved R,G,B triples.</param>
    public static void YCbCrToRgb(ReadOnlySpan<byte> y, ReadOnlySpan<byte> cb, ReadOnlySpan<byte> cr, Span<byte> rgb)
    {
        var n = y.Length;
        for (var i = 0; i < n; i++)
            YCbCrToRgb(y[i], cb[i], cr[i], out rgb[i * 3], out rgb[i * 3 + 1], out rgb[i * 3 + 2]);
    }

    /// <summary>Converts a CMYK pixel to RGB using the standard multiplicative model.</summary>
    /// <param name="c">Cyan channel.</param>
    /// <param name="m">Magenta channel.</param>
    /// <param name="y">Yellow channel.</param>
    /// <param name="k">Black channel.</param>
    /// <param name="r">Resulting red channel.</param>
    /// <param name="g">Resulting green channel.</param>
    /// <param name="b">Resulting blue channel.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CmykToRgb(byte c, byte m, byte y, byte k, out byte r, out byte g, out byte b)
    {
        r = (byte)((255 - c) * (255 - k) / 255);
        g = (byte)((255 - m) * (255 - k) / 255);
        b = (byte)((255 - y) * (255 - k) / 255);
    }

    /// <summary>
    /// Converts a high-precision CMYK pixel to RGB using the standard multiplicative model, scaled
    /// to <paramref name="maxValue"/> (<c>(1 &lt;&lt; precision) - 1</c>). Inputs and outputs are
    /// native samples in <c>[0, maxValue]</c>.
    /// </summary>
    /// <param name="c">Cyan channel.</param>
    /// <param name="m">Magenta channel.</param>
    /// <param name="y">Yellow channel.</param>
    /// <param name="k">Black channel.</param>
    /// <param name="maxValue">The maximum sample value, e.g. 4095 for 12-bit.</param>
    /// <param name="r">Resulting red channel.</param>
    /// <param name="g">Resulting green channel.</param>
    /// <param name="b">Resulting blue channel.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CmykToRgb(int c, int m, int y, int k, int maxValue, out ushort r, out ushort g, out ushort b)
    {
        // 64-bit intermediates: for 16-bit samples (maxValue up to 65535) the product overflows int.
        long kk = maxValue - k;
        r = (ushort)((maxValue - c) * kk / maxValue);
        g = (ushort)((maxValue - m) * kk / maxValue);
        b = (ushort)((maxValue - y) * kk / maxValue);
    }

    /// <summary>Converts an RGB pixel to CMYK using maximum black generation.</summary>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    /// <param name="c">Resulting cyan channel.</param>
    /// <param name="m">Resulting magenta channel.</param>
    /// <param name="y">Resulting yellow channel.</param>
    /// <param name="k">Resulting black channel.</param>
    public static void RgbToCmyk(byte r, byte g, byte b, out byte c, out byte m, out byte y, out byte k)
    {
        var max = Math.Max(r, Math.Max(g, b));
        k = (byte)(255 - max);
        if (max == 0)
        {
            c = m = y = 0;
            return;
        }

        c = (byte)((max - r) * 255 / max);
        m = (byte)((max - g) * 255 / max);
        y = (byte)((max - b) * 255 / max);
    }

    /// <summary>Converts an Adobe YCCK pixel to CMYK.</summary>
    /// <param name="yc">Luma-like channel.</param>
    /// <param name="cb">Blue-difference chroma.</param>
    /// <param name="cr">Red-difference chroma.</param>
    /// <param name="k">Black channel (passed through).</param>
    /// <param name="c">Resulting cyan channel.</param>
    /// <param name="m">Resulting magenta channel.</param>
    /// <param name="y">Resulting yellow channel.</param>
    /// <param name="outK">The unchanged black channel.</param>
    public static void YcckToCmyk(byte yc, byte cb, byte cr, byte k, out byte c, out byte m, out byte y, out byte outK)
    {
        YCbCrToRgb(yc, cb, cr, out var r, out var g, out var b);
        c = (byte)(255 - r);
        m = (byte)(255 - g);
        y = (byte)(255 - b);
        outK = k;
    }

    /// <summary>Converts a CMYK pixel to Adobe YCCK.</summary>
    /// <param name="c">Cyan channel.</param>
    /// <param name="m">Magenta channel.</param>
    /// <param name="y">Yellow channel.</param>
    /// <param name="k">Black channel (passed through).</param>
    /// <param name="yc">Resulting luma-like channel.</param>
    /// <param name="cb">Resulting blue-difference chroma.</param>
    /// <param name="cr">Resulting red-difference chroma.</param>
    /// <param name="outK">The unchanged black channel.</param>
    public static void CmykToYcck(byte c, byte m, byte y, byte k, out byte yc, out byte cb, out byte cr, out byte outK)
    {
        var r = (byte)(255 - c);
        var g = (byte)(255 - m);
        var b = (byte)(255 - y);
        RgbToYCbCr(r, g, b, out yc, out cb, out cr);
        outK = k;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Clamp(int value)
    {
        if (value < 0)
            return 0;
        if (value > 255)
            return 255;
        return (byte)value;
    }
}

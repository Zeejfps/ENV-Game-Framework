using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8;

/// <summary>
/// The VP8 YCbCr→RGB color conversion, using the fixed-point BT.601 coefficients from libwebp.
/// Luma and chroma samples are 8-bit; outputs are clamped to the 8-bit range.
/// </summary>
internal static class Vp8Yuv
{
    // Fractional bits carried through the conversion before the final >> 6 clamp.
    private const int FixShift = 6;
    private const int ClipMask = ~((256 << FixShift) - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MultHi(int value, int coeff) => (value * coeff) >> 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Clip8(int value) =>
        (value & ClipMask) == 0 ? (byte)(value >> FixShift) : value < 0 ? (byte)0 : (byte)255;

    /// <summary>Converts a single YUV sample to RGB.</summary>
    /// <param name="y">The luma sample (0..255).</param>
    /// <param name="u">The blue-difference chroma sample (0..255).</param>
    /// <param name="v">The red-difference chroma sample (0..255).</param>
    /// <param name="r">Receives the red channel.</param>
    /// <param name="g">Receives the green channel.</param>
    /// <param name="b">Receives the blue channel.</param>
    public static void YuvToRgb(int y, int u, int v, out byte r, out byte g, out byte b)
    {
        var y1 = MultHi(y, 19077);
        r = Clip8(y1 + MultHi(v, 26149) - 14234);
        g = Clip8(y1 - MultHi(u, 6419) - MultHi(v, 13320) + 8708);
        b = Clip8(y1 + MultHi(u, 33050) - 17685);
    }
}

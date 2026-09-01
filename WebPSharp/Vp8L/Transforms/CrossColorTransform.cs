using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8L.Transforms;

/// <summary>
/// The VP8L cross-color (color decorrelation) transform. Per tile, a sub-sampled color image
/// supplies three signed multipliers — green→red, green→blue, and red→blue — used to subtract
/// predictable color correlation from the red and blue channels. Green is left unchanged. Unlike
/// the predictor transform this is a pure per-pixel remap with no neighbor dependency, so there
/// are no boundary special cases.
/// </summary>
internal static class CrossColorTransform
{
    private const uint AlphaGreenMask = 0xFF00FF00u;

    /// <summary>Reverses the transform in place (decoder side).</summary>
    /// <param name="argb">The transformed pixels, restored in place.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="colorImage">The sub-sampled color multiplier image.</param>
    /// <param name="bits">The tile size in bits.</param>
    public static void Inverse(uint[] argb, int width, int height, uint[] colorImage, int bits)
    {
        var tilesPerRow = Vp8LSubSample.Size(width, bits);
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * width;
            var tileRow = (y >> bits) * tilesPerRow;
            for (var x = 0; x < width; x++)
            {
                var code = colorImage[tileRow + (x >> bits)];
                var greenToRed = (sbyte)code;
                var greenToBlue = (sbyte)(code >> 8);
                var redToBlue = (sbyte)(code >> 16);

                var argbPixel = argb[rowStart + x];
                var green = (sbyte)(argbPixel >> 8);
                var newRed = (int)((argbPixel >> 16) & 0xFF);
                var newBlue = (int)(argbPixel & 0xFF);

                newRed = (newRed + Delta(greenToRed, green)) & 0xFF;
                newBlue = (newBlue + Delta(greenToBlue, green) + Delta(redToBlue, (sbyte)newRed)) & 0xFF;

                argb[rowStart + x] = (argbPixel & AlphaGreenMask) | ((uint)newRed << 16) | (uint)newBlue;
            }
        }
    }

    /// <summary>Applies the forward transform in place (encoder side).</summary>
    /// <param name="argb">The pixels, replaced with decorrelated pixels.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="colorImage">The sub-sampled color multiplier image.</param>
    /// <param name="bits">The tile size in bits.</param>
    public static void Forward(uint[] argb, int width, int height, uint[] colorImage, int bits)
    {
        var tilesPerRow = Vp8LSubSample.Size(width, bits);
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * width;
            var tileRow = (y >> bits) * tilesPerRow;
            for (var x = 0; x < width; x++)
            {
                var code = colorImage[tileRow + (x >> bits)];
                var greenToRed = (sbyte)code;
                var greenToBlue = (sbyte)(code >> 8);
                var redToBlue = (sbyte)(code >> 16);

                var argbPixel = argb[rowStart + x];
                var green = (sbyte)(argbPixel >> 8);
                var red = (sbyte)(argbPixel >> 16);
                var newRed = (int)((argbPixel >> 16) & 0xFF);
                var newBlue = (int)(argbPixel & 0xFF);

                newRed = (newRed - Delta(greenToRed, green)) & 0xFF;
                newBlue = (newBlue - Delta(greenToBlue, green) - Delta(redToBlue, red)) & 0xFF;

                argb[rowStart + x] = (argbPixel & AlphaGreenMask) | ((uint)newRed << 16) | (uint)newBlue;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Delta(sbyte predictor, sbyte color) => (predictor * color) >> 5;
}

using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8L.Transforms;

/// <summary>
/// The 14 VP8L spatial predictors and their supporting per-channel arithmetic. Each predictor
/// estimates a pixel from its already-reconstructed left (L), top (T), top-left (TL), and
/// top-right (TR) neighbors; the transform stores the residual between the true pixel and the
/// prediction. All operations are defined per 8-bit channel exactly as in the WebP lossless
/// specification.
/// </summary>
internal static class Vp8LPredictors
{
    private const uint OpaqueBlack = 0xFF000000u;

    /// <summary>Adds two pixels per channel, modulo 256 (decoder residual reconstruction).</summary>
    /// <param name="a">First pixel.</param>
    /// <param name="b">Second pixel.</param>
    /// <returns>The per-channel sum.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AddPixels(uint a, uint b)
    {
        var alpha = ((a >> 24) + (b >> 24)) & 0xFF;
        var red = (((a >> 16) & 0xFF) + ((b >> 16) & 0xFF)) & 0xFF;
        var green = (((a >> 8) & 0xFF) + ((b >> 8) & 0xFF)) & 0xFF;
        var blue = ((a & 0xFF) + (b & 0xFF)) & 0xFF;
        return (alpha << 24) | (red << 16) | (green << 8) | blue;
    }

    /// <summary>Subtracts two pixels per channel, modulo 256 (encoder residual computation).</summary>
    /// <param name="a">The minuend pixel.</param>
    /// <param name="b">The subtrahend pixel.</param>
    /// <returns>The per-channel difference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint SubtractPixels(uint a, uint b)
    {
        var alpha = ((a >> 24) - (b >> 24)) & 0xFF;
        var red = (((a >> 16) & 0xFF) - ((b >> 16) & 0xFF)) & 0xFF;
        var green = (((a >> 8) & 0xFF) - ((b >> 8) & 0xFF)) & 0xFF;
        var blue = ((a & 0xFF) - (b & 0xFF)) & 0xFF;
        return (alpha << 24) | (red << 16) | (green << 8) | blue;
    }

    /// <summary>Evaluates predictor <paramref name="mode"/> for the given neighbors.</summary>
    /// <param name="mode">The predictor mode, 0..13.</param>
    /// <param name="left">The left neighbor (L).</param>
    /// <param name="top">The top neighbor (T).</param>
    /// <param name="topLeft">The top-left neighbor (TL).</param>
    /// <param name="topRight">The top-right neighbor (TR).</param>
    /// <returns>The predicted pixel.</returns>
    public static uint Predict(int mode, uint left, uint top, uint topLeft, uint topRight) => mode switch
    {
        0 => OpaqueBlack,
        1 => left,
        2 => top,
        3 => topRight,
        4 => topLeft,
        5 => Average2(Average2(left, topRight), top),
        6 => Average2(left, topLeft),
        7 => Average2(left, top),
        8 => Average2(topLeft, top),
        9 => Average2(top, topRight),
        10 => Average2(Average2(left, topLeft), Average2(top, topRight)),
        11 => Select(top, left, topLeft),
        12 => ClampedAddSubtractFull(left, top, topLeft),
        13 => ClampedAddSubtractHalf(left, top, topLeft),
        _ => OpaqueBlack,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Average2(uint a, uint b) => (((a ^ b) & 0xFEFEFEFEu) >> 1) + (a & b);

    private static uint Select(uint top, uint left, uint topLeft)
    {
        var paMinusPb =
            Sub3((int)(top >> 24), (int)(left >> 24), (int)(topLeft >> 24)) +
            Sub3((int)((top >> 16) & 0xFF), (int)((left >> 16) & 0xFF), (int)((topLeft >> 16) & 0xFF)) +
            Sub3((int)((top >> 8) & 0xFF), (int)((left >> 8) & 0xFF), (int)((topLeft >> 8) & 0xFF)) +
            Sub3((int)(top & 0xFF), (int)(left & 0xFF), (int)(topLeft & 0xFF));
        return paMinusPb <= 0 ? top : left;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Sub3(int a, int b, int c) => Math.Abs(b - c) - Math.Abs(a - c);

    private static uint ClampedAddSubtractFull(uint c0, uint c1, uint c2)
    {
        var a = AddSubtractFull((int)(c0 >> 24), (int)(c1 >> 24), (int)(c2 >> 24));
        var r = AddSubtractFull((int)((c0 >> 16) & 0xFF), (int)((c1 >> 16) & 0xFF), (int)((c2 >> 16) & 0xFF));
        var g = AddSubtractFull((int)((c0 >> 8) & 0xFF), (int)((c1 >> 8) & 0xFF), (int)((c2 >> 8) & 0xFF));
        var b = AddSubtractFull((int)(c0 & 0xFF), (int)(c1 & 0xFF), (int)(c2 & 0xFF));
        return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
    }

    private static uint ClampedAddSubtractHalf(uint c0, uint c1, uint c2)
    {
        var ave = Average2(c0, c1);
        var a = AddSubtractHalf((int)(ave >> 24), (int)(c2 >> 24));
        var r = AddSubtractHalf((int)((ave >> 16) & 0xFF), (int)((c2 >> 16) & 0xFF));
        var g = AddSubtractHalf((int)((ave >> 8) & 0xFF), (int)((c2 >> 8) & 0xFF));
        var b = AddSubtractHalf((int)(ave & 0xFF), (int)(c2 & 0xFF));
        return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AddSubtractFull(int a, int b, int c) => Clip255(a + b - c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AddSubtractHalf(int a, int b) => Clip255(a + (a - b) / 2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Clip255(int value) => value < 0 ? 0 : value > 255 ? 255 : value;
}

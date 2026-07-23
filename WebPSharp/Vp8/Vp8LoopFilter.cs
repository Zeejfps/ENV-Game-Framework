using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8;

/// <summary>
/// The VP8 loop (deblocking) filters from RFC 6386. Each operates on one line of samples straddling
/// an edge — p3 p2 p1 p0 | q0 q1 q2 q3 — indexed from the q0 position with a given step (1 for a
/// vertical edge filtered horizontally, the row stride for a horizontal edge). Callers apply a
/// filter along all lines of an edge. Arithmetic is performed on signed (sample − 128) values.
/// </summary>
internal static class Vp8LoopFilter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Sclamp(int v) => v < -128 ? -128 : v > 127 ? 127 : v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToPixel(int signed) => (byte)(Sclamp(signed) + 128);

    private static int CommonAdjust(bool useOuterTaps, Span<byte> d, int q0, int step)
    {
        var p1 = d[q0 - 2 * step] - 128;
        var p0 = d[q0 - step] - 128;
        var q0s = d[q0] - 128;
        var q1 = d[q0 + step] - 128;

        var a = Sclamp((useOuterTaps ? Sclamp(p1 - q1) : 0) + 3 * (q0s - p0));
        var b = Sclamp(a + 3) >> 3;
        a = Sclamp(a + 4) >> 3;

        d[q0] = ToPixel(q0s - a);
        d[q0 - step] = ToPixel(p0 + b);
        return a;
    }

    private static bool NeedsFilter(Span<byte> d, int q0, int step, int interiorLimit, int edgeLimit)
    {
        int p3 = d[q0 - 4 * step], p2 = d[q0 - 3 * step], p1 = d[q0 - 2 * step], p0 = d[q0 - step];
        int cur = d[q0], q1 = d[q0 + step], q2 = d[q0 + 2 * step], q3 = d[q0 + 3 * step];
        return Math.Abs(p0 - cur) * 2 + Math.Abs(p1 - q1) / 2 <= edgeLimit
               && Math.Abs(p3 - p2) <= interiorLimit
               && Math.Abs(p2 - p1) <= interiorLimit
               && Math.Abs(p1 - p0) <= interiorLimit
               && Math.Abs(q3 - q2) <= interiorLimit
               && Math.Abs(q2 - q1) <= interiorLimit
               && Math.Abs(q1 - cur) <= interiorLimit;
    }

    private static bool HighEdgeVariance(Span<byte> d, int q0, int step, int threshold)
        => Math.Abs(d[q0 - 2 * step] - d[q0 - step]) > threshold
           || Math.Abs(d[q0 + step] - d[q0]) > threshold;

    /// <summary>Applies the simple loop filter to a single line (luma only, two taps per side).</summary>
    /// <param name="d">The sample buffer.</param>
    /// <param name="q0">The index of the q0 sample.</param>
    /// <param name="step">The sample step (1 across a row, stride down a column).</param>
    /// <param name="edgeLimit">The edge threshold.</param>
    public static void SimpleFilter(Span<byte> d, int q0, int step, int edgeLimit)
    {
        int p1 = d[q0 - 2 * step], p0 = d[q0 - step], cur = d[q0], q1 = d[q0 + step];
        if (Math.Abs(p0 - cur) * 2 + Math.Abs(p1 - q1) / 2 <= edgeLimit)
            CommonAdjust(true, d, q0, step);
    }

    /// <summary>Applies the normal interior (subblock-edge) loop filter to a single line.</summary>
    /// <param name="d">The sample buffer.</param>
    /// <param name="q0">The index of the q0 sample.</param>
    /// <param name="step">The sample step.</param>
    /// <param name="hevThreshold">The high-edge-variance threshold.</param>
    /// <param name="interiorLimit">The interior difference limit.</param>
    /// <param name="edgeLimit">The edge threshold.</param>
    public static void SubblockFilter(Span<byte> d, int q0, int step, int hevThreshold, int interiorLimit, int edgeLimit)
    {
        if (!NeedsFilter(d, q0, step, interiorLimit, edgeLimit))
            return;

        var p1 = d[q0 - 2 * step] - 128;
        var q1 = d[q0 + step] - 128;
        if (HighEdgeVariance(d, q0, step, hevThreshold))
        {
            CommonAdjust(true, d, q0, step);
        }
        else
        {
            var a = (CommonAdjust(false, d, q0, step) + 1) >> 1;
            d[q0 + step] = ToPixel(q1 - a);
            d[q0 - 2 * step] = ToPixel(p1 + a);
        }
    }

    /// <summary>Applies the normal macroblock-edge loop filter to a single line (three taps per side).</summary>
    /// <param name="d">The sample buffer.</param>
    /// <param name="q0">The index of the q0 sample.</param>
    /// <param name="step">The sample step.</param>
    /// <param name="hevThreshold">The high-edge-variance threshold.</param>
    /// <param name="interiorLimit">The interior difference limit.</param>
    /// <param name="edgeLimit">The edge threshold.</param>
    public static void MacroblockFilter(Span<byte> d, int q0, int step, int hevThreshold, int interiorLimit, int edgeLimit)
    {
        if (!NeedsFilter(d, q0, step, interiorLimit, edgeLimit))
            return;

        if (HighEdgeVariance(d, q0, step, hevThreshold))
        {
            CommonAdjust(true, d, q0, step);
            return;
        }

        var p2 = d[q0 - 3 * step] - 128;
        var p1 = d[q0 - 2 * step] - 128;
        var p0 = d[q0 - step] - 128;
        var q0s = d[q0] - 128;
        var q1 = d[q0 + step] - 128;
        var q2 = d[q0 + 2 * step] - 128;

        var w = Sclamp(Sclamp(p1 - q1) + 3 * (q0s - p0));

        var a = Sclamp((27 * w + 63) >> 7);
        d[q0] = ToPixel(q0s - a);
        d[q0 - step] = ToPixel(p0 + a);

        a = Sclamp((18 * w + 63) >> 7);
        d[q0 + step] = ToPixel(q1 - a);
        d[q0 - 2 * step] = ToPixel(p1 + a);

        a = Sclamp((9 * w + 63) >> 7);
        d[q0 + 2 * step] = ToPixel(q2 - a);
        d[q0 - 3 * step] = ToPixel(p2 + a);
    }
}

using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8;

/// <summary>
/// The VP8 in-loop deblocking filters, ported verbatim from the libwebp reference decoder
/// (<c>src/dsp/dec.c</c>) so decoded output is bit-exact with the reference. Operates directly on a
/// reconstructed plane buffer given a base index and a per-sample step (1 across a row for a vertical
/// edge, the row stride down a column for a horizontal edge).
/// </summary>
internal static class Vp8FilterApply
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Sclip1(int v) => v < -128 ? -128 : v > 127 ? 127 : v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Sclip2(int v) => v < -16 ? -16 : v > 15 ? 15 : v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Clip1(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int v) => v < 0 ? -v : v;

    private static void DoFilter2(byte[] p, int i, int step)
    {
        int p1 = p[i - 2 * step], p0 = p[i - step], q0 = p[i], q1 = p[i + step];
        var a = 3 * (q0 - p0) + Sclip1(p1 - q1);
        var a1 = Sclip2((a + 4) >> 3);
        var a2 = Sclip2((a + 3) >> 3);
        p[i - step] = Clip1(p0 + a2);
        p[i] = Clip1(q0 - a1);
    }

    private static void DoFilter4(byte[] p, int i, int step)
    {
        int p1 = p[i - 2 * step], p0 = p[i - step], q0 = p[i], q1 = p[i + step];
        var a = 3 * (q0 - p0);
        var a1 = Sclip2((a + 4) >> 3);
        var a2 = Sclip2((a + 3) >> 3);
        var a3 = (a1 + 1) >> 1;
        p[i - 2 * step] = Clip1(p1 + a3);
        p[i - step] = Clip1(p0 + a2);
        p[i] = Clip1(q0 - a1);
        p[i + step] = Clip1(q1 - a3);
    }

    private static void DoFilter6(byte[] p, int i, int step)
    {
        int p2 = p[i - 3 * step], p1 = p[i - 2 * step], p0 = p[i - step];
        int q0 = p[i], q1 = p[i + step], q2 = p[i + 2 * step];
        var a = Sclip1(3 * (q0 - p0) + Sclip1(p1 - q1));
        var a1 = (27 * a + 63) >> 7;
        var a2 = (18 * a + 63) >> 7;
        var a3 = (9 * a + 63) >> 7;
        p[i - 3 * step] = Clip1(p2 + a3);
        p[i - 2 * step] = Clip1(p1 + a2);
        p[i - step] = Clip1(p0 + a1);
        p[i] = Clip1(q0 - a1);
        p[i + step] = Clip1(q1 - a2);
        p[i + 2 * step] = Clip1(q2 - a3);
    }

    private static bool Hev(byte[] p, int i, int step, int thresh)
        => Abs(p[i - 2 * step] - p[i - step]) > thresh || Abs(p[i + step] - p[i]) > thresh;

    private static bool NeedsFilter(byte[] p, int i, int step, int t)
        => 4 * Abs(p[i - step] - p[i]) + Abs(p[i - 2 * step] - p[i + step]) <= t;

    private static bool NeedsFilter2(byte[] p, int i, int step, int t, int it)
    {
        int p3 = p[i - 4 * step], p2 = p[i - 3 * step], p1 = p[i - 2 * step], p0 = p[i - step];
        int q0 = p[i], q1 = p[i + step], q2 = p[i + 2 * step], q3 = p[i + 3 * step];
        if (4 * Abs(p0 - q0) + Abs(p1 - q1) > t)
            return false;
        return Abs(p3 - p2) <= it && Abs(p2 - p1) <= it && Abs(p1 - p0) <= it
               && Abs(q3 - q2) <= it && Abs(q2 - q1) <= it && Abs(q1 - q0) <= it;
    }

    // ---- Simple filter (luma only) ----

    private static void SimpleSegment(byte[] p, int start, int hstride, int vstride, int size, int thresh)
    {
        var thresh2 = 2 * thresh + 1;
        for (var i = 0; i < size; i++)
        {
            var pos = start + i * vstride;
            if (NeedsFilter(p, pos, hstride, thresh2))
                DoFilter2(p, pos, hstride);
        }
    }

    /// <summary>Simple filter on a macroblock edge (16 samples).</summary>
    public static void SimpleH16(byte[] y, int off, int stride, int thresh) => SimpleSegment(y, off, 1, stride, 16, thresh);

    /// <summary>Simple filter on a macroblock edge (16 samples).</summary>
    public static void SimpleV16(byte[] y, int off, int stride, int thresh) => SimpleSegment(y, off, stride, 1, 16, thresh);

    /// <summary>Simple filter on the three inner vertical edges.</summary>
    public static void SimpleH16i(byte[] y, int off, int stride, int thresh)
    {
        for (var k = 1; k <= 3; k++)
            SimpleSegment(y, off + 4 * k, 1, stride, 16, thresh);
    }

    /// <summary>Simple filter on the three inner horizontal edges.</summary>
    public static void SimpleV16i(byte[] y, int off, int stride, int thresh)
    {
        for (var k = 1; k <= 3; k++)
            SimpleSegment(y, off + 4 * k * stride, stride, 1, 16, thresh);
    }

    // ---- Complex (normal) filter ----

    private static void Loop26(byte[] p, int start, int hstride, int vstride, int size, int thresh, int ithresh, int hev)
    {
        var thresh2 = 2 * thresh + 1;
        for (var s = 0; s < size; s++)
        {
            var pos = start + s * vstride;
            if (NeedsFilter2(p, pos, hstride, thresh2, ithresh))
            {
                if (Hev(p, pos, hstride, hev)) DoFilter2(p, pos, hstride);
                else DoFilter6(p, pos, hstride);
            }
        }
    }

    private static void Loop24(byte[] p, int start, int hstride, int vstride, int size, int thresh, int ithresh, int hev)
    {
        var thresh2 = 2 * thresh + 1;
        for (var s = 0; s < size; s++)
        {
            var pos = start + s * vstride;
            if (NeedsFilter2(p, pos, hstride, thresh2, ithresh))
            {
                if (Hev(p, pos, hstride, hev)) DoFilter2(p, pos, hstride);
                else DoFilter4(p, pos, hstride);
            }
        }
    }

    /// <summary>Normal filter on the left (vertical) luma macroblock edge.</summary>
    public static void H16(byte[] y, int off, int stride, int t, int it, int hev) => Loop26(y, off, 1, stride, 16, t, it, hev);

    /// <summary>Normal filter on the top (horizontal) luma macroblock edge.</summary>
    public static void V16(byte[] y, int off, int stride, int t, int it, int hev) => Loop26(y, off, stride, 1, 16, t, it, hev);

    /// <summary>Normal filter on the three inner vertical luma edges.</summary>
    public static void H16i(byte[] y, int off, int stride, int t, int it, int hev)
    {
        for (var k = 1; k <= 3; k++)
            Loop24(y, off + 4 * k, 1, stride, 16, t, it, hev);
    }

    /// <summary>Normal filter on the three inner horizontal luma edges.</summary>
    public static void V16i(byte[] y, int off, int stride, int t, int it, int hev)
    {
        for (var k = 1; k <= 3; k++)
            Loop24(y, off + 4 * k * stride, stride, 1, 16, t, it, hev);
    }

    /// <summary>Normal filter on the left (vertical) chroma macroblock edges (U and V).</summary>
    public static void H8(byte[] u, byte[] v, int off, int stride, int t, int it, int hev)
    {
        Loop26(u, off, 1, stride, 8, t, it, hev);
        Loop26(v, off, 1, stride, 8, t, it, hev);
    }

    /// <summary>Normal filter on the top (horizontal) chroma macroblock edges (U and V).</summary>
    public static void V8(byte[] u, byte[] v, int off, int stride, int t, int it, int hev)
    {
        Loop26(u, off, stride, 1, 8, t, it, hev);
        Loop26(v, off, stride, 1, 8, t, it, hev);
    }

    /// <summary>Normal filter on the single inner vertical chroma edge (U and V).</summary>
    public static void H8i(byte[] u, byte[] v, int off, int stride, int t, int it, int hev)
    {
        Loop24(u, off + 4, 1, stride, 8, t, it, hev);
        Loop24(v, off + 4, 1, stride, 8, t, it, hev);
    }

    /// <summary>Normal filter on the single inner horizontal chroma edge (U and V).</summary>
    public static void V8i(byte[] u, byte[] v, int off, int stride, int t, int it, int hev)
    {
        Loop24(u, off + 4 * stride, stride, 1, 8, t, it, hev);
        Loop24(v, off + 4 * stride, stride, 1, 8, t, it, hev);
    }
}

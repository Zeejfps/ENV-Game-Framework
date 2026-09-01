using WebPSharp.Vp8;

namespace WebPSharp.Tests;

// Direct vectors for the SHIPPED decode-path loop filter (Vp8FilterApply) — the kernels the decoder
// actually calls (DoFilter2/4/6 via SimpleH16/SimpleV16/H16/H16i/H8). These had no direct coverage;
// Vp8LoopFilterTests exercises only the dead RFC-variant Vp8LoopFilter. Every expected value is
// hand-computed from the libwebp dec.c integer formulas:
//   a  = 3*(q0-p0) [+ Sclip1(p1-q1) for DoFilter2/6]
//   Sclip1: clamp[-128,127]   Sclip2: clamp[-16,15]   Clip1: clamp[0,255]   >>: arithmetic
// The reference regions below are two uniform half-planes split at one edge, so all interior masks
// are trivially satisfied and only the modeled edge moves.
public class Vp8FilterApplyTests
{
    // ---- Simple filter (DoFilter2) via SimpleH16 (horizontal step, hstride = 1) ----

    [Fact]
    public void SimpleH16_SharpEdge_NeedsFilterTrue_AdjustsP0Q0Only()
    {
        // Per row: p3 p2 p1 p0 | q0 q1 q2 q3 = 190 195 200 200 | 100 100 105 110, edge (q0) at col 4.
        const int stride = 8;
        var y = new byte[16 * stride];
        byte[] row = { 190, 195, 200, 200, 100, 100, 105, 110 };
        for (var r = 0; r < 16; r++)
            for (var c = 0; c < 8; c++) y[r * stride + c] = row[c];

        // NeedsFilter: 4*|200-100| + |200-100| = 500 <= 2*255+1. a = 3*(100-200)+Sclip1(100)= -200.
        // a1 = Sclip2(-196>>3 = -25) = -16 ; a2 = Sclip2(-197>>3 = -25) = -16.
        // p0 = 200 + (-16) = 184 ; q0 = 100 - (-16) = 116.
        Vp8FilterApply.SimpleH16(y, off: 4, stride, thresh: 255);

        Assert.Equal(184, y[3]);           // p0
        Assert.Equal(116, y[4]);           // q0
        Assert.Equal(200, y[2]);           // p1 untouched by simple filter
        Assert.Equal(100, y[5]);           // q1 untouched
        Assert.Equal(195, y[1]);           // p2 untouched
        Assert.Equal(105, y[6]);           // q2 untouched
        // Every row filtered identically.
        Assert.Equal(184, y[7 * stride + 3]);
        Assert.Equal(116, y[7 * stride + 4]);
    }

    [Fact]
    public void SimpleH16_ThresholdTooSmall_NeedsFilterFalse_NoChange()
    {
        const int stride = 8;
        var y = new byte[16 * stride];
        byte[] row = { 190, 195, 200, 200, 100, 100, 105, 110 };
        for (var r = 0; r < 16; r++)
            for (var c = 0; c < 8; c++) y[r * stride + c] = row[c];
        var before = (byte[])y.Clone();

        // NeedsFilter: 500 > 2*10+1 = 21 -> mask fails -> untouched.
        Vp8FilterApply.SimpleH16(y, off: 4, stride, thresh: 10);
        Assert.Equal(before, y);
    }

    [Fact]
    public void SimpleV16_VerticalEdge_FiltersDownColumnAcrossStride()
    {
        // Same edge arranged vertically: rows p3..q3 down each of 16 columns, step = stride.
        const int stride = 16;
        var y = new byte[8 * stride];
        byte[] col = { 190, 195, 200, 200, 100, 100, 105, 110 };
        for (var r = 0; r < 8; r++)
            for (var c = 0; c < stride; c++) y[r * stride + c] = col[r];

        Vp8FilterApply.SimpleV16(y, off: 4 * stride, stride, thresh: 255);
        Assert.Equal(184, y[3 * stride + 0]);   // p0 (row 3, col 0)
        Assert.Equal(116, y[4 * stride + 0]);   // q0 (row 4, col 0)
        Assert.Equal(184, y[3 * stride + 15]);  // filtered on every column
        Assert.Equal(116, y[4 * stride + 15]);
    }

    // ---- Normal filter, wide branch (DoFilter6) via H16 macroblock edge ----

    [Fact]
    public void H16_SmoothStep_NoHev_RunsDoFilter6_AdjustsThreeEachSide()
    {
        // Half-planes 100 | 110 split at col 4 (the macroblock left edge = off).
        const int stride = 16;
        var y = new byte[16 * stride];
        for (var r = 0; r < 16; r++)
            for (var c = 0; c < stride; c++) y[r * stride + c] = (byte)(c < 4 ? 100 : 110);

        // a = Sclip1(3*10 + Sclip1(100-110)) = Sclip1(30-10) = 20.
        // a1 = (27*20+63)>>7 = 4 ; a2 = (18*20+63)>>7 = 3 ; a3 = (9*20+63)>>7 = 1.
        // p2,p1,p0 = 100 + {1,3,4} = 101,103,104 ; q0,q1,q2 = 110 - {4,3,1} = 106,107,109.
        Vp8FilterApply.H16(y, off: 4, stride, t: 30, it: 30, hev: 100);

        Assert.Equal(100, y[0]);   // p3 never touched
        Assert.Equal(101, y[1]);   // p2
        Assert.Equal(103, y[2]);   // p1
        Assert.Equal(104, y[3]);   // p0
        Assert.Equal(106, y[4]);   // q0
        Assert.Equal(107, y[5]);   // q1
        Assert.Equal(109, y[6]);   // q2
        Assert.Equal(110, y[7]);   // q3 never touched
    }

    [Fact]
    public void H16_ThresholdTooSmall_NeedsFilter2False_NoChange()
    {
        const int stride = 16;
        var y = new byte[16 * stride];
        for (var r = 0; r < 16; r++)
            for (var c = 0; c < stride; c++) y[r * stride + c] = (byte)(c < 4 ? 100 : 110);
        var before = (byte[])y.Clone();

        // 4*|100-110| + |100-110| = 50 > 2*5+1 = 11 -> NeedsFilter2 false -> untouched.
        Vp8FilterApply.H16(y, off: 4, stride, t: 5, it: 30, hev: 100);
        Assert.Equal(before, y);
    }

    // ---- Normal filter, HEV branch (DoFilter2) via H16 macroblock edge ----

    [Fact]
    public void H16_HighEdgeVariance_HevTrue_RunsDoFilter2()
    {
        // Per row: p3 p2 p1 p0 | q0 q1 q2 q3 = 80 90 100 150 | 160 170 175 180, edge at col 4.
        const int stride = 8;
        var y = new byte[16 * stride];
        byte[] row = { 80, 90, 100, 150, 160, 170, 175, 180 };
        for (var r = 0; r < 16; r++)
            for (var c = 0; c < 8; c++) y[r * stride + c] = row[c];

        // NeedsFilter2: 4*10 + |100-170| = 110 <= 2*60+1 ; interior max step |100-150|=50 <= it 60.
        // Hev: |p1-p0| = 50 > 20 -> HEV true -> DoFilter2 (p0,q0 only).
        // a = 3*(160-150)+Sclip1(100-170) = 30-70 = -40 ; a1 = Sclip2(-36>>3=-5) = -5 ; a2 = -5.
        // p0 = 150 + (-5) = 145 ; q0 = 160 - (-5) = 165.
        Vp8FilterApply.H16(y, off: 4, stride, t: 60, it: 60, hev: 20);

        Assert.Equal(145, y[3]);   // p0
        Assert.Equal(165, y[4]);   // q0
        Assert.Equal(100, y[2]);   // p1 untouched (DoFilter2, not DoFilter6)
        Assert.Equal(170, y[5]);   // q1 untouched
        Assert.Equal(90, y[1]);    // p2 untouched
        Assert.Equal(175, y[6]);   // q2 untouched
    }

    // ---- Normal filter, inner-edge narrow branch (DoFilter4) via H16i ----

    [Fact]
    public void H16i_SmoothStep_NoHev_RunsDoFilter4_AdjustsTwoEachSide()
    {
        // Half-planes 100 | 110 split at col 4. H16i filters inner edges at cols 4,8,12; only col 4
        // is a real step (cols 8,12 sit in the uniform 110 region -> a = 0 -> no change).
        const int stride = 16;
        var y = new byte[16 * stride];
        for (var r = 0; r < 16; r++)
            for (var c = 0; c < stride; c++) y[r * stride + c] = (byte)(c < 4 ? 100 : 110);

        // a = 3*(110-100) = 30 ; a1 = Sclip2(34>>3=4)=4 ; a2 = Sclip2(33>>3=4)=4 ; a3 = (4+1)>>1 = 2.
        // p1,p0 = 100 + {2,4} = 102,104 ; q0,q1 = 110 - {4,2} = 106,108.
        Vp8FilterApply.H16i(y, off: 0, stride, t: 30, it: 30, hev: 100);

        Assert.Equal(100, y[0]);   // p3 never touched by DoFilter4
        Assert.Equal(100, y[1]);   // p2 never touched
        Assert.Equal(102, y[2]);   // p1
        Assert.Equal(104, y[3]);   // p0
        Assert.Equal(106, y[4]);   // q0
        Assert.Equal(108, y[5]);   // q1
        Assert.Equal(110, y[6]);   // q2 unchanged (inner edges at 8,12 are no-ops)
        Assert.Equal(110, y[7]);
    }

    // ---- Chroma normal filter (DoFilter6) via H8 filters both U and V ----

    [Fact]
    public void H8_FiltersBothChromaPlanes()
    {
        const int stride = 8;
        var u = new byte[8 * stride];
        var v = new byte[8 * stride];
        for (var r = 0; r < 8; r++)
            for (var c = 0; c < 8; c++)
            {
                u[r * stride + c] = (byte)(c < 4 ? 100 : 110);
                v[r * stride + c] = (byte)(c < 4 ? 100 : 110);
            }

        Vp8FilterApply.H8(u, v, off: 4, stride, t: 30, it: 30, hev: 100);

        // Same DoFilter6 result as H16 (101,103,104 | 106,107,109), applied to BOTH planes.
        byte[] expectedEdge = { 100, 101, 103, 104, 106, 107, 109, 110 };
        for (var c = 0; c < 8; c++)
        {
            Assert.Equal(expectedEdge[c], u[c]);
            Assert.Equal(expectedEdge[c], v[c]);
        }
    }
}

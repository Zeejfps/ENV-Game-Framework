using WebPSharp.Vp8;

namespace WebPSharp.Tests;

// Non-uniform B_PRED vectors for the four "diagonal" 4x4 modes (VR4/VL4/HD4/HU4), plus the
// top-right-edge replication contract and the libwebp->RFC BModeToPred remap. Uniform-input tests
// (Vp8Prediction4Tests) can't distinguish sample placement; every expected block below is
// hand-computed from the RFC 6386 Avg2/Avg3 placement with distinct neighbor samples.
//
// Shared neighbor set (all distinct so misplacement is observable):
//   top   = A..H = { 10, 20, 30, 40, 50, 60, 70, 80 }
//   left  = I..L = { 90, 100, 110, 120 }
//   corner X = 130
//   Avg2(a,b) = (a+b+1)>>1 ; Avg3(a,b,c) = (a+2b+c+2)>>2
public class Vp8Prediction4NonUniformTests
{
    private static readonly byte[] Top = { 10, 20, 30, 40, 50, 60, 70, 80 };
    private static readonly byte[] Left = { 90, 100, 110, 120 };
    private const byte Corner = 130;

    private static byte[] Predict(int mode, byte[] top, byte[] left, byte corner)
    {
        var dst = new byte[16];
        Vp8Prediction4.Predict(mode, dst, 4, top, left, corner);
        return dst;
    }

    [Fact]
    public void VerticalRight_NonUniform_MatchesHandComputedPlacement()
    {
        // row-major (y*4+x), see class comment for the per-cell Avg2/Avg3 sources.
        byte[] expected =
        {
            70, 15, 25, 35,
            90, 43, 20, 30,
            103, 70, 15, 25,
            100, 90, 43, 20,
        };
        Assert.Equal(expected, Predict(Vp8Prediction4.VerticalRight, Top, Left, Corner));
    }

    [Fact]
    public void VerticalLeft_NonUniform_MatchesHandComputedPlacement()
    {
        byte[] expected =
        {
            15, 25, 35, 45,
            20, 30, 40, 50,
            25, 35, 45, 60,
            30, 40, 50, 70,
        };
        Assert.Equal(expected, Predict(Vp8Prediction4.VerticalLeft, Top, Left, Corner));
    }

    [Fact]
    public void HorizontalDown_NonUniform_MatchesHandComputedPlacement()
    {
        byte[] expected =
        {
            110, 90, 43, 20,
            95, 103, 110, 90,
            105, 100, 95, 103,
            115, 110, 105, 100,
        };
        Assert.Equal(expected, Predict(Vp8Prediction4.HorizontalDown, Top, Left, Corner));
    }

    [Fact]
    public void HorizontalUp_NonUniform_MatchesHandComputedPlacement()
    {
        byte[] expected =
        {
            95, 100, 105, 110,
            105, 110, 115, 118,
            115, 118, 120, 120,
            120, 120, 120, 120,
        };
        Assert.Equal(expected, Predict(Vp8Prediction4.HorizontalUp, Top, Left, Corner));
    }

    // Top-right-edge replication: libwebp, at the right edge, replicates the last valid top sample
    // (D) into the above-right neighbors E..H. DownLeft (LD4) consumes those samples, so the two
    // predictions must differ and the replicated one must match the D-fed hand computation.
    [Fact]
    public void DownLeft_ConsumesTopRightSamples()
    {
        // Distinct E..H = 50,60,70,80.
        byte[] distinct =
        {
            20, 30, 40, 50,
            30, 40, 50, 60,
            40, 50, 60, 70,
            50, 60, 70, 78,
        };
        Assert.Equal(distinct, Predict(Vp8Prediction4.DownLeft, Top, Left, Corner));
    }

    [Fact]
    public void DownLeft_ReplicatedTopRight_MatchesLastValidTopSample()
    {
        // Right-edge replication: E=F=G=H=D=40.
        var top = new byte[] { 10, 20, 30, 40, 40, 40, 40, 40 };
        byte[] expected =
        {
            20, 30, 38, 40,
            30, 38, 40, 40,
            38, 40, 40, 40,
            40, 40, 40, 40,
        };
        var replicated = Predict(Vp8Prediction4.DownLeft, top, Left, Corner);
        Assert.Equal(expected, replicated);
        // The above-right samples genuinely flow into the block: replicated != distinct.
        Assert.NotEqual(Predict(Vp8Prediction4.DownLeft, Top, Left, Corner), replicated);
    }

    // BModeToPred remaps the libwebp 4x4 mode enumeration (DC,TM,VE,HE,RD,VR,LD,VL,HD,HU) onto the
    // RFC-ordered Vp8Prediction4 constants. Entries 4/5/6 are the non-identity remaps.
    [Fact]
    public void BModeToPred_MapsLibwebpEnumToRfcOrder()
    {
        Assert.Equal(Vp8Prediction4.Dc, Vp8Decoder.BModeToPred[0]);
        Assert.Equal(Vp8Prediction4.TrueMotion, Vp8Decoder.BModeToPred[1]);
        Assert.Equal(Vp8Prediction4.Vertical, Vp8Decoder.BModeToPred[2]);
        Assert.Equal(Vp8Prediction4.Horizontal, Vp8Decoder.BModeToPred[3]);
        // Non-identity remaps: libwebp RD/VR/LD -> RFC DownRight(5)/VerticalRight(6)/DownLeft(4).
        Assert.Equal(Vp8Prediction4.DownRight, Vp8Decoder.BModeToPred[4]);
        Assert.Equal(Vp8Prediction4.VerticalRight, Vp8Decoder.BModeToPred[5]);
        Assert.Equal(Vp8Prediction4.DownLeft, Vp8Decoder.BModeToPred[6]);
        Assert.Equal(Vp8Prediction4.VerticalLeft, Vp8Decoder.BModeToPred[7]);
        Assert.Equal(Vp8Prediction4.HorizontalDown, Vp8Decoder.BModeToPred[8]);
        Assert.Equal(Vp8Prediction4.HorizontalUp, Vp8Decoder.BModeToPred[9]);
    }
}

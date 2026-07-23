using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8DequantTests
{
    // Builds a minimal, fully-valid VP8 key-frame payload whose only interesting content is a chosen
    // base quantizer index with all five dequant deltas absent and segmentation disabled. Parsing its
    // headers runs the exact production dequant-factor derivation, so the resulting Dequant[0] lets us
    // pin the factor formulas against hand-computed spec values.
    private static byte[] BuildMinimalVp8(int baseQ)
    {
        var enc = new Vp8BooleanEncoder();
        enc.PutBit(128, 0);          // color space
        enc.PutBit(128, 0);          // clamping type
        enc.PutBit(128, 0);          // segmentation disabled
        enc.PutBit(128, 0);          // filter: simple flag
        enc.PutLiteral(0, 6);        // filter level
        enc.PutLiteral(0, 3);        // filter sharpness
        enc.PutBit(128, 0);          // loop-filter delta disabled
        enc.PutLiteral(0, 2);        // log2(num partitions) = 0 -> 1 partition
        enc.PutLiteral((uint)baseQ, 7); // base quantizer index
        for (var i = 0; i < 5; i++)
            enc.PutBit(128, 0);      // dqy1_dc, dqy2_dc, dqy2_ac, dquv_dc, dquv_ac all absent
        enc.PutBit(128, 0);          // update_proba flag
        var upd = Vp8Tables.CoeffUpdateProbs;
        for (var i = 0; i < upd.Length; i++)
            enc.PutBit(upd[i], 0);   // no coefficient-probability updates (keep defaults)
        enc.PutBit(128, 0);          // use-skip-proba flag
        var first = enc.Finish();

        var payload = new byte[10 + first.Length];
        var tag = (first.Length << 5) | (1 << 4); // key frame, profile 0, show=1
        payload[0] = (byte)(tag & 0xFF);
        payload[1] = (byte)((tag >> 8) & 0xFF);
        payload[2] = (byte)((tag >> 16) & 0xFF);
        payload[3] = 0x9D;
        payload[4] = 0x01;
        payload[5] = 0x2A;
        payload[6] = 16; payload[7] = 0; // width 16
        payload[8] = 16; payload[9] = 0; // height 16
        Array.Copy(first, 0, payload, 10, first.Length);
        return payload;
    }

    private static Vp8QuantMatrix DequantForBaseQ(int baseQ)
    {
        var d = new Vp8Decoder(BuildMinimalVp8(baseQ));
        d.ParseHeaders();
        return d.Dequant[0];
    }

    // baseQ = 0: DcTable[0]=4, AcTable[0]=4.
    //   Y2Dc = 4*2 = 8
    //   Y2Ac = max(8, (4*101581)>>16) = max(8, 6) = 8   (the max(8,..) floor is exercised here)
    //   UvDc = DcTable[min(0,117)] = 4
    [Fact]
    public void Dequant_BaseQ0_MatchesHandComputedFactors()
    {
        var m = DequantForBaseQ(0);
        Assert.Equal(4, m.Y1Dc);
        Assert.Equal(4, m.Y1Ac);
        Assert.Equal(8, m.Y2Dc);
        Assert.Equal(8, m.Y2Ac);
        Assert.Equal(4, m.UvDc);
        Assert.Equal(4, m.UvAc);
    }

    // baseQ = 120 (> 117, so the UV-DC clamp bites): DcTable[120]=138, AcTable[120]=249, DcTable[117]=132.
    //   Y2Dc = 138*2 = 276
    //   Y2Ac = max(8, (249*101581)>>16) = max(8, 385) = 385  (pins the 101581 fixed-point multiplier)
    //   UvDc = DcTable[min(120,117)] = DcTable[117] = 132     (pins the 117 clamp)
    //   UvAc = AcTable[120] = 249                             (AC uses the 127 clamp, unaffected here)
    [Fact]
    public void Dequant_BaseQ120_PinsY2AcMultiplierAndUvDcClamp()
    {
        var m = DequantForBaseQ(120);
        Assert.Equal(138, m.Y1Dc);
        Assert.Equal(249, m.Y1Ac);
        Assert.Equal(276, m.Y2Dc);
        Assert.Equal(385, m.Y2Ac);
        Assert.Equal(132, m.UvDc);
        Assert.Equal(249, m.UvAc);
    }

    // The UV-DC clamp is to index 117, distinct from the 127 clamp used by every other factor. At
    // baseQ = 127 the UV-DC factor must still be DcTable[117]=132 while the luma DC factor is
    // DcTable[127]=157, proving the two clamps differ.
    [Fact]
    public void Dequant_UvDcClamp_IsSeparateFrom127Clamp()
    {
        var m = DequantForBaseQ(127);
        Assert.Equal(157, m.Y1Dc);                 // DcTable[127]
        Assert.Equal(132, m.UvDc);                 // DcTable[117], not DcTable[127]
        Assert.Equal(Vp8Tables.DcTable[117], m.UvDc);
    }
}

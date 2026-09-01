using WebPSharp.Vp8;

namespace WebPSharp.Tests;

// Explicit non-tautological YUV->RGB golden vectors. Existing Vp8YuvTests covers grayscale/white/
// black plus a reference-formula fuzz; these pin specific colored samples and the clamp corners,
// hand-computed from the exact libwebp fixed-point constants:
//   y1 = (Y*19077)>>8
//   R = clip8(y1 + (V*26149>>8) - 14234)
//   G = clip8(y1 - (U*6419>>8) - (V*13320>>8) + 8708)
//   B = clip8(y1 + (U*33050>>8) - 17685)
//   clip8(x): (x>>6) if 0 <= x>>6 <= 255 else clamp to [0,255]
public class Vp8YuvVectorTests
{
    [Fact]
    public void ColoredSample_128_64_192()
    {
        // y1 = (128*19077)>>8 = 9538.
        // R = (9538 + (192*26149>>8=19611) - 14234) = 14915 ; 14915>>6 = 233.
        // G = (9538 - (64*6419>>8=1604) - (192*13320>>8=9990) + 8708) = 6652 ; 6652>>6 = 103.
        // B = (9538 + (64*33050>>8=8262) - 17685) = 115 ; 115>>6 = 1.
        Vp8Yuv.YuvToRgb(128, 64, 192, out var r, out var g, out var b);
        Assert.Equal(233, r);
        Assert.Equal(103, g);
        Assert.Equal(1, b);
    }

    [Fact]
    public void ColoredSample_150_255_50_BlueClampsHigh()
    {
        // Hand-computed: R=32, G=170, B saturates (B pre-shift >> 6 exceeds 255) -> 255.
        Vp8Yuv.YuvToRgb(150, 255, 50, out var r, out var g, out var b);
        Assert.Equal(32, r);
        Assert.Equal(170, g);
        Assert.Equal(255, b);
    }

    [Fact]
    public void ClampCorners_RedHigh_GreenLow()
    {
        // (76,84,255): R = 5663+26046-14234 = 17475 -> 17475>>6=273 -> clamps to 255.
        //              G = 5663-2106-13268+8708 = -1003 -> negative -> clamps to 0.
        Vp8Yuv.YuvToRgb(76, 84, 255, out var r, out var g, out var b);
        Assert.Equal(255, r);   // clamp-to-255
        Assert.Equal(0, g);     // clamp-to-0
    }

    [Fact]
    public void DarkLuma_NeutralChroma_ClampsRedAndBlueToZero()
    {
        // (16,128,128): y1=(16*19077)>>8=1192.
        // R = 1192 + (128*26149>>8=13074) - 14234 = 32 -> 32>>6 = 0.
        // B = 1192 + (128*33050>>8=16525) - 17685 = 32 -> 32>>6 = 0.
        Vp8Yuv.YuvToRgb(16, 128, 128, out var r, out var g, out var b);
        Assert.Equal(0, r);
        Assert.Equal(0, g);
        Assert.Equal(0, b);
    }
}

using WebPSharp.Vp8L.Transforms;

namespace WebPSharp.Tests;

public class PredictorTests
{
    [Fact]
    public void AddPixels_ThenSubtract_IsIdentity()
    {
        var rng = new Random(3);
        for (var i = 0; i < 10000; i++)
        {
            var a = (uint)rng.NextInt64(0, 1L << 32);
            var b = (uint)rng.NextInt64(0, 1L << 32);
            var sum = Vp8LPredictors.AddPixels(a, b);
            Assert.Equal(a, Vp8LPredictors.SubtractPixels(sum, b));
        }
    }

    [Fact]
    public void AddPixels_IsPerChannelModulo256()
    {
        // A: 0x01_02_03_04, B: 0xFF_FF_FF_FF -> each channel wraps by -1.
        var sum = Vp8LPredictors.AddPixels(0x01020304u, 0xFFFFFFFFu);
        Assert.Equal(0x00010203u, sum);
    }

    [Fact]
    public void Predictor0_IsOpaqueBlack()
    {
        Assert.Equal(0xFF000000u, Vp8LPredictors.Predict(0, 0x11223344, 0x55667788, 0x99AABBCC, 0xDDEEFF00));
    }

    [Fact]
    public void Predictor1_IsLeft()
    {
        Assert.Equal(0x11223344u, Vp8LPredictors.Predict(1, 0x11223344, 0x55667788, 0x99AABBCC, 0xDDEEFF00));
    }

    [Fact]
    public void Predictor2_IsTop()
    {
        Assert.Equal(0x55667788u, Vp8LPredictors.Predict(2, 0x11223344, 0x55667788, 0x99AABBCC, 0xDDEEFF00));
    }

    [Fact]
    public void Predictor3_IsTopRight()
    {
        Assert.Equal(0xDDEEFF00u, Vp8LPredictors.Predict(3, 0x11223344, 0x55667788, 0x99AABBCC, 0xDDEEFF00));
    }

    [Fact]
    public void Predictor4_IsTopLeft()
    {
        Assert.Equal(0x99AABBCCu, Vp8LPredictors.Predict(4, 0x11223344, 0x55667788, 0x99AABBCC, 0xDDEEFF00));
    }

    [Fact]
    public void Predictor7_AveragesLeftAndTop()
    {
        // Average2 per channel = floor((a+b)/2).
        var result = Vp8LPredictors.Predict(7, 0x02040608, 0x04040404, 0, 0);
        Assert.Equal(0x03040506u, result);
    }

    [Fact]
    public void Predictor5_Average3_LeftTopRightTop_Vector()
    {
        // mode 5 = Average2(Average2(left, topRight), top), per channel floor((a+b)/2).
        // left=FF102030, topRight=FF304050, top=FF000000, topLeft irrelevant.
        // inner=Avg(left,topRight)=FF203040 ; result=Avg(inner,top)=FF101820.
        var result = Vp8LPredictors.Predict(5, 0xFF102030u, 0xFF000000u, 0x00000000u, 0xFF304050u);
        Assert.Equal(0xFF101820u, result);
    }

    [Fact]
    public void Predictor10_Average3_Vector()
    {
        // mode 10 = Average2(Average2(left, topLeft), Average2(top, topRight)).
        // left=FF102030, topLeft=FF306050, top=FF004080, topRight=FF200000.
        // Avg(left,topLeft)=FF204040 ; Avg(top,topRight)=FF102040 ; Avg of those=FF183040.
        var result = Vp8LPredictors.Predict(10, 0xFF102030u, 0xFF004080u, 0xFF306050u, 0xFF200000u);
        Assert.Equal(0xFF183040u, result);
    }

    [Fact]
    public void Predictor11_Select_PicksLeft_Vector()
    {
        // Select(top, left, topLeft): paMinusPb = sum(|left-tl| - |top-tl|) per channel.
        // left=FF206080, top=FF285888, topLeft=FF1040A0 -> paMinusPb = 0-8+8+8 = 8 > 0 -> left.
        var result = Vp8LPredictors.Predict(11, 0xFF206080u, 0xFF285888u, 0xFF1040A0u, 0x00000000u);
        Assert.Equal(0xFF206080u, result);
    }

    [Fact]
    public void Predictor11_Select_PicksTop_Vector()
    {
        // left=FF808080 (near topLeft), top=FF000000 (far), topLeft=FF828282.
        // paMinusPb per non-alpha channel = |128-130| - |0-130| = 2-130 = -128, sum=-384 <= 0 -> top.
        var result = Vp8LPredictors.Predict(11, 0xFF808080u, 0xFF000000u, 0xFF828282u, 0x00000000u);
        Assert.Equal(0xFF000000u, result);
    }

    [Fact]
    public void Predictor12_ClampedAddSubtractFull_Vector()
    {
        // Per channel Clip255(left + top - topLeft).
        // left=FFC81040, top=FF640850, topLeft=FF0A2030.
        // A:255+255-255=255 ; R:200+100-10=290->255 ; G:16+8-32=-8->0 ; B:64+80-48=96.
        var result = Vp8LPredictors.Predict(12, 0xFFC81040u, 0xFF640850u, 0xFF0A2030u, 0x00000000u);
        Assert.Equal(0xFFFF0060u, result);
    }

    [Fact]
    public void Predictor13_ClampedAddSubtractHalf_Vector()
    {
        // ave = Average2(left, top) per channel; result = Clip255(ave + (ave - topLeft)/2).
        // left=FF8010C0, top=FF4020C0, topLeft=FF006010.
        // ave: A=255 R=96 G=24 B=192.
        // A:255+(255-255)/2=255 ; R:96+(96-0)/2=144 ; G:24+(24-96)/2=24-36=-12->0 ; B:192+(192-16)/2=280->255.
        var result = Vp8LPredictors.Predict(13, 0xFF8010C0u, 0xFF4020C0u, 0xFF006010u, 0x00000000u);
        Assert.Equal(0xFF9000FFu, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public void PredictorTransform_ForwardInverse_IsIdentity(int mode)
    {
        const int width = 19, height = 13, bits = 3;
        var rng = new Random(1000 + mode);
        var argb = new uint[width * height];
        for (var i = 0; i < argb.Length; i++)
            argb[i] = (uint)rng.NextInt64(0, 1L << 32);

        var subW = Vp8LSubSample.Size(width, bits);
        var subH = Vp8LSubSample.Size(height, bits);
        var modeImage = new uint[subW * subH];
        for (var i = 0; i < modeImage.Length; i++)
            modeImage[i] = 0xFF000000u | ((uint)mode << 8);

        var work = (uint[])argb.Clone();
        PredictorTransform.Forward(work, width, height, modeImage, bits);
        PredictorTransform.Inverse(work, width, height, modeImage, bits);

        Assert.Equal(argb, work);
    }

    [Fact]
    public void PredictorTransform_MixedModeImage_ForwardInverse_IsIdentity()
    {
        const int width = 40, height = 24, bits = 2;
        var rng = new Random(77);
        var argb = new uint[width * height];
        for (var i = 0; i < argb.Length; i++)
            argb[i] = (uint)rng.NextInt64(0, 1L << 32);

        var subW = Vp8LSubSample.Size(width, bits);
        var subH = Vp8LSubSample.Size(height, bits);
        var modeImage = new uint[subW * subH];
        for (var i = 0; i < modeImage.Length; i++)
            modeImage[i] = 0xFF000000u | ((uint)rng.Next(0, 14) << 8);

        var work = (uint[])argb.Clone();
        PredictorTransform.Forward(work, width, height, modeImage, bits);
        PredictorTransform.Inverse(work, width, height, modeImage, bits);

        Assert.Equal(argb, work);
    }
}

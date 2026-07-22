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

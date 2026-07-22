using WebPSharp.Vp8L.Transforms;

namespace WebPSharp.Tests;

public class SubtractGreenTransformTests
{
    [Fact]
    public void ForwardThenInverse_IsIdentity()
    {
        var rng = new Random(5);
        var argb = new uint[1000];
        for (var i = 0; i < argb.Length; i++)
            argb[i] = (uint)rng.NextInt64(0, 1L << 32);
        var original = (uint[])argb.Clone();

        SubtractGreenTransform.Forward(argb);
        SubtractGreenTransform.Inverse(argb);

        Assert.Equal(original, argb);
    }

    [Fact]
    public void Inverse_AddsGreenToRedAndBlue_ModuloByte()
    {
        // A=0x11, R=0xF0, G=0x20, B=0xFF  -> inverse: R=(0xF0+0x20)=0x10, B=(0xFF+0x20)=0x1F
        var argb = new[] { (0x11u << 24) | (0xF0u << 16) | (0x20u << 8) | 0xFFu };
        SubtractGreenTransform.Inverse(argb);

        Assert.Equal(0x11u, (argb[0] >> 24) & 0xFF); // alpha unchanged
        Assert.Equal(0x10u, (argb[0] >> 16) & 0xFF); // red wrapped
        Assert.Equal(0x20u, (argb[0] >> 8) & 0xFF);  // green unchanged
        Assert.Equal(0x1Fu, argb[0] & 0xFF);         // blue wrapped
    }

    [Fact]
    public void Forward_SubtractsGreenFromRedAndBlue_ModuloByte()
    {
        var argb = new[] { (0x11u << 24) | (0x10u << 16) | (0x20u << 8) | 0x1Fu };
        SubtractGreenTransform.Forward(argb);

        Assert.Equal(0xF0u, (argb[0] >> 16) & 0xFF);
        Assert.Equal(0x20u, (argb[0] >> 8) & 0xFF);
        Assert.Equal(0xFFu, argb[0] & 0xFF);
    }
}

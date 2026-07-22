using JpegSharp.Decoder;
using Xunit;

namespace JpegSharp.Tests;

public class LevelShiftClampTests
{
    private const int Center12 = 1 << (12 - 1);
    private const int Max12 = (1 << 12) - 1;

    [Fact]
    public void Decode_ExtremeCoefficientOverflow_SaturatesToMaxNotZero()
    {
        // Dequantized coefficients (short 32767 * ushort step 65535) drive IDCT outputs
        // that exceed int.MaxValue; the level-shift must not wrap such values to 0.
        var overflow = 3.0e9;

        Assert.Equal(255, BaselineDecoder.LevelShiftClamp8(overflow));
        Assert.Equal(Max12, BaselineDecoder.LevelShiftClampHigh(overflow, Center12, Max12));
    }

    [Fact]
    public void Decode_AllZeroCoefficientBlock_ReconstructsLevelShiftCenter()
    {
        Assert.Equal(128, BaselineDecoder.LevelShiftClamp8(0.0));
        Assert.Equal(Center12, BaselineDecoder.LevelShiftClampHigh(0.0, Center12, Max12));
    }

    [Fact]
    public void Decode_LargeNegative_SaturatesToZero()
    {
        var negative = -3.0e9;

        Assert.Equal(0, BaselineDecoder.LevelShiftClamp8(negative));
        Assert.Equal(0, BaselineDecoder.LevelShiftClampHigh(negative, Center12, Max12));
    }

    [Fact]
    public void Decode_LargePositive_SaturatesToMax()
    {
        var positive = 3.0e9;

        Assert.Equal(255, BaselineDecoder.LevelShiftClamp8(positive));
        Assert.Equal(Max12, BaselineDecoder.LevelShiftClampHigh(positive, Center12, Max12));
    }

    [Fact]
    public void Decode_InRangeValues_PreserveLevelShiftedSample()
    {
        Assert.Equal(200, BaselineDecoder.LevelShiftClamp8(72.0));
        Assert.Equal(56, BaselineDecoder.LevelShiftClamp8(-72.0));
        Assert.Equal(3048, BaselineDecoder.LevelShiftClampHigh(1000.0, Center12, Max12));
    }
}

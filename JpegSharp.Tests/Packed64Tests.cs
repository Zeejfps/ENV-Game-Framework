using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class Packed64Tests
{
    [Fact]
    public void Rgb_PacksChannelsPerFormat_AlphaIsMaxSample()
    {
        // 12-bit pixel R=100, G=2000, B=4095.
        var image = JpegImage16.CreateRgb(1, 1, 12, [100, 2000, 4095]);
        const long a = 4095; // MaxSampleValue for 12-bit

        Assert.Equal((100L << 48) | (2000L << 32) | (4095L << 16) | a, image.ToPackedPixels64(PackedPixelFormat64.Rgba16161616)[0]);
        Assert.Equal((a << 48) | (100L << 32) | (2000L << 16) | 4095L, image.ToPackedPixels64(PackedPixelFormat64.Argb16161616)[0]);
        Assert.Equal((4095L << 48) | (2000L << 32) | (100L << 16) | a, image.ToPackedPixels64(PackedPixelFormat64.Bgra16161616)[0]);
        Assert.Equal((a << 48) | (4095L << 32) | (2000L << 16) | 100L, image.ToPackedPixels64(PackedPixelFormat64.Abgr16161616)[0]);
    }

    [Fact]
    public void Grayscale_ReplicatesAcrossChannels()
    {
        var image = JpegImage16.CreateGrayscale(1, 1, 12, [1234]);
        Assert.Equal((1234L << 48) | (1234L << 32) | (1234L << 16) | 4095L, image.ToRgba16161616()[0]);
    }

    [Fact]
    public void NamedWrappers_MatchGeneralForm()
    {
        var image = JpegImage16.CreateRgb(2, 1, 12, [10, 20, 30, 40, 50, 60]);

        Assert.Equal(image.ToPackedPixels64(PackedPixelFormat64.Rgba16161616), image.ToRgba16161616());
        Assert.Equal(image.ToPackedPixels64(PackedPixelFormat64.Argb16161616), image.ToArgb16161616());
        Assert.Equal(image.ToPackedPixels64(PackedPixelFormat64.Bgra16161616), image.ToBgra16161616());
        Assert.Equal(image.ToPackedPixels64(PackedPixelFormat64.Abgr16161616), image.ToAbgr16161616());
    }

    [Fact]
    public void SpanOverload_MatchesArray_AndLeavesExtraUntouched()
    {
        var image = JpegImage16.CreateRgb(2, 1, 12, [10, 20, 30, 40, 50, 60]);
        var expected = image.ToBgra16161616();

        var destination = new long[3];
        destination[2] = unchecked((long)0xDEADBEEFDEADBEEF);
        image.ToBgra16161616(destination);

        Assert.Equal(expected[0], destination[0]);
        Assert.Equal(expected[1], destination[1]);
        Assert.Equal(unchecked((long)0xDEADBEEFDEADBEEF), destination[2]);
    }

    [Fact]
    public void RoundTripsThroughCreateFromPacked()
    {
        var original = JpegImage16.CreateRgb(2, 2, 12, [
            10, 20, 30,     40, 50, 60,
            4095, 0, 2048,  1, 2, 3,
        ]);

        foreach (var format in new[]
                 {
                     PackedPixelFormat64.Rgba16161616, PackedPixelFormat64.Argb16161616,
                     PackedPixelFormat64.Bgra16161616, PackedPixelFormat64.Abgr16161616,
                 })
        {
            var packed = original.ToPackedPixels64(format);
            var rebuilt = JpegImage16.CreateFromPackedPixels64(2, 2, 12, packed, format);
            Assert.Equal(original.PixelData, rebuilt.PixelData);
            Assert.Equal(12, rebuilt.Precision);
        }
    }

    [Fact]
    public void SpanOverload_ThrowsWhenDestinationTooSmall()
    {
        var image = JpegImage16.CreateRgb(2, 2, 12, new ushort[2 * 2 * 3]);
        Assert.Throws<ArgumentException>(() => image.ToPackedPixels64(new long[3], PackedPixelFormat64.Rgba16161616));
    }

    [Fact]
    public void CreateFromPacked_ThrowsWhenPixelCountMismatched()
    {
        Assert.Throws<ArgumentException>(() => JpegImage16.CreateFromRgba16161616(2, 2, 12, new long[3]));
    }

    [Fact]
    public void Cmyk_NoInk_IsWhiteAtFullScale()
    {
        // No ink → white; channels are native samples so white is MaxSampleValue (4095), alpha too.
        var image = JpegImage16.CreateCmyk(1, 1, 12, [0, 0, 0, 0]);
        Assert.Equal((4095L << 48) | (4095L << 32) | (4095L << 16) | 4095L, image.ToRgba16161616()[0]);
    }

    [Fact]
    public void Cmyk_FullBlack_IsBlack()
    {
        var image = JpegImage16.CreateCmyk(1, 1, 12, [0, 0, 0, 4095]);
        // R=G=B=0, alpha=MaxSampleValue.
        Assert.Equal(4095L, image.ToRgba16161616()[0]);
    }

    [Fact]
    public void Cmyk_UsesMultiplicativeModel()
    {
        const int c = 500, m = 1000, y = 1500, k = 200, max = 4095;
        var image = JpegImage16.CreateCmyk(1, 1, 12, [c, m, y, k]);

        long r = (long)(max - c) * (max - k) / max;
        long g = (long)(max - m) * (max - k) / max;
        long b = (long)(max - y) * (max - k) / max;
        Assert.Equal((r << 48) | (g << 32) | (b << 16) | max, image.ToRgba16161616()[0]);
    }
}

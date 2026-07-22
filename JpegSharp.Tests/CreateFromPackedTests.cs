using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class CreateFromPackedTests
{
    [Fact]
    public void UnpacksChannelsPerFormat_DroppingAlpha()
    {
        // Packed value with a non-opaque alpha to prove alpha is discarded.
        int rgba = (10 << 24) | (20 << 16) | (30 << 8) | 128;
        var image = JpegImage.CreateFromRgba8888(1, 1, [rgba]);

        Assert.Equal(JpegColorSpace.Rgb, image.ColorSpace);
        Assert.Equal([10, 20, 30], image.PixelData);
    }

    [Fact]
    public void RoundTripsWithPackingHelpers()
    {
        var original = JpegImage.CreateRgb(2, 2, [
            10, 20, 30,     40, 50, 60,
            70, 80, 90,     100, 110, 120,
        ]);

        foreach (var format in new[]
                 {
                     PackedPixelFormat.Rgba8888, PackedPixelFormat.Argb8888,
                     PackedPixelFormat.Bgra8888, PackedPixelFormat.Abgr8888,
                 })
        {
            var packed = original.ToPackedPixels(format);
            var rebuilt = JpegImage.CreateFromPackedPixels(2, 2, packed, format);
            Assert.Equal(original.PixelData, rebuilt.PixelData);
        }
    }

    [Fact]
    public void NamedWrappers_MatchGeneralForm()
    {
        int[] pixels = [0x01020304, unchecked((int)0xFFEEDDCC)];

        Assert.Equal(JpegImage.CreateFromPackedPixels(2, 1, pixels, PackedPixelFormat.Rgba8888).PixelData, JpegImage.CreateFromRgba8888(2, 1, pixels).PixelData);
        Assert.Equal(JpegImage.CreateFromPackedPixels(2, 1, pixels, PackedPixelFormat.Argb8888).PixelData, JpegImage.CreateFromArgb8888(2, 1, pixels).PixelData);
        Assert.Equal(JpegImage.CreateFromPackedPixels(2, 1, pixels, PackedPixelFormat.Bgra8888).PixelData, JpegImage.CreateFromBgra8888(2, 1, pixels).PixelData);
        Assert.Equal(JpegImage.CreateFromPackedPixels(2, 1, pixels, PackedPixelFormat.Abgr8888).PixelData, JpegImage.CreateFromAbgr8888(2, 1, pixels).PixelData);
    }

    [Fact]
    public void ThrowsWhenPixelCountMismatched()
    {
        Assert.Throws<ArgumentException>(() => JpegImage.CreateFromRgba8888(2, 2, new int[3]));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void ThrowsWhenDimensionNotPositive(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => JpegImage.CreateFromRgba8888(width, height, new int[Math.Max(0, width * height)]));
    }
}

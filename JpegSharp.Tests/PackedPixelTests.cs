using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class PackedPixelTests
{
    [Fact]
    public void Rgb_PacksChannelsPerFormat()
    {
        // Single pixel R=10, G=20, B=30.
        var image = JpegImage.CreateRgb(1, 1, [10, 20, 30]);

        Assert.Equal((10 << 24) | (20 << 16) | (30 << 8) | 255, image.ToPackedPixels(PackedPixelFormat.Rgba8888)[0]);
        Assert.Equal((255 << 24) | (10 << 16) | (20 << 8) | 30, image.ToPackedPixels(PackedPixelFormat.Argb8888)[0]);
        Assert.Equal((30 << 24) | (20 << 16) | (10 << 8) | 255, image.ToPackedPixels(PackedPixelFormat.Bgra8888)[0]);
        Assert.Equal((255 << 24) | (30 << 16) | (20 << 8) | 10, image.ToPackedPixels(PackedPixelFormat.Abgr8888)[0]);
    }

    [Fact]
    public void NamedExtensions_MatchExplicitFormat()
    {
        var image = JpegImage.CreateRgb(1, 1, [10, 20, 30]);

        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Rgba8888), image.ToRgba8888());
        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Argb8888), image.ToArgb8888());
        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Bgra8888), image.ToBgra8888());
        Assert.Equal(image.ToPackedPixels(PackedPixelFormat.Abgr8888), image.ToAbgr8888());
    }

    [Fact]
    public void NamedExtension_SpanOverload_MatchesArray()
    {
        var image = JpegImage.CreateRgb(2, 1, [10, 20, 30, 40, 50, 60]);
        var destination = new int[2];
        image.ToArgb8888(destination);
        Assert.Equal(image.ToArgb8888(), destination);
    }

    [Fact]
    public void Grayscale_ReplicatesLuminanceAndIsOpaque()
    {
        var image = JpegImage.CreateGrayscale(1, 1, [128]);
        Assert.Equal((128 << 24) | (128 << 16) | (128 << 8) | 255, image.ToRgba8888()[0]);
    }

    [Fact]
    public void ProducesOnePixelPerSampleInRowMajorOrder()
    {
        var image = JpegImage.CreateRgb(2, 2, [
            0, 0, 0,        1, 1, 1,
            2, 2, 2,        3, 3, 3,
        ]);

        var packed = image.ToRgba8888();
        Assert.Equal(4, packed.Length);
        Assert.Equal((3 << 24) | (3 << 16) | (3 << 8) | 255, packed[3]);
    }

    [Fact]
    public void SpanOverload_MatchesArrayOverload_AndLeavesExtraUntouched()
    {
        var image = JpegImage.CreateRgb(2, 1, [10, 20, 30, 40, 50, 60]);
        var expected = image.ToPackedPixels(PackedPixelFormat.Bgra8888);

        var destination = new int[3];
        destination[2] = unchecked((int)0xDEADBEEF);
        image.ToPackedPixels(destination, PackedPixelFormat.Bgra8888);

        Assert.Equal(expected[0], destination[0]);
        Assert.Equal(expected[1], destination[1]);
        Assert.Equal(unchecked((int)0xDEADBEEF), destination[2]);
    }

    [Fact]
    public void SpanOverload_ThrowsWhenDestinationTooSmall()
    {
        var image = JpegImage.CreateRgb(2, 2, new byte[2 * 2 * 3]);
        Assert.Throws<ArgumentException>(() => image.ToPackedPixels(new int[3], PackedPixelFormat.Rgba8888));
    }

    [Fact]
    public void Cmyk_Throws()
    {
        var image = JpegImage.CreateCmyk(1, 1, [10, 20, 30, 40]);
        Assert.Throws<NotSupportedException>(() => image.ToRgba8888());
    }
}

using WebPSharp.Api;

namespace WebPSharp.Tests;

public class WebPInfoEnrichmentTests
{
    private static WebPImage Noise(int w, int h, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(w, h, pixels);
    }

    [Fact]
    public void SimpleLossless_HasNoMetadataOrFrames()
    {
        var info = WebP.Identify(WebP.Encode(Noise(8, 8, 1)));
        Assert.False(info.HasIccProfile);
        Assert.False(info.HasExif);
        Assert.False(info.HasXmp);
        Assert.Equal(0, info.FrameCount);
        Assert.Equal(0, info.LoopCount);
    }

    [Fact]
    public void ExtendedWithMetadata_ReportsFlags()
    {
        var image = Noise(8, 8, 2);
        image.Metadata = new WebPMetadata
        {
            IccProfile = new byte[] { 1 },
            Xmp = new byte[] { 2, 3 },
        };
        var info = WebP.Identify(WebP.Encode(image));

        Assert.Equal(WebPFormat.Extended, info.Format);
        Assert.True(info.HasIccProfile);
        Assert.False(info.HasExif);
        Assert.True(info.HasXmp);
    }

    [Fact]
    public void Animation_ReportsFrameAndLoopCount()
    {
        var anim = new WebPAnimation(16, 16) { LoopCount = 7 };
        for (var f = 0; f < 5; f++)
            anim.Frames.Add(new WebPFrame(Noise(16, 16, f)));

        var info = WebP.Identify(WebP.EncodeAnimation(anim));
        Assert.True(info.HasAnimation);
        Assert.Equal(5, info.FrameCount);
        Assert.Equal(7, info.LoopCount);
    }

    [Fact]
    public void Animation_WithMetadata_ReportsBoth()
    {
        var anim = new WebPAnimation(10, 10) { LoopCount = 3 };
        anim.Frames.Add(new WebPFrame(Noise(10, 10, 1)));
        anim.Metadata = new WebPMetadata { Exif = new byte[] { 9, 9 } };

        var info = WebP.Identify(WebP.EncodeAnimation(anim));
        Assert.True(info.HasAnimation);
        Assert.Equal(1, info.FrameCount);
        Assert.Equal(3, info.LoopCount);
        Assert.True(info.HasExif);
    }
}

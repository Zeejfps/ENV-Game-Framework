using WebPSharp.Api;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Tests;

// Consolidated contract coverage over the entire public API surface: argument validation, option
// effects, and every entry point round-tripping.
public class WebPApiContractTests
{
    private static WebPImage Sample(int seed = 1)
    {
        var rng = new Random(seed);
        var pixels = new byte[6 * 5 * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(6, 5, pixels);
    }

    [Fact]
    public void StaticMethods_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => WebP.Identify((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => WebP.Identify((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => WebP.IdentifyFile(null!));
        Assert.Throws<ArgumentNullException>(() => WebP.Decode((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => WebP.Decode((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => WebP.Load(null!));
        Assert.Throws<ArgumentNullException>(() => WebP.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => WebP.Save(null!, "x.webp"));
        Assert.Throws<ArgumentNullException>(() => WebP.Save(Sample(), null!));
        Assert.Throws<ArgumentNullException>(() => WebP.EncodeAnimation(null!));
        Assert.Throws<ArgumentNullException>(() => WebP.DecodeAnimation((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => WebP.DecodeAnimation((Stream)null!));
    }

    [Fact]
    public void IdentifyFile_ReadsInfoFromDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"webpsharp_contract_{Guid.NewGuid():N}.webp");
        try
        {
            WebP.Save(Sample(), path);
            var info = WebP.IdentifyFile(path);
            Assert.Equal(6, info.Width);
            Assert.Equal(5, info.Height);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EncoderOptions_LosslessFalse_EncodesImageButThrowsForAnimation()
    {
        // Lossy still-image encoding is supported.
        var bytes = WebP.Encode(Sample(), new WebPEncoderOptions { Lossless = false });
        Assert.Equal(WebPFormat.Lossy, WebP.Identify(bytes).Format);

        // Lossy animation encoding is not yet supported.
        var anim = new WebPAnimation(4, 4);
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(4, 4, new byte[64])));
        Assert.Throws<WebPException>(() => WebP.EncodeAnimation(anim, new WebPEncoderOptions { Lossless = false }));
    }

    [Fact]
    public void EncoderOptions_Defaults_AreSensible()
    {
        var options = new WebPEncoderOptions();
        Assert.True(options.Lossless);
        Assert.InRange(options.Quality, 0, 100);
        Assert.InRange(options.Effort, 0, 9);
    }

    [Fact]
    public void DecoderOptions_Defaults_AreSensible()
    {
        var options = new WebPDecoderOptions();
        Assert.True(options.MaxPixels > 0);
        Assert.True(options.ReadMetadata);
    }

    [Fact]
    public void AllEncodeDecodeEntryPoints_RoundTrip()
    {
        var image = Sample(2);
        var expected = image.PixelData;

        // byte[] path
        Assert.Equal(expected, WebP.Decode(WebP.Encode(image)).PixelData);

        // Stream path
        using var ms = new MemoryStream();
        WebP.Encode(image, ms);
        ms.Position = 0;
        Assert.Equal(expected, WebP.Decode(ms).PixelData);

        // File path
        var path = Path.Combine(Path.GetTempPath(), $"webpsharp_ep_{Guid.NewGuid():N}.webp");
        try
        {
            WebP.Save(image, path);
            Assert.Equal(expected, WebP.Load(path).PixelData);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void WebPImage_ContractsHold()
    {
        var rgba = WebPImage.CreateRgba(2, 3, new byte[2 * 3 * 4]);
        Assert.Equal(WebPColorFormat.Rgba, rgba.Format);
        Assert.Equal(4, rgba.ComponentCount);
        Assert.True(rgba.HasAlpha);
        Assert.Equal(8, rgba.Stride);

        var rgb = WebPImage.CreateRgb(2, 3, new byte[2 * 3 * 3]);
        Assert.False(rgb.HasAlpha);
        Assert.Equal(6, rgb.Stride);

        Assert.Throws<ArgumentNullException>(() => new WebPImage(1, 1, WebPColorFormat.Rgba, null!));
        Assert.Throws<ArgumentException>(() => new WebPImage(2, 2, WebPColorFormat.Rgba, new byte[3]));
    }

    [Fact]
    public void WebPFrame_ValidatesConstruction()
    {
        var img = WebPImage.CreateRgba(2, 2, new byte[16]);
        Assert.Throws<ArgumentNullException>(() => new WebPFrame(null!));
        Assert.Throws<ArgumentException>(() => new WebPFrame(img, x: 1));
        Assert.Throws<ArgumentException>(() => new WebPFrame(img, y: -2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebPFrame(img, durationMs: -1));

        var frame = new WebPFrame(img, 2, 4, 60, WebPBlendMethod.Source, WebPDisposalMethod.Background);
        Assert.Equal(2, frame.X);
        Assert.Equal(4, frame.Y);
        Assert.Equal(2, frame.Width);
        Assert.Equal(WebPBlendMethod.Source, frame.Blend);
        Assert.Equal(WebPDisposalMethod.Background, frame.Disposal);
    }

    [Fact]
    public void WebPAnimation_ValidatesDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebPAnimation(0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebPAnimation(4, -1));
        var anim = new WebPAnimation(8, 8);
        Assert.Empty(anim.Frames);
        Assert.Equal(0, anim.LoopCount);
    }
}

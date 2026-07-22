using WebPSharp.Api;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Tests;

public class WebPAnimationTests
{
    private static WebPImage SolidRgba(int w, int h, byte r, byte g, byte b, byte a)
    {
        var pixels = new byte[w * h * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r; pixels[i + 1] = g; pixels[i + 2] = b; pixels[i + 3] = a;
        }
        return WebPImage.CreateRgba(w, h, pixels);
    }

    private static WebPImage NoiseRgba(int w, int h, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(w, h, pixels);
    }

    [Fact]
    public void RoundTrip_PreservesFramesAndGlobals()
    {
        var anim = new WebPAnimation(32, 24) { LoopCount = 5, BackgroundColor = 0x80112233 };
        anim.Frames.Add(new WebPFrame(NoiseRgba(32, 24, 1), 0, 0, 100, WebPBlendMethod.Source, WebPDisposalMethod.None));
        anim.Frames.Add(new WebPFrame(NoiseRgba(8, 6, 2), 4, 2, 250, WebPBlendMethod.Over, WebPDisposalMethod.Background));
        anim.Frames.Add(new WebPFrame(NoiseRgba(16, 10, 3), 6, 8, 40, WebPBlendMethod.Source, WebPDisposalMethod.None));

        var decoded = WebP.DecodeAnimation(WebP.EncodeAnimation(anim));

        Assert.Equal(32, decoded.Width);
        Assert.Equal(24, decoded.Height);
        Assert.Equal(5, decoded.LoopCount);
        Assert.Equal(0x80112233u, decoded.BackgroundColor);
        Assert.Equal(3, decoded.Frames.Count);

        for (var i = 0; i < 3; i++)
        {
            var a = anim.Frames[i];
            var b = decoded.Frames[i];
            Assert.Equal(a.X, b.X);
            Assert.Equal(a.Y, b.Y);
            Assert.Equal(a.DurationMs, b.DurationMs);
            Assert.Equal(a.Blend, b.Blend);
            Assert.Equal(a.Disposal, b.Disposal);
            Assert.Equal(a.Image.Width, b.Image.Width);
            Assert.Equal(a.Image.Height, b.Image.Height);
            Assert.Equal(a.Image.PixelData, b.Image.PixelData);
        }
    }

    [Fact]
    public void Identify_ReportsAnimation()
    {
        var anim = new WebPAnimation(10, 10);
        anim.Frames.Add(new WebPFrame(SolidRgba(10, 10, 1, 2, 3, 255)));
        var info = WebP.Identify(WebP.EncodeAnimation(anim));
        Assert.Equal(WebPFormat.Extended, info.Format);
        Assert.True(info.HasAnimation);
    }

    [Fact]
    public void Decode_OnAnimatedFile_Throws()
    {
        var anim = new WebPAnimation(10, 10);
        anim.Frames.Add(new WebPFrame(SolidRgba(10, 10, 9, 9, 9, 255)));
        var bytes = WebP.EncodeAnimation(anim);
        Assert.Throws<WebPException>(() => WebP.Decode(bytes));
    }

    [Fact]
    public void EncodeAnimation_NoFrames_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => WebP.EncodeAnimation(new WebPAnimation(10, 10)));
    }

    [Fact]
    public void Frame_OddOffset_Throws()
    {
        Assert.Throws<ArgumentException>(() => new WebPFrame(SolidRgba(4, 4, 0, 0, 0, 255), x: 3, y: 0));
    }

    [Fact]
    public void Compose_SourceBlend_OverwritesRectangle()
    {
        var anim = new WebPAnimation(4, 2) { BackgroundColor = 0xFF000000 }; // opaque black
        // Frame 0: fill whole canvas red.
        anim.Frames.Add(new WebPFrame(SolidRgba(4, 2, 255, 0, 0, 255), 0, 0, 100, WebPBlendMethod.Source));
        // Frame 1: a 2x2 green block at (2,0), overwrite, no dispose.
        anim.Frames.Add(new WebPFrame(SolidRgba(2, 2, 0, 255, 0, 255), 2, 0, 100, WebPBlendMethod.Source));

        var frames = anim.RenderFrames();
        Assert.Equal(2, frames.Count);

        // Frame 0 is all red.
        for (var i = 0; i < frames[0].Length; i += 4)
        {
            Assert.Equal(255, frames[0][i]);
            Assert.Equal(0, frames[0][i + 1]);
            Assert.Equal(0, frames[0][i + 2]);
        }

        // Frame 1: left half still red, right half (x>=2) green.
        byte PixelAt(byte[] f, int x, int y, int ch) => f[(y * 4 + x) * 4 + ch];
        Assert.Equal(255, PixelAt(frames[1], 0, 0, 0)); // red
        Assert.Equal(0, PixelAt(frames[1], 0, 0, 1));
        Assert.Equal(0, PixelAt(frames[1], 2, 0, 0));   // green
        Assert.Equal(255, PixelAt(frames[1], 2, 0, 1));
        Assert.Equal(255, PixelAt(frames[1], 3, 1, 1));
    }

    [Fact]
    public void Compose_DisposeBackground_RestoresBackground()
    {
        var anim = new WebPAnimation(4, 1) { BackgroundColor = 0xFF0000FF }; // opaque blue (ARGB)
        anim.Frames.Add(new WebPFrame(SolidRgba(2, 1, 255, 0, 0, 255), 0, 0, 100, WebPBlendMethod.Source, WebPDisposalMethod.Background));
        anim.Frames.Add(new WebPFrame(SolidRgba(2, 1, 0, 255, 0, 255), 2, 0, 100, WebPBlendMethod.Source));

        var frames = anim.RenderFrames();
        // After frame 0 renders red at x0-1, it disposes to background before frame 1.
        // Frame 1 draws green at x2-3, so x0-1 should be background blue.
        byte PixelAt(byte[] f, int x, int ch) => f[x * 4 + ch];
        Assert.Equal(0, PixelAt(frames[1], 0, 0));    // R
        Assert.Equal(0, PixelAt(frames[1], 0, 1));    // G
        Assert.Equal(255, PixelAt(frames[1], 0, 2));  // B (background blue)
        Assert.Equal(0, PixelAt(frames[1], 2, 0));    // green block
        Assert.Equal(255, PixelAt(frames[1], 2, 1));
    }

    [Fact]
    public void SingleFrame_RoundTrips()
    {
        var anim = new WebPAnimation(6, 6) { LoopCount = 0 };
        anim.Frames.Add(new WebPFrame(NoiseRgba(6, 6, 42)));
        var decoded = WebP.DecodeAnimation(WebP.EncodeAnimation(anim));
        Assert.Single(decoded.Frames);
        Assert.Equal(anim.Frames[0].Image.PixelData, decoded.Frames[0].Image.PixelData);
    }
}

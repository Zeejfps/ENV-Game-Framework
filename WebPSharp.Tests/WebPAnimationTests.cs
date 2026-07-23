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

    [Fact]
    public void Decode_AnmfFrameRectExceedsCanvas_Throws()
    {
        // Canvas 10x10. Frame is 6x6 placed so x + width = 6 + 6 = 12 > 10.
        var xAnim = new WebPAnimation(10, 10);
        xAnim.Frames.Add(new WebPFrame(NoiseRgba(6, 6, 11), x: 6, y: 0));
        var xBytes = WebP.EncodeAnimation(xAnim);
        Assert.Throws<WebPFormatException>(() => WebP.DecodeAnimation(xBytes));

        // Same canvas, frame placed so y + height = 6 + 6 = 12 > 10.
        var yAnim = new WebPAnimation(10, 10);
        yAnim.Frames.Add(new WebPFrame(NoiseRgba(6, 6, 12), x: 0, y: 6));
        var yBytes = WebP.EncodeAnimation(yAnim);
        Assert.Throws<WebPFormatException>(() => WebP.DecodeAnimation(yBytes));

        // A frame exactly filling the canvas is in bounds and must still decode.
        var fitAnim = new WebPAnimation(10, 10);
        fitAnim.Frames.Add(new WebPFrame(NoiseRgba(10, 10, 13), x: 0, y: 0));
        var decoded = WebP.DecodeAnimation(WebP.EncodeAnimation(fitAnim));
        Assert.Single(decoded.Frames);

        // A frame flush against the far edge (x + width == canvasWidth) is also in bounds.
        var edgeAnim = new WebPAnimation(10, 10);
        edgeAnim.Frames.Add(new WebPFrame(NoiseRgba(6, 6, 14), x: 4, y: 4));
        var edgeDecoded = WebP.DecodeAnimation(WebP.EncodeAnimation(edgeAnim));
        Assert.Single(edgeDecoded.Frames);
    }

    [Theory]
    [InlineData(20, 20)] // declared dims LARGER than the actual 8x8 sub-image
    [InlineData(4, 4)]   // declared dims SMALLER than the actual 8x8 sub-image
    public void Decode_AnmfDeclaredDimsMismatch_UsesActualImageDims_MatchesLibwebp(int declaredWidth, int declaredHeight)
    {
        // libwebp's demuxer (src/demux/demux.c) reads the ANMF-declared Frame Width/Height
        // (ParseAnimationFrame) but then OVERWRITES them with the actual VP8/VP8L bitstream
        // dimensions in SetFrameInfo (frame->width = features->width; frame->height = ...).
        // It performs NO declared-vs-actual mismatch rejection; the declared fields are
        // discarded. CheckFrameBounds and the iterator report the actual image dims.
        // Empirically, `webpmux -info` on a file whose ANMF declares 20x20 or 4x4 over an
        // 8x8 sub-image still reports width=8 height=8 and decodes fine. WebPSharp mirrors
        // this: it derives frame dims from the decoded sub-image and ignores the declared
        // fields, so patching them must not change the decode result.
        var anim = new WebPAnimation(8, 8) { LoopCount = 0 };
        anim.Frames.Add(new WebPFrame(NoiseRgba(8, 8, 99)));
        var reference = WebP.EncodeAnimation(anim);

        var patched = (byte[])reference.Clone();
        var anmf = IndexOfFourCc(patched, "ANMF");
        Assert.True(anmf >= 0, "expected an ANMF chunk in the encoded animation");
        // ANMF payload starts after the 8-byte chunk header. Frame Width Minus One is at
        // payload offset 6, Frame Height Minus One at payload offset 9 (both 24-bit LE).
        var payload = anmf + 8;
        WriteUInt24Le(patched, payload + 6, declaredWidth - 1);
        WriteUInt24Le(patched, payload + 9, declaredHeight - 1);

        // Sanity: the patch actually changed the declared dims but left the VP8L intact.
        Assert.NotEqual(reference, patched);

        var decoded = WebP.DecodeAnimation(patched);

        // Matches libwebp: decode succeeds and reports the ACTUAL 8x8 sub-image dims/pixels,
        // identical to decoding the unpatched file — declared dims are ignored.
        var referenceDecoded = WebP.DecodeAnimation(reference);
        Assert.Single(decoded.Frames);
        Assert.Equal(8, decoded.Frames[0].Width);
        Assert.Equal(8, decoded.Frames[0].Height);
        Assert.Equal(referenceDecoded.Frames[0].Image.PixelData, decoded.Frames[0].Image.PixelData);
    }

    private static void WriteUInt24Le(byte[] data, int offset, int value)
    {
        data[offset + 0] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    private static int IndexOfFourCc(byte[] data, string fourCc)
    {
        for (var i = 0; i + 4 <= data.Length; i++)
        {
            if (data[i] == fourCc[0] && data[i + 1] == fourCc[1] &&
                data[i + 2] == fourCc[2] && data[i + 3] == fourCc[3])
                return i;
        }
        return -1;
    }

    [Theory]
    [InlineData(0x7FFFFFFFu)]
    [InlineData(0x80000000u)]
    [InlineData(0xFFFFFFFFu)]
    public void Decode_AnmfInnerChunkOversized_ThrowsFormatException(uint oversizedSize)
    {
        var anim = new WebPAnimation(6, 6) { LoopCount = 0 };
        anim.Frames.Add(new WebPFrame(NoiseRgba(6, 6, 7)));
        var bytes = WebP.EncodeAnimation(anim);

        // Locate the ANMF chunk, skip its 8-byte header and 16-byte frame header to reach the
        // inner sub-chunk header, then overwrite the inner sub-chunk's 4-byte size field.
        var anmf = IndexOfFourCc(bytes, "ANMF");
        Assert.True(anmf >= 0, "expected an ANMF chunk in the encoded animation");
        var innerSizeOffset = anmf + 8 + 16 + 4;
        bytes[innerSizeOffset + 0] = (byte)(oversizedSize & 0xFF);
        bytes[innerSizeOffset + 1] = (byte)((oversizedSize >> 8) & 0xFF);
        bytes[innerSizeOffset + 2] = (byte)((oversizedSize >> 16) & 0xFF);
        bytes[innerSizeOffset + 3] = (byte)((oversizedSize >> 24) & 0xFF);

        Assert.Throws<WebPFormatException>(() => WebP.DecodeAnimation(bytes));
    }
}

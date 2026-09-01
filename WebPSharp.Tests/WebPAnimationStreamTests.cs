using WebPSharp.Api;
using WebPSharp.Api.Exceptions;
using WebPSharp.Container;

namespace WebPSharp.Tests;

public class WebPAnimationStreamTests
{
    private static WebPAnimation MakeAnimation(int frames, int seed)
    {
        var anim = new WebPAnimation(16, 12) { LoopCount = 4 };
        var rng = new Random(seed);
        for (var f = 0; f < frames; f++)
        {
            var pixels = new byte[16 * 12 * 4];
            rng.NextBytes(pixels);
            anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(16, 12, pixels), durationMs: 50 + f));
        }
        return anim;
    }

    [Fact]
    public void EncodeAnimation_ToStream_Then_Decode_RoundTrips()
    {
        var anim = MakeAnimation(3, 1);
        using var ms = new MemoryStream();
        WebP.EncodeAnimation(anim, ms);
        ms.Position = 0;
        var decoded = WebP.DecodeAnimation(ms);
        Assert.Equal(3, decoded.Frames.Count);
        Assert.Equal(anim.Frames[1].Image.PixelData, decoded.Frames[1].Image.PixelData);
    }

    [Fact]
    public async Task EncodeAnimationAsync_Then_DecodeAnimationAsync_RoundTrips()
    {
        var anim = MakeAnimation(4, 2);
        using var ms = new MemoryStream();
        await WebP.EncodeAnimationAsync(anim, ms);
        ms.Position = 0;
        var decoded = await WebP.DecodeAnimationAsync(ms);
        Assert.Equal(4, decoded.Frames.Count);
        Assert.Equal(anim.LoopCount, decoded.LoopCount);
        for (var f = 0; f < 4; f++)
            Assert.Equal(anim.Frames[f].Image.PixelData, decoded.Frames[f].Image.PixelData);
    }

    [Fact]
    public async Task DecodeAnimationAsync_CanceledToken_Throws()
    {
        var bytes = WebP.EncodeAnimation(MakeAnimation(2, 3));
        using var ms = new MemoryStream(bytes);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await WebP.DecodeAnimationAsync(ms, cancellationToken: cts.Token));
    }

    [Fact]
    public void Vp8XCanvasDimensionMismatch_Throws()
    {
        // VP8X declares a 10x10 canvas but the VP8L image is 8x8 -> inconsistent.
        var rng = new Random(9);
        var pixels = new byte[8 * 8 * 4];
        rng.NextBytes(pixels);
        var vp8l = Vp8L.Vp8LEncoder.Encode(WebPImage.CreateRgba(8, 8, pixels));

        var vp8x = new byte[10];
        vp8x[4] = 9;  // width - 1 = 9 -> width 10
        vp8x[7] = 9;  // height - 1 = 9 -> height 10

        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        writer.WriteChunk(WebPChunkIds.Vp8X, vp8x);
        writer.WriteChunk(WebPChunkIds.Vp8L, vp8l);
        writer.Complete();

        Assert.Throws<WebPFormatException>(() => WebP.Decode(ms.ToArray()));
    }

    [Fact]
    public void Vp8XCanvasDimensionMatch_Decodes()
    {
        var rng = new Random(10);
        var pixels = new byte[8 * 8 * 4];
        rng.NextBytes(pixels);
        var vp8l = Vp8L.Vp8LEncoder.Encode(WebPImage.CreateRgba(8, 8, pixels));

        var vp8x = new byte[10];
        vp8x[4] = 7; // width 8
        vp8x[7] = 7; // height 8

        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        writer.WriteChunk(WebPChunkIds.Vp8X, vp8x);
        writer.WriteChunk(WebPChunkIds.Vp8L, vp8l);
        writer.Complete();

        var decoded = WebP.Decode(ms.ToArray());
        Assert.Equal(pixels, decoded.PixelData);
    }
}

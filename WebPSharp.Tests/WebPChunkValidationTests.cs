using WebPSharp.Api;
using WebPSharp.Api.Exceptions;
using WebPSharp.Container;

namespace WebPSharp.Tests;

public class WebPChunkValidationTests
{
    private static byte[] Vp8LPayload(int w, int h, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        return Vp8L.Vp8LEncoder.Encode(WebPImage.CreateRgba(w, h, pixels));
    }

    private static byte[] Vp8XHeader(int w, int h, byte flags)
    {
        var p = new byte[10];
        p[0] = flags;
        p[4] = (byte)(w - 1); p[5] = (byte)((w - 1) >> 8); p[6] = (byte)((w - 1) >> 16);
        p[7] = (byte)(h - 1); p[8] = (byte)((h - 1) >> 8); p[9] = (byte)((h - 1) >> 16);
        return p;
    }

    private static byte[] Container(params (FourCc Id, byte[] Payload)[] chunks)
    {
        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        foreach (var (id, payload) in chunks)
            writer.WriteChunk(id, payload);
        writer.Complete();
        return ms.ToArray();
    }

    [Fact]
    public void DuplicateImageChunk_Throws()
    {
        var img = Vp8LPayload(8, 8, 1);
        var bytes = Container(
            (WebPChunkIds.Vp8X, Vp8XHeader(8, 8, 0)),
            (WebPChunkIds.Vp8L, img),
            (WebPChunkIds.Vp8L, img));
        Assert.Throws<WebPFormatException>(() => WebP.Decode(bytes));
    }

    [Fact]
    public void DuplicateIccp_Throws()
    {
        var bytes = Container(
            (WebPChunkIds.Vp8X, Vp8XHeader(8, 8, 0x20)),
            (WebPChunkIds.Iccp, new byte[] { 1, 2 }),
            (WebPChunkIds.Iccp, new byte[] { 3, 4 }),
            (WebPChunkIds.Vp8L, Vp8LPayload(8, 8, 2)));
        Assert.Throws<WebPFormatException>(() => WebP.Decode(bytes));
    }

    [Fact]
    public void DuplicateExif_Throws()
    {
        var bytes = Container(
            (WebPChunkIds.Vp8X, Vp8XHeader(8, 8, 0x08)),
            (WebPChunkIds.Vp8L, Vp8LPayload(8, 8, 3)),
            (WebPChunkIds.Exif, new byte[] { 1 }),
            (WebPChunkIds.Exif, new byte[] { 2 }));
        Assert.Throws<WebPFormatException>(() => WebP.Decode(bytes));
    }

    [Fact]
    public void ValidSingleChunks_Decode()
    {
        var bytes = Container(
            (WebPChunkIds.Vp8X, Vp8XHeader(8, 8, 0x28)),
            (WebPChunkIds.Iccp, new byte[] { 1, 2, 3 }),
            (WebPChunkIds.Vp8L, Vp8LPayload(8, 8, 4)),
            (WebPChunkIds.Exif, new byte[] { 9 }));
        var decoded = WebP.Decode(bytes);
        Assert.Equal(8, decoded.Width);
        Assert.Equal(new byte[] { 1, 2, 3 }, decoded.Metadata!.IccProfile);
        Assert.Equal(new byte[] { 9 }, decoded.Metadata.Exif);
    }

    [Fact]
    public void AnmfBeforeAnim_Throws()
    {
        // Build a minimal ANMF payload (16-byte header + a VP8L chunk).
        var vp8l = Vp8LPayload(8, 8, 5);
        using var frame = new MemoryStream();
        frame.Write(new byte[16]); // x=y=0, w-1=h-1=0... but real dims come from the image
        // Fix width/height minus one in the ANMF header to 7 (8-1).
        var hdr = new byte[16];
        hdr[6] = 7; hdr[9] = 7;
        using var f2 = new MemoryStream();
        f2.Write(hdr);
        RiffWriter.WriteChunkTo(f2, WebPChunkIds.Vp8L, vp8l);
        var anmf = f2.ToArray();

        var bytes = Container(
            (WebPChunkIds.Vp8X, Vp8XHeader(8, 8, 0x02)),
            (WebPChunkIds.Anmf, anmf), // ANMF before ANIM -> invalid
            (WebPChunkIds.Anim, new byte[] { 0, 0, 0, 0, 0, 0 }));
        Assert.Throws<WebPFormatException>(() => WebP.DecodeAnimation(bytes));
    }
}

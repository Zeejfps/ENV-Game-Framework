using WebPSharp.Container;

namespace WebPSharp.Tests;

/// <summary>
/// Helpers for constructing synthetic WebP byte streams used across the container and identify
/// tests. These build only enough of each bitstream header to be recognized; they do not carry
/// decodable pixel data.
/// </summary>
public static class WebPTestData
{
    /// <summary>Wraps a single chunk payload in a valid RIFF/WEBP container.</summary>
    /// <param name="id">The chunk id.</param>
    /// <param name="payload">The chunk payload.</param>
    /// <returns>The container bytes.</returns>
    public static byte[] Container(FourCc id, byte[] payload)
    {
        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        writer.WriteChunk(id, payload);
        writer.Complete();
        return ms.ToArray();
    }

    /// <summary>Wraps several chunks in a valid RIFF/WEBP container in order.</summary>
    /// <param name="chunks">The chunks to write.</param>
    /// <returns>The container bytes.</returns>
    public static byte[] Container(params (FourCc Id, byte[] Payload)[] chunks)
    {
        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        foreach (var (id, payload) in chunks)
            writer.WriteChunk(id, payload);
        writer.Complete();
        return ms.ToArray();
    }

    /// <summary>Builds a minimal VP8 (lossy) key-frame header carrying the given dimensions.</summary>
    /// <param name="width">Image width (1..16383).</param>
    /// <param name="height">Image height (1..16383).</param>
    /// <returns>The <c>VP8&#160;</c> chunk payload.</returns>
    public static byte[] Vp8Header(int width, int height)
    {
        var p = new byte[10];
        // Frame tag: all zero => key frame, version 0, show_frame 0, partition length 0.
        p[3] = 0x9D;
        p[4] = 0x01;
        p[5] = 0x2A;
        p[6] = (byte)(width & 0xFF);
        p[7] = (byte)((width >> 8) & 0x3F);
        p[8] = (byte)(height & 0xFF);
        p[9] = (byte)((height >> 8) & 0x3F);
        return p;
    }

    /// <summary>Builds a minimal VP8L (lossless) header carrying the given dimensions and alpha flag.</summary>
    /// <param name="width">Image width (1..16384).</param>
    /// <param name="height">Image height (1..16384).</param>
    /// <param name="hasAlpha">Whether the alpha-is-used bit is set.</param>
    /// <returns>The <c>VP8L</c> chunk payload.</returns>
    public static byte[] Vp8LHeader(int width, int height, bool hasAlpha)
    {
        var p = new byte[5];
        p[0] = 0x2F;
        var bits = (uint)(width - 1)
                   | ((uint)(height - 1) << 14)
                   | ((hasAlpha ? 1u : 0u) << 28);
        p[1] = (byte)bits;
        p[2] = (byte)(bits >> 8);
        p[3] = (byte)(bits >> 16);
        p[4] = (byte)(bits >> 24);
        return p;
    }

    /// <summary>Builds a VP8X extended header.</summary>
    /// <param name="width">Canvas width (1..16777216).</param>
    /// <param name="height">Canvas height (1..16777216).</param>
    /// <param name="hasAlpha">Whether the alpha flag is set.</param>
    /// <param name="hasAnimation">Whether the animation flag is set.</param>
    /// <returns>The <c>VP8X</c> chunk payload.</returns>
    public static byte[] Vp8XHeader(int width, int height, bool hasAlpha = false, bool hasAnimation = false)
    {
        var p = new byte[10];
        byte flags = 0;
        if (hasAlpha) flags |= 0x10;
        if (hasAnimation) flags |= 0x02;
        p[0] = flags;
        var w = width - 1;
        var h = height - 1;
        p[4] = (byte)w;
        p[5] = (byte)(w >> 8);
        p[6] = (byte)(w >> 16);
        p[7] = (byte)h;
        p[8] = (byte)(h >> 8);
        p[9] = (byte)(h >> 16);
        return p;
    }
}

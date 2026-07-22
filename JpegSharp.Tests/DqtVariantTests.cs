using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class DqtVariantTests
{
    [Fact]
    public void SixteenBitQuantTable_DecodesIdentically()
    {
        var pixels = Gradient(32, 32);
        var image = JpegImage.CreateGrayscale(32, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 });

        var reference = Jpeg.Decode(bytes);
        var rewritten = RewriteDqtAs16Bit(bytes);

        // The quantization values are unchanged, just encoded at 16-bit precision.
        Assert.NotEqual(bytes.Length, rewritten.Length); // the DQT grew
        var decoded = Jpeg.Decode(rewritten);
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void MultipleQuantTablesInOneSegment_Decode()
    {
        var pixels = ColorGradient(24, 24);
        var image = JpegImage.CreateRgb(24, 24, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 });

        var reference = Jpeg.Decode(bytes);
        var merged = MergeDqtSegments(bytes);

        var decoded = Jpeg.Decode(merged);
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void TruncatedSixteenBitDqt_ThrowsFormatException()
    {
        // A DQT claiming 16-bit precision but missing the second half of the table.
        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, 0xDB]);
        var payload = new byte[1 + 64]; // 8-bit worth of data but declared 16-bit
        payload[0] = 0x10; // Pq=1 (16-bit), Tq=0
        var len = payload.Length + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        ms.Write(payload);
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(ms.ToArray()));
    }

    private static byte[] RewriteDqtAs16Bit(byte[] data)
    {
        using var ms = new MemoryStream();
        ms.Write(data, 0, 2); // SOI
        var i = 2;
        while (i < data.Length - 1)
        {
            var code = data[i + 1];
            if (code == 0xDA)
            {
                ms.Write(data, i, data.Length - i);
                break;
            }

            var len = (data[i + 2] << 8) | data[i + 3];
            if (code == 0xDB)
            {
                var payload = data[(i + 4)..(i + 2 + len)];
                var rewritten = new List<byte>();
                var p = 0;
                while (p < payload.Length)
                {
                    var tq = payload[p++] & 0x0F;
                    rewritten.Add((byte)(0x10 | tq)); // Pq=1
                    for (var k = 0; k < 64; k++)
                    {
                        var value = payload[p++];
                        rewritten.Add(0);      // high byte
                        rewritten.Add(value);  // low byte
                    }
                }

                var newLen = rewritten.Count + 2;
                ms.Write([0xFF, 0xDB, (byte)(newLen >> 8), (byte)(newLen & 0xFF)]);
                ms.Write(rewritten.ToArray());
            }
            else
            {
                ms.Write(data, i, 2 + len);
            }

            i += 2 + len;
        }

        return ms.ToArray();
    }

    private static byte[] MergeDqtSegments(byte[] data)
    {
        var payloads = new List<byte[]>();
        var others = new List<(int Start, int Length)>();
        var firstDqt = -1;
        var i = 2;
        while (i < data.Length - 1)
        {
            var code = data[i + 1];
            if (code == 0xDA)
                break;
            var len = (data[i + 2] << 8) | data[i + 3];
            if (code == 0xDB)
            {
                if (firstDqt < 0)
                    firstDqt = others.Count;
                payloads.Add(data[(i + 4)..(i + 2 + len)]);
                others.Add((-1, 0)); // placeholder for the merged segment
            }
            else
            {
                others.Add((i, 2 + len));
            }

            i += 2 + len;
        }

        var scanStart = i;
        using var ms = new MemoryStream();
        ms.Write(data, 0, 2); // SOI
        var wroteMerged = false;
        foreach (var (start, length) in others)
        {
            if (start < 0)
            {
                if (!wroteMerged)
                {
                    var combined = new List<byte>();
                    foreach (var pl in payloads)
                        combined.AddRange(pl);
                    var newLen = combined.Count + 2;
                    ms.Write([0xFF, 0xDB, (byte)(newLen >> 8), (byte)(newLen & 0xFF)]);
                    ms.Write(combined.ToArray());
                    wroteMerged = true;
                }
            }
            else
            {
                ms.Write(data, start, length);
            }
        }

        ms.Write(data, scanStart, data.Length - scanStart);
        return ms.ToArray();
    }

    private static byte[] Gradient(int w, int h)
    {
        var d = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                d[y * w + x] = (byte)((x * 255 / (w - 1) + y * 255 / (h - 1)) / 2);
        return d;
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var idx = (y * w + x) * 3;
                d[idx] = (byte)(x * 255 / (w - 1));
                d[idx + 1] = (byte)(y * 255 / (h - 1));
                d[idx + 2] = (byte)((x + y) % 256);
            }
        return d;
    }
}

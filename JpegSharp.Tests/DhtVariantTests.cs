using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class DhtVariantTests
{
    [Fact]
    public void MultipleHuffmanTablesInOneSegment_Decode()
    {
        var pixels = ColorGradient(24, 24);
        var image = JpegImage.CreateRgb(24, 24, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 });

        var reference = Jpeg.Decode(bytes);
        var merged = MergeSegments(bytes, 0xC4);

        var decoded = Jpeg.Decode(merged);
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void InterScanDhtRedefinition_IsHonored()
    {
        var pixels = ColorGradient(32, 32);
        var image = JpegImage.CreateRgb(32, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 82, Progressive = true });

        var reference = Jpeg.Decode(bytes);

        // Insert a copy of the first DHT segment just before the second scan; the decoder must
        // parse (and re-apply) it between scans without disturbing the result.
        var dht = FindSegmentWithMarker(bytes, 0xC4);
        var withInterScan = InsertBeforeNthScan(bytes, dht, scanIndex: 1);

        var decoded = Jpeg.Decode(withInterScan);
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    private static byte[] MergeSegments(byte[] data, byte marker)
    {
        var payloads = new List<byte[]>();
        var pieces = new List<(int Start, int Length, bool IsTarget)>();
        var i = 2;
        while (i < data.Length - 1)
        {
            var code = data[i + 1];
            if (code == 0xDA)
                break;
            var len = (data[i + 2] << 8) | data[i + 3];
            if (code == marker)
            {
                payloads.Add(data[(i + 4)..(i + 2 + len)]);
                pieces.Add((0, 0, true));
            }
            else
            {
                pieces.Add((i, 2 + len, false));
            }

            i += 2 + len;
        }

        var scanStart = i;
        using var ms = new MemoryStream();
        ms.Write(data, 0, 2);
        var wrote = false;
        foreach (var (start, length, isTarget) in pieces)
        {
            if (isTarget)
            {
                if (!wrote)
                {
                    var combined = new List<byte>();
                    foreach (var pl in payloads)
                        combined.AddRange(pl);
                    var newLen = combined.Count + 2;
                    ms.Write([0xFF, marker, (byte)(newLen >> 8), (byte)(newLen & 0xFF)]);
                    ms.Write(combined.ToArray());
                    wrote = true;
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

    private static byte[] FindSegmentWithMarker(byte[] data, byte marker)
    {
        var i = 2;
        while (i < data.Length - 1)
        {
            var code = data[i + 1];
            if (code == 0xDA)
                break;
            var len = (data[i + 2] << 8) | data[i + 3];
            if (code == marker)
                return data[i..(i + 2 + len)];
            i += 2 + len;
        }

        throw new InvalidOperationException($"marker 0x{marker:X2} not found");
    }

    private static byte[] InsertBeforeNthScan(byte[] data, byte[] segment, int scanIndex)
    {
        var scan = 0;
        for (var i = 0; i < data.Length - 1; i++)
        {
            if (data[i] == 0xFF && data[i + 1] == 0xDA)
            {
                if (scan == scanIndex)
                {
                    using var ms = new MemoryStream();
                    ms.Write(data, 0, i);
                    ms.Write(segment);
                    ms.Write(data, i, data.Length - i);
                    return ms.ToArray();
                }

                scan++;
            }
        }

        throw new InvalidOperationException($"scan {scanIndex} not found");
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

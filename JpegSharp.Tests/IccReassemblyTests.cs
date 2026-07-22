using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class IccReassemblyTests
{
    [Fact]
    public void IccChunks_OutOfOrderInStream_ReassembleBySequenceNumber()
    {
        // A profile large enough to span several APP2 segments.
        var icc = new byte[180_000];
        for (var i = 0; i < icc.Length; i++)
            icc[i] = (byte)((i * 31 + 7) & 0xFF);

        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = new JpegMetadata { IccProfile = icc } });

        var shuffled = ReverseApp2Order(bytes);
        Assert.NotEqual(bytes, shuffled); // order actually changed

        var decoded = Jpeg.Decode(shuffled);
        // Reassembly follows the ICC sequence bytes, not the file order, so it still matches.
        Assert.Equal(icc, decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void SingleChunkIcc_HasCorrectSequenceHeader()
    {
        var icc = new byte[100];
        for (var i = 0; i < icc.Length; i++)
            icc[i] = (byte)i;
        var image = JpegImage.CreateGrayscale(8, 8, new byte[64]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = new JpegMetadata { IccProfile = icc } });

        var app2 = FindApp2Segments(bytes);
        var seg = Assert.Single(app2);
        // Payload: "ICC_PROFILE\0" (12) + seq (1) + count (1) + data.
        Assert.Equal(1, seg[12]); // sequence 1
        Assert.Equal(1, seg[13]); // count 1
        Assert.Equal(icc.Length, seg.Length - 14);
    }

    private static byte[] ReverseApp2Order(byte[] data)
    {
        // Collect the byte ranges of each APP2 segment (marker + length + payload).
        var ranges = new List<(int Start, int Length)>();
        var i = 2;
        while (i < data.Length - 1)
        {
            if (data[i] != 0xFF)
            {
                i++;
                continue;
            }

            var code = data[i + 1];
            if (code == 0xDA || code == 0xD9)
                break;
            if (code is 0xD8 or (>= 0xD0 and <= 0xD7) or 0x00 or 0xFF)
            {
                i += 2;
                continue;
            }

            var len = (data[i + 2] << 8) | data[i + 3];
            var total = 2 + len;
            if (code == 0xE2)
                ranges.Add((i, total));
            i += total;
        }

        if (ranges.Count < 2)
            return data;

        // Rebuild: replace the APP2 block region with the segments in reverse order.
        using var ms = new MemoryStream();
        var first = ranges[0].Start;
        var last = ranges[^1].Start + ranges[^1].Length;
        ms.Write(data, 0, first);
        for (var r = ranges.Count - 1; r >= 0; r--)
            ms.Write(data, ranges[r].Start, ranges[r].Length);
        ms.Write(data, last, data.Length - last);
        return ms.ToArray();
    }

    private static List<byte[]> FindApp2Segments(byte[] data)
    {
        var result = new List<byte[]>();
        var i = 2;
        while (i < data.Length - 1)
        {
            if (data[i] != 0xFF)
            {
                i++;
                continue;
            }

            var code = data[i + 1];
            if (code == 0xDA || code == 0xD9)
                break;
            if (code is 0xD8 or (>= 0xD0 and <= 0xD7) or 0x00 or 0xFF)
            {
                i += 2;
                continue;
            }

            var len = (data[i + 2] << 8) | data[i + 3];
            if (code == 0xE2)
                result.Add(data[(i + 4)..(i + 2 + len)]);
            i += 2 + len;
        }

        return result;
    }
}

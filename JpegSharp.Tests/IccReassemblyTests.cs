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

    private static readonly byte[] IccIdentifier = "ICC_PROFILE\0"u8.ToArray();

    // Builds a raw APP2 (0xFFE2) marker segment carrying one ICC_PROFILE chunk.
    private static byte[] IccApp2Segment(int seq, int count, byte[] data)
    {
        var payload = new byte[14 + data.Length];
        IccIdentifier.CopyTo(payload, 0);
        payload[12] = (byte)seq;
        payload[13] = (byte)count;
        data.CopyTo(payload, 14);

        var length = payload.Length + 2; // JPEG segment length includes the two length bytes
        var segment = new byte[4 + payload.Length];
        segment[0] = 0xFF;
        segment[1] = 0xE2;
        segment[2] = (byte)(length >> 8);
        segment[3] = (byte)(length & 0xFF);
        payload.CopyTo(segment, 4);
        return segment;
    }

    // Splices hand-crafted marker segments in immediately after the SOI (FFD8) of a real JPEG.
    private static byte[] InsertAfterSoi(byte[] jpeg, params byte[][] segments)
    {
        using var ms = new MemoryStream();
        ms.Write(jpeg, 0, 2); // SOI
        foreach (var segment in segments)
            ms.Write(segment, 0, segment.Length);
        ms.Write(jpeg, 2, jpeg.Length - 2);
        return ms.ToArray();
    }

    private static byte[] BaseJpeg() =>
        Jpeg.Encode(JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]));

    private static byte[] Range(int start, int length)
    {
        var data = new byte[length];
        for (var i = 0; i < length; i++)
            data[i] = (byte)(start + i);
        return data;
    }

    [Fact]
    public void Icc_ValidMultiChunk_ReassemblesInOrder()
    {
        var c1 = Range(0, 10);
        var c2 = Range(10, 10);
        var c3 = Range(20, 10);
        var expected = new byte[30];
        c1.CopyTo(expected, 0);
        c2.CopyTo(expected, 10);
        c3.CopyTo(expected, 20);

        // Deliver the chunks OUT of segment order (3, 1, 2).
        var jpeg = InsertAfterSoi(
            BaseJpeg(),
            IccApp2Segment(3, 3, c3),
            IccApp2Segment(1, 3, c1),
            IccApp2Segment(2, 3, c2));

        var decoded = Jpeg.Decode(jpeg);
        Assert.Equal(expected, decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void Icc_DuplicateChunk_Tolerated()
    {
        var c1 = Range(0, 8);
        var c2 = Range(8, 8);
        var expected = new byte[16];
        c1.CopyTo(expected, 0);
        c2.CopyTo(expected, 8);

        // seq 2 appears twice with IDENTICAL data; must dedup, not double.
        var jpeg = InsertAfterSoi(
            BaseJpeg(),
            IccApp2Segment(1, 2, c1),
            IccApp2Segment(2, 2, c2),
            IccApp2Segment(2, 2, c2));

        var decoded = Jpeg.Decode(jpeg);
        Assert.Equal(expected, decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void Icc_ConflictingDuplicate_DroppedNotCorrupt()
    {
        var c1 = Range(0, 8);
        var c2a = Range(8, 8);
        var c2b = Range(100, 8); // same seq, DIFFERENT data

        var jpeg = InsertAfterSoi(
            BaseJpeg(),
            IccApp2Segment(1, 2, c1),
            IccApp2Segment(2, 2, c2a),
            IccApp2Segment(2, 2, c2b));

        var decoded = Jpeg.Decode(jpeg);
        Assert.Null(decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void Icc_MissingChunk_DroppedNotCorrupt()
    {
        var c1 = Range(0, 8);
        var c3 = Range(16, 8);

        // count=3 but chunk 2 is absent (gap).
        var jpeg = InsertAfterSoi(
            BaseJpeg(),
            IccApp2Segment(1, 3, c1),
            IccApp2Segment(3, 3, c3));

        var decoded = Jpeg.Decode(jpeg);
        Assert.Null(decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void Icc_SeqZeroOrOutOfRange_Dropped()
    {
        var zeroSeq = InsertAfterSoi(
            BaseJpeg(),
            IccApp2Segment(0, 1, Range(0, 8)));
        Assert.Null(Jpeg.Decode(zeroSeq).Metadata!.IccProfile);

        // seq greater than the declared count.
        var outOfRange = InsertAfterSoi(
            BaseJpeg(),
            IccApp2Segment(1, 2, Range(0, 8)),
            IccApp2Segment(3, 2, Range(8, 8)));
        Assert.Null(Jpeg.Decode(outOfRange).Metadata!.IccProfile);
    }

    [Fact]
    public void Icc_InconsistentCount_Dropped()
    {
        // Chunks disagree on the total count.
        var jpeg = InsertAfterSoi(
            BaseJpeg(),
            IccApp2Segment(1, 2, Range(0, 8)),
            IccApp2Segment(2, 3, Range(8, 8)));

        var decoded = Jpeg.Decode(jpeg);
        Assert.Null(decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void Icc_EncoderRoundTrip_Unchanged()
    {
        // Large enough to force the encoder to emit multiple APP2 chunks.
        var icc = new byte[200_000];
        new Random(11).NextBytes(icc);
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = new JpegMetadata { IccProfile = icc } });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(icc, decoded.Metadata!.IccProfile);
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

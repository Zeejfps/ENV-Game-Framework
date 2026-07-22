using System.Collections.Generic;
using System.Linq;
using JpegSharp.Api;
using JpegSharp.Bitstream;
using JpegSharp.Coding;
using JpegSharp.Huffman;
using JpegSharp.Markers;
using JpegSharp.Quantization;
using JpegSharp.Transforms;
using Xunit;

namespace JpegSharp.Tests;

// Covers FOLLOWUP-ORDER: (A) header APPn/COM segment order is preserved on decode->re-encode, and
// (B) COM segments that appear between scans are captured rather than dropped.
public class MetadataOrderingTests
{
    [Fact]
    public void Metadata_UnusualSegmentOrder_PreservedOnReencode()
    {
        // Header order: an unrecognized APP5 BEFORE Exif, and Exif BEFORE JFIF — none of which is
        // the encoder's fixed order (JFIF, Exif, ...). Re-encode must replay this exact order.
        var app5 = Concat("Custom\0"u8.ToArray(), new byte[] { 1, 2, 3, 4 });
        var exif = Concat("Exif\0\0"u8.ToArray(), new byte[] { 0x49, 0x49, 0x2A, 0x00, 9, 8, 7 });
        var jfif = DefaultJfifPayload();

        var original = SpliceHeaderSegments(EncodePlainGray(), new (byte, byte[])[]
        {
            (JpegMarkers.App0 + 5, app5), // APP5
            (JpegMarkers.App1, exif),
            (JpegMarkers.App0, jfif),
        });

        var decoded = Jpeg.Decode(original);
        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Metadata = decoded.Metadata });

        var expected = new byte[] { JpegMarkers.App0 + 5, JpegMarkers.App1, JpegMarkers.App0 };
        Assert.Equal(expected, MetadataMarkers(original));
        Assert.Equal(expected, MetadataMarkers(reencoded));
    }

    [Fact]
    public void Metadata_TypicalOrder_ByteIdentical()
    {
        // A normally-ordered file (JFIF, Exif, ICC, COM) whose order matches the encoder's fixed
        // order must re-encode byte-for-byte identically after a decode round-trip.
        var icc = new byte[500];
        for (var i = 0; i < icc.Length; i++)
            icc[i] = (byte)(i * 7);
        var metadata = new JpegMetadata
        {
            Density = new JfifDensity(JpegDensityUnit.DotsPerInch, 72, 72),
            Exif = new byte[] { 0x49, 0x49, 0x2A, 0x00, 1, 2, 3, 4 },
            IccProfile = icc,
        };
        metadata.Comments.Add("First");
        metadata.Comments.Add("Second");

        var image = JpegImage.CreateRgb(24, 24, Gradient(24, 24, 3));
        var options = new JpegEncoderOptions { Quality = 85, Metadata = metadata };
        var original = Jpeg.Encode(image, options);

        var decoded = Jpeg.Decode(original);
        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Quality = 85, Metadata = decoded.Metadata });

        // Decode is lossy, so the entropy differs; the metadata region (SOI through the last
        // APPn/COM, before DQT) must be byte-for-byte identical.
        Assert.Equal(MetadataRegion(original), MetadataRegion(reencoded));
    }

    [Fact]
    public void Metadata_UserConstructed_UsesFixedOrder()
    {
        // Fresh metadata (empty manifest) must emit in the encoder's fixed order:
        // JFIF (APP0), Exif (APP1), ICC (APP2), COM, then preserved application segments.
        var metadata = new JpegMetadata
        {
            Exif = new byte[] { 1, 2, 3, 4 },
            IccProfile = new byte[300],
        };
        metadata.Comments.Add("hello");
        metadata.ApplicationSegments.Add(new JpegApplicationSegment(JpegMarkers.App0 + 7, new byte[] { 9, 9 }));

        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var expected = new byte[]
        {
            JpegMarkers.App0,      // JFIF
            JpegMarkers.App1,      // Exif
            JpegMarkers.App2,      // ICC
            JpegMarkers.Comment,   // COM
            JpegMarkers.App0 + 7,  // preserved APP7
        };
        Assert.Equal(expected, MetadataMarkers(bytes));
    }

    [Fact]
    public void Com_BetweenScans_NotDropped_Baseline()
    {
        // Multi-scan baseline stream with a COM inserted AFTER the first scan's entropy data,
        // between scans. It must be captured (not dropped) and surface in CommentBytes.
        var between = new byte[] { 0x42, 0x45, 0x54, 0x57 }; // "BETW"
        var bytes = BuildMultiScanRgbWithBetweenScanComment(24, 16, between);

        var decoded = Jpeg.Decode(bytes);
        Assert.NotNull(decoded.Metadata);
        Assert.Contains(decoded.Metadata!.CommentBytes, c => c.SequenceEqual(between));
    }

    [Fact]
    public void Com_BetweenScans_NotDropped_Progressive()
    {
        // Progressive stream (multiple scans) with a COM spliced in before the second scan header.
        var between = new byte[] { 0x50, 0x52, 0x4F, 0x47 }; // "PROG"
        var progressive = Jpeg.Encode(
            JpegImage.CreateRgb(24, 24, Gradient(24, 24, 5)),
            new JpegEncoderOptions { Quality = 80, Progressive = true });
        var spliced = SpliceCommentBeforeSecondScan(progressive, between);

        var decoded = Jpeg.Decode(spliced);
        Assert.Contains(decoded.Metadata!.CommentBytes, c => c.SequenceEqual(between));
    }

    [Fact]
    public void Metadata_IccExifComAppn_EmittedExactlyOnce()
    {
        var metadata = new JpegMetadata
        {
            Exif = new byte[] { 0x49, 0x49, 0x2A, 0x00, 5, 6, 7, 8 },
            IccProfile = new byte[400],
        };
        metadata.Comments.Add("only-comment");
        metadata.ApplicationSegments.Add(new JpegApplicationSegment(JpegMarkers.App0 + 7, new byte[] { 3, 1, 4 }));

        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var original = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(original);
        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Metadata = decoded.Metadata });

        var markers = MetadataMarkers(reencoded);
        Assert.Equal(1, markers.Count(m => m == JpegMarkers.App0));     // JFIF
        Assert.Equal(1, markers.Count(m => m == JpegMarkers.App1));     // Exif
        Assert.Equal(1, markers.Count(m => m == JpegMarkers.App2));     // ICC (single chunk)
        Assert.Equal(1, markers.Count(m => m == JpegMarkers.Comment));  // COM
        Assert.Equal(1, markers.Count(m => m == JpegMarkers.App0 + 7)); // preserved APP7
    }

    [Fact]
    public void Metadata_DecodeAddComment_Reencode_KeepsBoth()
    {
        // Source with exactly one COM => non-empty manifest. Decode, append a second comment, and
        // re-encode: both comments must survive, the original first and the added one appended.
        var metadata = new JpegMetadata();
        metadata.Comments.Add("orig");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var original = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(original);
        Assert.NotEmpty(decoded.Metadata!.HeaderSegmentOrder);
        Assert.Single(decoded.Metadata.CommentBytes);
        decoded.Metadata.CommentBytes.Add("added"u8.ToArray());

        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Metadata = decoded.Metadata });

        Assert.Equal(2, MetadataMarkers(reencoded).Count(m => m == JpegMarkers.Comment));
        var roundTrip = Jpeg.Decode(reencoded);
        Assert.Equal(2, roundTrip.Metadata!.CommentBytes.Count);
        Assert.True(roundTrip.Metadata.CommentBytes[0].SequenceEqual("orig"u8.ToArray()));
        Assert.True(roundTrip.Metadata.CommentBytes[1].SequenceEqual("added"u8.ToArray()));
    }

    [Fact]
    public void Metadata_DecodeAddExif_Reencode_EmitsExif()
    {
        // Source WITHOUT Exif but with a COM => non-empty manifest, no Exif ref. Setting Exif after
        // decode must still be emitted via the tail.
        var metadata = new JpegMetadata();
        metadata.Comments.Add("no-exif");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var original = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(original);
        Assert.NotEmpty(decoded.Metadata!.HeaderSegmentOrder);
        Assert.Null(decoded.Metadata.Exif);
        decoded.Metadata.Exif = new byte[] { 0x49, 0x49, 0x2A, 0x00, 1, 2, 3, 4 };

        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Metadata = decoded.Metadata });

        var roundTrip = Jpeg.Decode(reencoded);
        Assert.NotNull(roundTrip.Metadata!.Exif);
        Assert.Equal(1, MetadataMarkers(reencoded).Count(m => m == JpegMarkers.App1));
    }

    [Fact]
    public void Metadata_DecodeAddIcc_Reencode_EmitsIcc()
    {
        // Source WITHOUT ICC => non-empty manifest, no ICC ref. Setting IccProfile after decode must
        // be emitted via the tail and reassemble to the same bytes.
        var metadata = new JpegMetadata();
        metadata.Comments.Add("no-icc");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var original = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(original);
        Assert.NotEmpty(decoded.Metadata!.HeaderSegmentOrder);
        Assert.Null(decoded.Metadata.IccProfile);
        var icc = new byte[300];
        for (var i = 0; i < icc.Length; i++)
            icc[i] = (byte)(i * 5);
        decoded.Metadata.IccProfile = icc;

        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Metadata = decoded.Metadata });

        var roundTrip = Jpeg.Decode(reencoded);
        Assert.NotNull(roundTrip.Metadata!.IccProfile);
        Assert.True(roundTrip.Metadata.IccProfile!.SequenceEqual(icc));
    }

    [Fact]
    public void Metadata_DecodeAddAppSegment_Reencode_EmitsIt()
    {
        // Source with no preserved application segments => non-empty manifest, no App ref. Appending
        // one after decode must be emitted via the tail exactly once.
        var metadata = new JpegMetadata();
        metadata.Comments.Add("base");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var original = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(original);
        Assert.NotEmpty(decoded.Metadata!.HeaderSegmentOrder);
        Assert.Empty(decoded.Metadata.ApplicationSegments);
        decoded.Metadata.ApplicationSegments.Add(new JpegApplicationSegment(JpegMarkers.App0 + 7, new byte[] { 1, 2, 3 }));

        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Metadata = decoded.Metadata });

        Assert.Equal(1, MetadataMarkers(reencoded).Count(m => m == JpegMarkers.App0 + 7));
    }

    [Fact]
    public void Metadata_PureRoundTrip_StillByteIdentical()
    {
        // Pure decode->encode with NO edits: the manifest covers everything, so the tail must be
        // empty and nothing double-emitted; the metadata region is byte-for-byte identical.
        var icc = new byte[500];
        for (var i = 0; i < icc.Length; i++)
            icc[i] = (byte)(i * 7);
        var metadata = new JpegMetadata
        {
            Density = new JfifDensity(JpegDensityUnit.DotsPerInch, 72, 72),
            Exif = new byte[] { 0x49, 0x49, 0x2A, 0x00, 1, 2, 3, 4 },
            IccProfile = icc,
        };
        metadata.Comments.Add("First");
        metadata.ApplicationSegments.Add(new JpegApplicationSegment(JpegMarkers.App0 + 7, new byte[] { 9, 9 }));

        var image = JpegImage.CreateRgb(24, 24, Gradient(24, 24, 3));
        var original = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Metadata = metadata });

        var decoded = Jpeg.Decode(original);
        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Quality = 85, Metadata = decoded.Metadata });

        Assert.Equal(MetadataRegion(original), MetadataRegion(reencoded));
    }

    // ----- helpers -----

    private static byte[] EncodePlainGray()
    {
        var gray = new byte[16 * 16];
        for (var i = 0; i < gray.Length; i++)
            gray[i] = (byte)((i * 3 + 2) & 0xFF);
        return Jpeg.Encode(JpegImage.CreateGrayscale(16, 16, gray), new JpegEncoderOptions { Quality = 80 });
    }

    // Returns the metadata region: SOI plus every header segment up to (but excluding) the first
    // structural marker (DQT/SOF/DHT/DRI/SOS).
    private static byte[] MetadataRegion(byte[] jpeg)
    {
        var pos = 2; // skip SOI
        while (pos + 1 < jpeg.Length)
        {
            var marker = jpeg[pos + 1];
            if (!JpegMarkers.IsAppMarker(marker) && marker != JpegMarkers.Comment)
                break;
            var length = (jpeg[pos + 2] << 8) | jpeg[pos + 3];
            pos += 2 + length;
        }

        return jpeg[..pos];
    }

    private static byte[] DefaultJfifPayload() => new byte[]
    {
        (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0x00,
        0x01, 0x01,
        0x00,             // unit None
        0x00, 0x01,       // x density 1
        0x00, 0x01,       // y density 1
        0x00, 0x00,       // no thumbnail
    };

    // Replaces the encoder-emitted APP0 (JFIF) that immediately follows SOI with a caller-provided
    // ordered list of header segments, then keeps the rest of the stream (DQT onward) intact.
    private static byte[] SpliceHeaderSegments(byte[] baseJpeg, IReadOnlyList<(byte Marker, byte[] Payload)> segments)
    {
        Assert.Equal(0xFF, baseJpeg[0]);
        Assert.Equal(JpegMarkers.StartOfImage, baseJpeg[1]);
        Assert.Equal(0xFF, baseJpeg[2]);
        Assert.Equal(JpegMarkers.App0, baseJpeg[3]); // encoder always writes JFIF first for grayscale
        var app0Length = (baseJpeg[4] << 8) | baseJpeg[5];
        var bodyStart = 4 + app0Length; // SOI(2) + marker(2) + length-value(includes its own 2 bytes)

        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteMarker(JpegMarkers.StartOfImage);
        foreach (var (marker, payload) in segments)
            writer.WriteSegment(marker, payload);
        ms.Write(baseJpeg, bodyStart, baseJpeg.Length - bodyStart);
        return ms.ToArray();
    }

    // Returns the sequence of APPn/COM marker codes appearing in the header, in file order, stopping
    // at the first SOS.
    private static List<byte> MetadataMarkers(byte[] jpeg)
    {
        var markers = new List<byte>();
        var pos = 2; // skip SOI
        while (pos + 1 < jpeg.Length)
        {
            if (jpeg[pos] != 0xFF)
                break;
            var marker = jpeg[pos + 1];
            pos += 2;
            if (marker == JpegMarkers.StartOfScan || marker == JpegMarkers.EndOfImage)
                break;
            if (!JpegMarkers.HasLengthField(marker))
                continue;
            var length = (jpeg[pos] << 8) | jpeg[pos + 1];
            if (JpegMarkers.IsAppMarker(marker) || marker == JpegMarkers.Comment)
                markers.Add(marker);
            pos += length;
        }

        return markers;
    }

    // Inserts a COM segment right before the second SOS marker found in the stream. Progressive scan
    // headers are real markers (FF DA with the following byte never 0x00), so scanning for them is safe.
    private static byte[] SpliceCommentBeforeSecondScan(byte[] jpeg, byte[] comment)
    {
        var scanCount = 0;
        for (var i = 2; i + 1 < jpeg.Length; i++)
        {
            if (jpeg[i] != 0xFF || jpeg[i + 1] != JpegMarkers.StartOfScan)
                continue;
            scanCount++;
            if (scanCount != 2)
                continue;

            using var ms = new MemoryStream();
            ms.Write(jpeg, 0, i);
            new MarkerWriter(ms).WriteSegment(JpegMarkers.Comment, comment);
            ms.Write(jpeg, i, jpeg.Length - i);
            return ms.ToArray();
        }

        throw new InvalidOperationException("Stream did not contain a second scan to splice before.");
    }

    private static byte[] Gradient(int w, int h, int seed)
    {
        var p = new byte[w * h * 3];
        for (var i = 0; i < w * h; i++)
        {
            p[i * 3] = (byte)((i + seed) & 0xFF);
            p[i * 3 + 1] = (byte)((i * 3 + seed) & 0xFF);
            p[i * 3 + 2] = (byte)((i * 5 + seed) & 0xFF);
        }

        return p;
    }

    // Builds a baseline SOF0 stream with three full-resolution direct-RGB components each in its own
    // non-interleaved scan, with a COM inserted between the first and second scans.
    private static byte[] BuildMultiScanRgbWithBetweenScanComment(int w, int h, byte[] betweenScanComment)
    {
        var quant = QuantizationTable.Luminance(90);
        var dc = StandardHuffmanTables.DcLuminance;
        var ac = StandardHuffmanTables.AcLuminance;
        var ids = new[] { (byte)'R', (byte)'G', (byte)'B' };
        var planes = new byte[3][];
        for (var ci = 0; ci < 3; ci++)
        {
            planes[ci] = new byte[w * h];
            for (var i = 0; i < w * h; i++)
                planes[ci][i] = (byte)((i * (ci + 2)) & 0xFF);
        }

        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteMarker(JpegMarkers.StartOfImage);

        Span<ushort> zig = stackalloc ushort[64];
        quant.CopyToZigZag(zig);
        Span<byte> dqt = stackalloc byte[1 + 64];
        dqt[0] = 0;
        for (var k = 0; k < 64; k++)
            dqt[1 + k] = (byte)zig[k];
        writer.WriteSegment(JpegMarkers.DefineQuantizationTables, dqt);

        Span<byte> sof = stackalloc byte[6 + 3 * 3];
        var p = 0;
        sof[p++] = 8;
        sof[p++] = (byte)(h >> 8); sof[p++] = (byte)h;
        sof[p++] = (byte)(w >> 8); sof[p++] = (byte)w;
        sof[p++] = 3;
        foreach (var id in ids)
        {
            sof[p++] = id;
            sof[p++] = 0x11;
            sof[p++] = 0;
        }

        writer.WriteSegment(JpegMarkers.StartOfFrameBaseline, sof[..p]);

        WriteDht(writer, 0, 0, dc);
        WriteDht(writer, 1, 0, ac);

        var blocksWide = (w + 7) / 8;
        var blocksHigh = (h + 7) / 8;

        Span<byte> sos = stackalloc byte[1 + 2 + 3];
        Span<double> samples = stackalloc double[64];
        Span<double> coeffs = stackalloc double[64];
        Span<short> quantized = stackalloc short[64];
        Span<short> block = stackalloc short[64];
        for (var ci = 0; ci < 3; ci++)
        {
            sos[0] = 1;
            sos[1] = ids[ci];
            sos[2] = 0x00;
            sos[3] = 0;
            sos[4] = 63;
            sos[5] = 0;
            writer.WriteSegment(JpegMarkers.StartOfScan, sos);

            var bitWriter = new BitWriter(ms);
            var predictor = 0;
            for (var by = 0; by < blocksHigh; by++)
            {
                for (var bx = 0; bx < blocksWide; bx++)
                {
                    ExtractBlock(planes[ci], w, h, bx * 8, by * 8, samples);
                    FastDct.Forward(samples, coeffs);
                    Quantizer.Quantize(coeffs, quant.AsSpan(), quantized);
                    ZigZag.FromNatural(quantized, block);
                    predictor = BlockScanCoder.EncodeBlock(bitWriter, block, predictor, dc, ac);
                }
            }

            bitWriter.Flush();

            // Inject the between-scan COM right after the first scan's entropy data.
            if (ci == 0)
                writer.WriteSegment(JpegMarkers.Comment, betweenScanComment);
        }

        writer.WriteMarker(JpegMarkers.EndOfImage);
        return ms.ToArray();
    }

    private static void ExtractBlock(byte[] plane, int w, int h, int x0, int y0, Span<double> samples)
    {
        for (var yy = 0; yy < 8; yy++)
        {
            var sy = Math.Min(y0 + yy, h - 1);
            for (var xx = 0; xx < 8; xx++)
            {
                var sx = Math.Min(x0 + xx, w - 1);
                samples[yy * 8 + xx] = plane[sy * w + sx] - 128;
            }
        }
    }

    private static void WriteDht(MarkerWriter writer, int tableClass, int tableId, HuffmanTable table)
    {
        var counts = table.Counts;
        var symbols = table.Symbols;
        Span<byte> payload = stackalloc byte[1 + 16 + 256];
        payload[0] = (byte)((tableClass << 4) | tableId);
        for (var i = 0; i < 16; i++)
            payload[1 + i] = counts[i];
        for (var i = 0; i < symbols.Length; i++)
            payload[17 + i] = symbols[i];
        writer.WriteSegment(JpegMarkers.DefineHuffmanTables, payload[..(17 + symbols.Length)]);
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        a.CopyTo(result, 0);
        b.CopyTo(result, a.Length);
        return result;
    }
}

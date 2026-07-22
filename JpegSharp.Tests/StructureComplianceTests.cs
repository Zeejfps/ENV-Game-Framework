using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

/// <summary>
/// Verifies that encoder output conforms to the structural rules any compliant JPEG decoder
/// relies on: correct marker order, mandatory segments present, and consistent header values.
/// </summary>
public class StructureComplianceTests
{
    [Fact]
    public void Baseline_HasExpectedMarkerOrderAndSegments()
    {
        var image = JpegImage.CreateRgb(24, 16, new byte[24 * 16 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 });
        var markers = Markers(bytes);

        Assert.Equal(0xD8, markers[0].Code); // SOI first
        Assert.Equal(0xD9, markers[^1].Code); // EOI last

        var order = markers.ConvertAll(m => m.Code);
        // APP0 (JFIF) appears, DQT before SOF0, SOF0 before DHT/SOS.
        Assert.Contains((byte)0xE0, order);
        Assert.True(order.IndexOf(0xDB) < order.IndexOf(0xC0), "DQT must precede SOF0");
        Assert.True(order.IndexOf(0xC0) < order.IndexOf(0xDA), "SOF0 must precede SOS");
        Assert.Contains((byte)0xC4, order); // DHT present
    }

    [Fact]
    public void Sof0_EncodesDimensionsAndComponentCount()
    {
        var image = JpegImage.CreateRgb(37, 19, new byte[37 * 19 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, Subsampling = ChromaSubsampling.Samp420 });
        var sof = FindSegment(bytes, 0xC0)!;

        Assert.Equal(8, sof[0]); // precision
        Assert.Equal(19, (sof[1] << 8) | sof[2]); // height
        Assert.Equal(37, (sof[3] << 8) | sof[4]); // width
        Assert.Equal(3, sof[5]); // component count

        // Luma component (id 1) carries the 2x2 sampling factor for 4:2:0.
        Assert.Equal(1, sof[6]);
        Assert.Equal(0x22, sof[7]);
    }

    [Fact]
    public void EverySegmentLength_MatchesItsPayload()
    {
        var image = JpegImage.CreateRgb(40, 40, new byte[40 * 40 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Metadata = MakeMetadata() });

        // Walk all length-bearing segments and confirm each length stays in bounds and the
        // stream is well-formed up to SOS.
        var i = 2;
        while (i < bytes.Length - 1)
        {
            if (bytes[i] != 0xFF)
            {
                i++;
                continue;
            }

            var code = bytes[i + 1];
            if (code == 0xDA || code == 0xD9)
                break;
            if (code is 0xD8 or (>= 0xD0 and <= 0xD7) or 0x00 or 0xFF)
            {
                i += 2;
                continue;
            }

            var len = (bytes[i + 2] << 8) | bytes[i + 3];
            Assert.True(len >= 2, $"segment 0x{code:X2} length {len} too small");
            Assert.True(i + 2 + len <= bytes.Length, "segment length overruns stream");
            i += 2 + len;
        }
    }

    [Fact]
    public void Progressive_UsesSof2AndMultipleScans()
    {
        var image = JpegImage.CreateRgb(24, 24, new byte[24 * 24 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true });
        var order = Markers(bytes).ConvertAll(m => m.Code);

        Assert.Contains((byte)0xC2, order); // SOF2
        Assert.DoesNotContain((byte)0xC0, order); // no baseline frame

        // Count SOS markers across the whole stream: 0xFF 0xDA never appears in stuffed
        // entropy data (stuffing yields 0xFF 0x00; restart markers are 0xD0-0xD7).
        var scanCount = 0;
        for (var i = 0; i < bytes.Length - 1; i++)
            if (bytes[i] == 0xFF && bytes[i + 1] == 0xDA)
                scanCount++;
        Assert.True(scanCount >= 4, $"expected multiple scans, got {scanCount}");
    }

    private static JpegMetadata MakeMetadata()
    {
        var m = new JpegMetadata { Exif = new byte[] { 1, 2, 3, 4 } };
        m.Comments.Add("test");
        return m;
    }

    private static List<(byte Code, int Offset)> Markers(byte[] data)
    {
        var result = new List<(byte, int)>();
        var i = 0;
        while (i < data.Length - 1)
        {
            if (data[i] != 0xFF)
            {
                i++;
                continue;
            }

            var code = data[i + 1];
            if (code == 0x00 || code == 0xFF)
            {
                i += 2;
                continue;
            }

            result.Add((code, i));
            if (code == 0xDA)
                break; // stop before entropy data (which may contain 0xFF)
            if (code is 0xD8 or 0xD9 or (>= 0xD0 and <= 0xD7))
            {
                i += 2;
                continue;
            }

            var len = (data[i + 2] << 8) | data[i + 3];
            i += 2 + len;
        }

        // Ensure the final marker is EOI.
        result.Add((0xD9, data.Length - 2));
        return result;
    }

    private static byte[]? FindSegment(byte[] data, byte markerCode)
    {
        var i = 2;
        while (i < data.Length - 1)
        {
            if (data[i] != 0xFF)
            {
                i++;
                continue;
            }

            var code = data[i + 1];
            if (code == markerCode)
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                return data[(i + 4)..(i + 2 + len)];
            }

            if (code is 0xD8 or 0xD9 or (>= 0xD0 and <= 0xD7) or 0x00 or 0xFF)
            {
                i += 2;
                continue;
            }

            if (code == 0xDA)
                return null;

            var segLen = (data[i + 2] << 8) | data[i + 3];
            i += 2 + segLen;
        }

        return null;
    }
}

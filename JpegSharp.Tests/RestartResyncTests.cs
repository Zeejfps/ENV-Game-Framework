using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using Xunit;

namespace JpegSharp.Tests;

public class RestartResyncTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Restart_ValidSequence_DecodesIdentically(int interval)
    {
        var pixels = Gradient(40, 40);
        var image = JpegImage.CreateGrayscale(40, 40, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RestartInterval = interval });

        var lenient = Jpeg.Decode(bytes, new JpegDecoderOptions { StrictRestartMarkers = false });
        var strict = Jpeg.Decode(bytes, new JpegDecoderOptions { StrictRestartMarkers = true });

        Assert.Equal(lenient.PixelData, strict.PixelData);
        AssertClose(pixels, lenient.PixelData, 4.0, 40);
        AssertClose(pixels, strict.PixelData, 4.0, 40);
    }

    [Fact]
    public void Restart_MisorderedMarker_LenientResyncs_StrictThrows()
    {
        var image = JpegImage.CreateGrayscale(40, 40, Gradient(40, 40));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RestartInterval = 2 });

        var rst = FindRestartMarkers(bytes);
        Assert.NotEmpty(rst);

        // The first restart marker must be RST0; replace it with a different (out-of-sequence) RSTn.
        Assert.Equal(0xD0, bytes[rst[0] + 1]);
        bytes[rst[0] + 1] = 0xD2;

        var lenient = Jpeg.Decode(bytes, new JpegDecoderOptions { StrictRestartMarkers = false });
        Assert.Equal(40 * 40, lenient.PixelData.Length);

        Assert.Throws<JpegCorruptException>(
            () => Jpeg.Decode(bytes, new JpegDecoderOptions { StrictRestartMarkers = true }));
    }

    [Fact]
    public void Restart_MissingMarker_LenientContinues_StrictThrows()
    {
        var image = JpegImage.CreateGrayscale(40, 40, Gradient(40, 40));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RestartInterval = 2 });
        var rst = FindRestartMarkers(bytes);
        Assert.NotEmpty(rst);

        // Removing a restart marker's two bytes preserves byte alignment of the following
        // interval, so a lenient decode can resync and finish; a strict decode must reject it.
        // The exact interval affected is stream-dependent, so pick the first removal a lenient
        // decode survives (the encoder is deterministic, so this selection is stable).
        byte[]? damaged = null;
        foreach (var pos in rst)
        {
            var candidate = RemoveBytes(bytes, pos, 2);
            var ex = Record.Exception(
                () => Jpeg.Decode(candidate, new JpegDecoderOptions { StrictRestartMarkers = false }));
            if (ex is null)
            {
                damaged = candidate;
                break;
            }
        }

        Assert.NotNull(damaged);

        var lenient = Jpeg.Decode(damaged!, new JpegDecoderOptions { StrictRestartMarkers = false });
        Assert.Equal(40 * 40, lenient.PixelData.Length);

        Assert.Throws<JpegCorruptException>(
            () => Jpeg.Decode(damaged!, new JpegDecoderOptions { StrictRestartMarkers = true }));
    }

    [Fact]
    public void Restart_BoundedResync_DoesNotSkipArbitraryData()
    {
        // Exactly on the marker: found and consumed.
        Assert.Equal(RestartResync.FoundExpected, Resync([0xFF, 0xD0], 0, false, out var p0));
        Assert.Equal(2, p0);

        // A couple of leftover entropy bytes are tolerated.
        Assert.Equal(RestartResync.FoundExpected, Resync([0x11, 0xFF, 0xD0], 0, false, out var p1));
        Assert.Equal(3, p1);

        // A wrong RSTn is a mismatch (lenient), consumed for resync.
        Assert.Equal(RestartResync.FoundMismatch, Resync([0xFF, 0xD3], 0, false, out var p2));
        Assert.Equal(2, p2);

        // A marker far behind arbitrary junk is NOT scanned to; it is reported missing and the
        // position is left where it started (no advance past the wrong bytes).
        byte[] far = [0x11, 0x22, 0x33, 0x44, 0x55, 0xFF, 0xD0];
        Assert.Equal(RestartResync.Missing, Resync(far, 0, false, out var p3));
        Assert.Equal(0, p3);

        // Strict rejects both the far/missing and the misordered cases.
        Assert.Throws<JpegCorruptException>(() => ResyncStrict(far, 0));
        Assert.Throws<JpegCorruptException>(() => ResyncStrict([0xFF, 0xD3], 0));
    }

    private static RestartResync Resync(byte[] data, int expected, bool strict, out int position)
    {
        var reader = new BitReader(data);
        var result = reader.SkipRestartMarker(expected, strict);
        position = reader.BytePosition;
        return result;
    }

    private static void ResyncStrict(byte[] data, int expected)
    {
        var reader = new BitReader(data);
        reader.SkipRestartMarker(expected, true);
    }

    private static List<int> FindRestartMarkers(byte[] data)
    {
        var start = FindScanStart(data);
        var list = new List<int>();
        for (var i = start; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] is >= 0xD0 and <= 0xD7)
                list.Add(i);
        return list;
    }

    private static int FindScanStart(byte[] data)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] == 0xDA)
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                return i + 2 + len;
            }
        return data.Length;
    }

    private static byte[] RemoveBytes(byte[] data, int offset, int count)
    {
        var result = new byte[data.Length - count];
        Array.Copy(data, 0, result, 0, offset);
        Array.Copy(data, offset + count, result, offset, data.Length - offset - count);
        return result;
    }

    private static void AssertClose(byte[] expected, byte[] actual, double meanTol, int maxTol)
    {
        Assert.Equal(expected.Length, actual.Length);
        long total = 0;
        var max = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            var d = Math.Abs(expected[i] - actual[i]);
            total += d;
            if (d > max)
                max = d;
        }

        Assert.True((double)total / expected.Length <= meanTol);
        Assert.True(max <= maxTol);
    }

    private static byte[] Gradient(int w, int h)
    {
        var data = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                data[y * w + x] = (byte)((x * 255 / Math.Max(1, w - 1) + y * 255 / Math.Max(1, h - 1)) / 2);
        return data;
    }
}

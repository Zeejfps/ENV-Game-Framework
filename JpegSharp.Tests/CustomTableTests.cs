using JpegSharp.Api;
using JpegSharp.Huffman;
using JpegSharp.Quantization;
using Xunit;

namespace JpegSharp.Tests;

public class CustomTableTests
{
    [Fact]
    public void CustomQuantTable_AllOnes_RoundTripsNearLossless()
    {
        var ones = new ushort[64];
        Array.Fill(ones, (ushort)1);
        var table = new QuantizationTable(ones);

        var pixels = Gradient(32, 32);
        var image = JpegImage.CreateGrayscale(32, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { LuminanceQuantizationTable = table });

        var decoded = Jpeg.Decode(bytes);
        // With a unit quantization table, only DCT rounding error remains.
        AssertClose(pixels, decoded.PixelData, 1.5, 6);
    }

    [Fact]
    public void CustomQuantTable_IsWrittenToDqt()
    {
        var values = new ushort[64];
        for (var i = 0; i < 64; i++)
            values[i] = (ushort)(i + 1);
        var table = new QuantizationTable(values);

        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { LuminanceQuantizationTable = table });

        // Find the DQT segment and check its zig-zag values match the custom table.
        var dqt = FindSegment(bytes, 0xDB);
        Assert.NotNull(dqt);
        Span<ushort> zig = stackalloc ushort[64];
        table.CopyToZigZag(zig);
        for (var i = 0; i < 64; i++)
            Assert.Equal((byte)zig[i], dqt![1 + i]); // skip the Pq/Tq byte
    }

    [Fact]
    public void CustomHuffmanTables_RoundTrip()
    {
        // Build complete custom tables (covering every symbol) via uniform frequencies.
        var freq = new int[256];
        Array.Fill(freq, 1);
        var dc = HuffmanTable.BuildOptimized(freq);
        var ac = HuffmanTable.BuildOptimized(freq);

        var pixels = Gradient(40, 40);
        var image = JpegImage.CreateGrayscale(40, 40, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions
        {
            Quality = 85,
            LuminanceDcHuffmanTable = dc,
            LuminanceAcHuffmanTable = ac,
        });

        var reference = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 }));
        var decoded = Jpeg.Decode(bytes);
        // Same coefficients, different entropy coding => identical reconstruction.
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void CustomHuffmanTable_IsWrittenToDht()
    {
        var freq = new int[256];
        Array.Fill(freq, 1);
        var dc = HuffmanTable.BuildOptimized(freq);

        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { LuminanceDcHuffmanTable = dc });

        var dht = FindSegment(bytes, 0xC4);
        Assert.NotNull(dht);
        // First DHT is the DC luma table: byte0 = class/id, bytes 1..16 = counts.
        Assert.Equal(0x00, dht![0]);
        for (var i = 0; i < 16; i++)
            Assert.Equal(dc.Counts[i], dht[1 + i]);
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
                return null; // reached scan data

            var segLen = (data[i + 2] << 8) | data[i + 3];
            i += 2 + segLen;
        }

        return null;
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

        Assert.True((double)total / expected.Length <= meanTol, $"mean {(double)total / expected.Length:F2} > {meanTol}");
        Assert.True(max <= maxTol, $"max {max} > {maxTol}");
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

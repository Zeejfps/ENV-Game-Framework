using JpegSharp.Api;
using JpegSharp.Quantization;
using Xunit;

namespace JpegSharp.Tests;

public class HighPrecisionQuantTests
{
    [Fact]
    public void QuantValueAbove255_IsWrittenAs16BitAndRoundTrips()
    {
        var values = new ushort[64];
        Array.Fill(values, (ushort)8);
        values[0] = 400;  // exceeds 8-bit range
        values[5] = 1000; // well beyond a byte
        var table = new QuantizationTable(values);

        var pixels = new byte[32 * 32];
        Array.Fill(pixels, (byte)128); // flat image
        var image = JpegImage.CreateGrayscale(32, 32, pixels);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { LuminanceQuantizationTable = table });

        // The DQT must be written at 16-bit precision (Pq=1) with the true values.
        var dqt = FindSegment(bytes, 0xDB)!;
        Assert.Equal(0x10, dqt[0] & 0xF0); // Pq = 1

        Span<ushort> zig = stackalloc ushort[64];
        table.CopyToZigZag(zig);
        for (var k = 0; k < 64; k++)
        {
            var stored = (ushort)((dqt[1 + 2 * k] << 8) | dqt[1 + 2 * k + 1]);
            Assert.Equal(zig[k], stored);
        }

        // And the reconstruction stays consistent (encoder and decoder use the same value).
        var decoded = Jpeg.Decode(bytes);
        // A flat 128 image quantized by DC=400 reconstructs near 128 (coarse but consistent).
        // The old truncation bug (400 -> 144) would push the value far below.
        Assert.InRange(decoded.PixelData[0], 100, 160);
    }

    [Fact]
    public void EightBitQuantTable_StillUses8BitPrecision()
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 75 });
        var dqt = FindSegment(bytes, 0xDB)!;
        Assert.Equal(0x00, dqt[0] & 0xF0); // Pq = 0 (8-bit) for standard tables
        Assert.Equal(1 + 64, dqt.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(95)]
    public void Encode_8bit_QuantTables_Unchanged(int quality)
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = quality });
        var dqt = FindSegment(bytes, 0xDB)!;

        // 8-bit encodes always emit an 8-bit (Pq=0) DQT of exactly 1 + 64 bytes.
        Assert.Equal(0x00, dqt[0] & 0xF0);
        Assert.Equal(1 + 64, dqt.Length);

        // Emitted steps match the default-precision luminance table (byte-identical regression).
        var expected = QuantizationTable.Luminance(quality);
        Span<ushort> zig = stackalloc ushort[64];
        expected.CopyToZigZag(zig);
        for (var k = 0; k < 64; k++)
            Assert.Equal((byte)zig[k], dqt[1 + k]);
    }

    [Fact]
    public void Encode16_LowQuality_UsesWideQuantSteps()
    {
        var image = JpegImage16.CreateGrayscale(16, 16, 12, Gradient12(16, 16));
        var bytes = Jpeg.Encode16(image, new JpegEncoderOptions { Quality = 1 });

        var dqt = FindSegment(bytes, 0xDB)!;
        Assert.Equal(0x10, dqt[0] & 0xF0);   // Pq = 1 (16-bit table)
        Assert.Equal(1 + 128, dqt.Length);

        var sawAbove255 = false;
        for (var k = 0; k < 64; k++)
        {
            var stored = (ushort)((dqt[1 + 2 * k] << 8) | dqt[1 + 2 * k + 1]);
            if (stored > 255)
                sawAbove255 = true;
        }

        Assert.True(sawAbove255, "12-bit low-quality DQT must carry at least one step > 255.");

        // The wide table still round-trips.
        var decoded = Jpeg.Decode16(bytes);
        Assert.Equal(12, decoded.Precision);
        Assert.Equal(16, decoded.Width);
    }

    private static ushort[] Gradient12(int w, int h)
    {
        var max = (1 << 12) - 1;
        var data = new ushort[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var t = (x + y) / (double)(w + h);
                data[y * w + x] = (ushort)Math.Clamp((int)(t * max), 0, max);
            }
        return data;
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

using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class CorruptionTests
{
    [Fact]
    public void EmptyInput_ThrowsFormatException()
        => Assert.Throws<JpegFormatException>(() => Jpeg.Decode(Array.Empty<byte>()));

    [Fact]
    public void RandomBytes_ThrowsFormatException()
    {
        var junk = new byte[64];
        new Random(1).NextBytes(junk);
        junk[0] = 0x12; // ensure it does not start with a marker prefix
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(junk));
    }

    [Fact]
    public void MissingSoi_ThrowsFormatException()
        => Assert.Throws<JpegFormatException>(() => Jpeg.Decode([0xFF, 0xD9]));

    [Fact]
    public void TruncatedAfterSoi_ThrowsFormatException()
        => Assert.Throws<JpegFormatException>(() => Jpeg.Decode([0xFF, 0xD8]));

    [Fact]
    public void TruncatedSegment_ThrowsFormatException()
    {
        // APP0 declaring a 100-byte length but no payload.
        byte[] data = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x64];
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(data));
    }

    [Fact]
    public void OversubscribedHuffmanTable_ThrowsFormatException()
    {
        // SOI + DHT with 3 codes of length 1 (impossible).
        var counts = new byte[16];
        counts[0] = 3;
        var payload = new byte[1 + 16 + 3];
        payload[0] = 0x00; // class 0, id 0
        counts.CopyTo(payload, 1);
        payload[17] = 1;
        payload[18] = 2;
        payload[19] = 3;

        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, 0xC4]);
        var len = payload.Length + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        ms.Write(payload);

        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(ms.ToArray()));
    }

    [Fact]
    public void ZeroDimensions_ThrowsFormatException()
    {
        // SOI + SOF0 with height 0.
        byte[] sof = [8, 0x00, 0x00, 0x00, 0x08, 1, 1, 0x11, 0];
        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, 0xC0]);
        var len = sof.Length + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        ms.Write(sof);

        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(ms.ToArray()));
    }

    [Fact]
    public void InvalidComponentSamplingFactor_ThrowsFormatException()
    {
        // SOF0 with sampling factor byte 0x00 (H=0, V=0).
        byte[] sof = [8, 0x00, 0x08, 0x00, 0x08, 1, 1, 0x00, 0];
        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, 0xC0]);
        var len = sof.Length + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        ms.Write(sof);

        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(ms.ToArray()));
    }

    [Fact]
    public void ScanReferencingMissingTable_ThrowsFormatException()
    {
        // A structurally valid stream but with the DHT segments removed.
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image);
        var stripped = RemoveMarkers(bytes, 0xC4); // drop all DHT segments
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(stripped));
    }

    [Fact]
    public void TruncatedStream_NeverThrowsNonJpegException()
    {
        var image = JpegImage.CreateRgb(24, 24, ColorGradient(24, 24));
        var full = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 });

        for (var length = 0; length < full.Length; length += 3)
        {
            var prefix = full[..length];
            try
            {
                Jpeg.Decode(prefix);
            }
            catch (JpegException)
            {
                // Expected class of failure.
            }
            catch (Exception ex)
            {
                Assert.Fail($"Truncation at {length} threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [Fact]
    public void CorruptedEntropy_NeverThrowsNonJpegException()
    {
        var image = JpegImage.CreateGrayscale(32, 32, Gradient(32, 32));
        var full = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 });
        var rng = new Random(9);

        for (var trial = 0; trial < 50; trial++)
        {
            var copy = (byte[])full.Clone();
            // Corrupt a byte in the second half (entropy region), avoiding the EOI.
            var idx = full.Length / 2 + rng.Next(full.Length / 2 - 2);
            copy[idx] = (byte)rng.Next(256);
            try
            {
                Jpeg.Decode(copy);
            }
            catch (JpegException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Corruption at {idx} threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private static byte[] RemoveMarkers(byte[] data, byte markerCode)
    {
        using var ms = new MemoryStream();
        var i = 2; // keep SOI
        ms.Write(data, 0, 2);
        while (i < data.Length - 1)
        {
            if (data[i] == 0xFF && data[i + 1] == markerCode)
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                i += 2 + len;
            }
            else if (data[i] == 0xFF && data[i + 1] == 0xDA)
            {
                ms.Write(data, i, data.Length - i); // copy the rest verbatim
                break;
            }
            else if (data[i] == 0xFF && data[i + 1] is not 0x00 and not (>= 0xD0 and <= 0xD7))
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                ms.Write(data, i, 2 + len);
                i += 2 + len;
            }
            else
            {
                ms.WriteByte(data[i++]);
            }
        }

        return ms.ToArray();
    }

    private static byte[] Gradient(int w, int h)
    {
        var data = new byte[w * h];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);
        return data;
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var data = new byte[w * h * 3];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);
        return data;
    }
}

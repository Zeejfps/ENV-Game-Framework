using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class TableIdValidationTests
{
    [Fact]
    public void FrameQuantTableIdOutOfRange_ThrowsCleanly()
    {
        var bytes = Grayscale();
        var sof = FindMarker(bytes, 0xC0);
        // SOF0 payload: precision, H(2), W(2), Nf, then [id, HV, Tq]; Tq is at payload+8.
        bytes[sof + 4 + 8] = 5; // QuantId = 5 (> 3)

        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Fact]
    public void ScanHuffmanTableIdOutOfRange_ThrowsCleanly()
    {
        var bytes = Grayscale();
        var sos = FindMarker(bytes, 0xDA);
        // SOS payload: Ns, then [Cs, TdTa]; TdTa is at payload+2.
        bytes[sos + 4 + 2] = 0x44; // Td = 4, Ta = 4 (both > 3)

        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Fact]
    public void ScanAcTableIdOutOfRange_ThrowsCleanly()
    {
        var bytes = Grayscale();
        var sos = FindMarker(bytes, 0xDA);
        bytes[sos + 4 + 2] = 0x05; // Td = 0, Ta = 5

        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    private static byte[] Grayscale()
    {
        var pixels = new byte[16 * 16];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(i % 256);
        return Jpeg.Encode(JpegImage.CreateGrayscale(16, 16, pixels), new JpegEncoderOptions { Quality = 80 });
    }

    private static int FindMarker(byte[] data, byte marker)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] == marker)
                return i;
        throw new InvalidOperationException($"marker 0x{marker:X2} not found");
    }
}

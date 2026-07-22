using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class DhtComplianceTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(15)]
    public void Dht_InvalidTableClass_Rejected(int tableClass)
    {
        var bytes = Grayscale();
        var dht = FindMarker(bytes, 0xC4);
        var tcTh = dht + 4;
        var id = bytes[tcTh] & 0x0F;
        bytes[tcTh] = (byte)((tableClass << 4) | id);

        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Fact]
    public void Dht_BitsSumExceeds256_Rejected()
    {
        var bytes = Grayscale();
        var dht = FindMarker(bytes, 0xC4);
        var counts = dht + 5; // skip FF C4 len(2) tcTh
        for (var i = 0; i < 16; i++)
            bytes[counts + i] = 0;
        bytes[counts] = 255;
        bytes[counts + 1] = 2; // sum = 257

        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Fact]
    public void Dht_ValidDcAndAcTables_Parse()
    {
        var bytes = Grayscale();

        var decoded = Jpeg.Decode(bytes);

        Assert.Equal(16, decoded.Width);
        Assert.Equal(16, decoded.Height);
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

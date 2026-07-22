using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class SosScanHeaderComplianceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(255)]
    public void Sos_NsZeroOrTooMany_Rejected(byte ns)
    {
        var bytes = MakeBaseline();
        SetScanNs(bytes, ns);
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Theory]
    [InlineData(1, 63, 0x00)] // Ss != 0
    [InlineData(0, 62, 0x00)] // Se != 63
    [InlineData(0, 63, 0x10)] // Ah != 0
    [InlineData(0, 63, 0x01)] // Al != 0
    public void Sos_Sequential_NonBaselineSpectralParams_Rejected(byte ss, byte se, byte ahAl)
    {
        var bytes = MakeBaseline();
        SetSequentialSpectralParams(bytes, ss, se, ahAl);
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Fact]
    public void Sos_Sequential_ValidBaselineParams_Accepted()
    {
        var image = MakeImage();
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 });
        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
    }

    [Fact]
    public void Sos_Progressive_NonZeroSpectralParams_Allowed()
    {
        var image = MakeImage();
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true });

        // A progressive stream legitimately contains AC scans with Ss != 0, Se != 63.
        var sawAcScan = false;
        for (var i = 0; i < bytes.Length - 9; i++)
        {
            if (bytes[i] == 0xFF && bytes[i + 1] == 0xDA)
            {
                var ns = bytes[i + 4];
                var ssOffset = i + 4 + 1 + ns * 2;
                if (bytes[ssOffset] != 0)
                {
                    sawAcScan = true;
                    break;
                }
            }
        }

        Assert.True(sawAcScan, "expected a progressive AC scan with Ss != 0");
        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
    }

    private static JpegImage MakeImage()
    {
        var pixels = new byte[24 * 24 * 3];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(i % 256);
        return JpegImage.CreateRgb(24, 24, pixels);
    }

    private static byte[] MakeBaseline()
        => Jpeg.Encode(MakeImage(), new JpegEncoderOptions { Quality = 85 });

    private static void SetScanNs(byte[] data, byte ns)
    {
        var sos = FindFirstSos(data);
        data[sos + 4] = ns;
    }

    private static void SetSequentialSpectralParams(byte[] data, byte ss, byte se, byte ahAl)
    {
        var sos = FindFirstSos(data);
        var count = data[sos + 4];
        var p = sos + 4 + 1 + count * 2;
        data[p] = ss;
        data[p + 1] = se;
        data[p + 2] = ahAl;
    }

    private static int FindFirstSos(byte[] data)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] == 0xDA)
                return i;
        throw new Xunit.Sdk.XunitException("no SOS marker found");
    }
}

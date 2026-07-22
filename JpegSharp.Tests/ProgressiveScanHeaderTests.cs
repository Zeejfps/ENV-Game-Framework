using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class ProgressiveScanHeaderTests
{
    [Fact]
    public void SpectralSelectionEndAboveMax_ThrowsCleanly()
    {
        var bytes = MakeProgressive();
        CorruptFirstAcScanSe(bytes, 100); // Se = 100 (> 63)
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Fact]
    public void SpectralSelectionStartAboveEnd_ThrowsCleanly()
    {
        var bytes = MakeProgressive();
        // Force an AC scan with Ss > Se (Se set below Ss).
        CorruptFirstAcScanSe(bytes, 0);
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    [Fact]
    public void DcScanWithNonZeroSe_ThrowsCleanly()
    {
        var bytes = MakeProgressive();
        // Find the DC scan (first SOS, Ss == 0) and set Se to a non-zero value.
        for (var i = 0; i < bytes.Length - 9; i++)
        {
            if (bytes[i] == 0xFF && bytes[i + 1] == 0xDA)
            {
                var ns = bytes[i + 4];
                var ssOffset = i + 4 + 1 + ns * 2;
                if (bytes[ssOffset] == 0) // DC scan
                {
                    bytes[ssOffset + 1] = 20; // Se = 20 with Ss = 0 is illegal
                    break;
                }
            }
        }

        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    private static byte[] MakeProgressive()
    {
        var pixels = new byte[24 * 24 * 3];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(i % 256);
        return Jpeg.Encode(JpegImage.CreateRgb(24, 24, pixels), new JpegEncoderOptions { Quality = 80, Progressive = true });
    }

    private static void CorruptFirstAcScanSe(byte[] data, byte se)
    {
        for (var i = 0; i < data.Length - 9; i++)
        {
            if (data[i] == 0xFF && data[i + 1] == 0xDA)
            {
                var ns = data[i + 4];
                var ssOffset = i + 4 + 1 + ns * 2;
                if (ssOffset + 1 < data.Length && data[ssOffset] != 0) // AC scan (Ss != 0)
                {
                    data[ssOffset + 1] = se;
                    return;
                }
            }
        }
    }
}

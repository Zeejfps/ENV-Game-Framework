using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class HeaderFuzzTests
{
    public static IEnumerable<object[]> Fixtures =>
    [
        [Encode(JpegImage.CreateGrayscale(24, 24, Ramp(24 * 24)), new JpegEncoderOptions { Quality = 80 })],
        [Encode(JpegImage.CreateRgb(24, 24, Ramp(24 * 24 * 3)), new JpegEncoderOptions { Quality = 80, Subsampling = ChromaSubsampling.Samp420 })],
        [Encode(JpegImage.CreateCmyk(16, 16, Ramp(16 * 16 * 4)), new JpegEncoderOptions { Quality = 80 })],
        [Encode(JpegImage.CreateRgb(24, 24, Ramp(24 * 24 * 3)), new JpegEncoderOptions { Quality = 80, Progressive = true })],
    ];

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void CorruptingAnyHeaderByte_NeverThrowsNonJpegException(byte[] original)
    {
        var headerEnd = FindScanStart(original);

        // Try several corruptions at every header byte.
        for (var pos = 2; pos < headerEnd; pos++)
        {
            foreach (var mutation in new byte[] { 0x00, 0xFF, 0x01, 0x80 })
            {
                var copy = (byte[])original.Clone();
                copy[pos] = mutation;
                try
                {
                    var decoded = Jpeg.Decode(copy);
                    _ = decoded.PixelData.Length; // force full materialization
                }
                catch (JpegException)
                {
                    // Acceptable: malformed header rejected cleanly.
                }
                catch (Exception ex)
                {
                    Assert.Fail($"pos {pos} = 0x{mutation:X2} threw {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }

    private static int FindScanStart(byte[] data)
    {
        for (var i = 0; i < data.Length - 3; i++)
            if (data[i] == 0xFF && data[i + 1] == 0xDA)
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                return i + 2 + len;
            }
        return data.Length;
    }

    private static byte[] Encode(JpegImage image, JpegEncoderOptions options) => Jpeg.Encode(image, options);

    private static byte[] Ramp(int length)
    {
        var d = new byte[length];
        for (var i = 0; i < length; i++)
            d[i] = (byte)((i * 19 + 3) % 256);
        return d;
    }
}

using WebPSharp.Api;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Tests;

public class WebPAlphaTests
{
    private static string Asset(string a) => Path.Combine(AppContext.BaseDirectory, "Assets", a);

    /// <summary>Returns the byte offset of the first byte of the given chunk's payload, or -1.</summary>
    private static int FindChunkPayloadOffset(byte[] webp, string fourCc)
    {
        // RIFF header is 12 bytes: "RIFF" + size(4) + "WEBP". Chunks follow.
        var pos = 12;
        while (pos + 8 <= webp.Length)
        {
            var cc = System.Text.Encoding.ASCII.GetString(webp, pos, 4);
            var size = (uint)(webp[pos + 4] | (webp[pos + 5] << 8) | (webp[pos + 6] << 16) | (webp[pos + 7] << 24));
            var payload = pos + 8;
            if (cc == fourCc)
                return payload;
            pos = payload + (int)size;
            if ((size & 1) == 1) pos++; // chunks are padded to even size
        }
        return -1;
    }

    [Fact]
    public void Decode_AlphaPreprocessingP1_Accepted()
    {
        var webp = File.ReadAllBytes(Asset("alpha_q80.webp"));
        var alph = FindChunkPayloadOffset(webp, "ALPH");
        Assert.True(alph > 0, "ALPH chunk not found in test asset.");

        // Set the pre-processing (P) field (bits 4-5) to 1; leave C, F, reserved intact.
        webp[alph] = (byte)((webp[alph] & ~0x30) | 0x10);

        // libwebp accepts P<=1 (P is informational); decode must not throw.
        var image = WebP.Decode(webp);
        Assert.Equal(WebPColorFormat.Rgba, image.Format);
    }

    [Theory]
    [InlineData(0x20)] // P = 2
    [InlineData(0x30)] // P = 3
    [InlineData(0x40)] // reserved bit set
    public void Decode_AlphaPreprocessingInvalid_Throws(int headerBits)
    {
        var webp = File.ReadAllBytes(Asset("alpha_q80.webp"));
        var alph = FindChunkPayloadOffset(webp, "ALPH");
        Assert.True(alph > 0, "ALPH chunk not found in test asset.");

        // Clear P (bits 4-5) and reserved (bits 6-7), then apply the invalid bits.
        webp[alph] = (byte)((webp[alph] & ~0xF0) | headerBits);

        Assert.Throws<WebPFormatException>(() => WebP.Decode(webp));
    }

    [Fact]
    public void DecodesLossyWithAlpha_MatchesDwebp()
    {
        var image = WebP.Decode(File.ReadAllBytes(Asset("alpha_q80.webp")));
        Assert.Equal(WebPColorFormat.Rgba, image.Format);

        var reference = File.ReadAllBytes(Asset("alpha_q80.rgba"));
        Assert.Equal(reference.Length, image.PixelData.Length);

        int maxRgb = 0, maxAlpha = 0;
        for (var i = 0; i < reference.Length; i++)
        {
            var d = Math.Abs(image.PixelData[i] - reference[i]);
            if (i % 4 == 3) maxAlpha = Math.Max(maxAlpha, d);
            else maxRgb = Math.Max(maxRgb, d);
        }
        // Alpha is coded losslessly -> exact; RGB is lossy -> within 1 (RGB rounding).
        Assert.Equal(0, maxAlpha);
        Assert.True(maxRgb <= 1, $"RGB maxDiff={maxRgb}");
    }

    [Fact]
    public void Identify_LossyAlpha_ReportsAlpha()
    {
        var info = WebP.Identify(File.ReadAllBytes(Asset("alpha_q80.webp")));
        Assert.Equal(WebPFormat.Extended, info.Format);
        Assert.True(info.HasAlpha);
    }
}

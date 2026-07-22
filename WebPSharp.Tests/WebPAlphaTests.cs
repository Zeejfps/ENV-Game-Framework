using WebPSharp.Api;

namespace WebPSharp.Tests;

public class WebPAlphaTests
{
    private static string Asset(string a) => Path.Combine(AppContext.BaseDirectory, "Assets", a);

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

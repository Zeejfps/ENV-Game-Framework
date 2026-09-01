using WebPSharp.Api;

namespace WebPSharp.Tests;

public class WebPLossyApiTests
{
    private static string Asset(string a) => Path.Combine(AppContext.BaseDirectory, "Assets", a);

    [Fact]
    public void Decode_LossyFile_MatchesDwebp()
    {
        var bytes = File.ReadAllBytes(Asset("grad_q80.webp"));
        var image = WebP.Decode(bytes);
        Assert.Equal(64, image.Width);
        Assert.Equal(48, image.Height);
        Assert.Equal(WebPColorFormat.Rgba, image.Format);

        var reference = File.ReadAllBytes(Asset("grad_q80.rgba"));
        var max = 0;
        for (var i = 0; i < reference.Length; i++)
            max = Math.Max(max, Math.Abs(image.PixelData[i] - reference[i]));
        Assert.True(max <= 1, $"WebP.Decode differs from dwebp: max={max}");
    }

    [Fact]
    public void Identify_LossyFile_ReportsLossy()
    {
        var info = WebP.Identify(File.ReadAllBytes(Asset("grad_q80.webp")));
        Assert.Equal(WebPFormat.Lossy, info.Format);
        Assert.False(info.IsLossless);
    }

    [Fact]
    public void Load_LossyFile_Works()
    {
        var image = WebP.Load(Asset("grad_q80.webp"));
        Assert.Equal(64 * 48 * 4, image.PixelData.Length);
    }
}

using WebPSharp.Api;

namespace WebPSharp.Tests;

public class Vp8GoldenBatchTests
{
    private static string Asset(string a) => Path.Combine(AppContext.BaseDirectory, "Assets", a);

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
    public void DecodesLossyCase_MatchesDwebpNoFancy(int n)
    {
        var webp = File.ReadAllBytes(Asset($"case_{n}.webp"));
        var reference = File.ReadAllBytes(Asset($"case_{n}.rgba"));
        var image = WebP.Decode(webp);

        Assert.Equal(reference.Length, image.PixelData.Length);
        var max = 0;
        long sum = 0;
        for (var i = 0; i < reference.Length; i++)
        {
            var dd = Math.Abs(image.PixelData[i] - reference[i]);
            if (dd > max) max = dd;
            sum += dd;
        }
        // Nearest-upsampling decode must be bit-exact with dwebp -nofancy (allow 1 for RGB rounding).
        Assert.True(max <= 1, $"case {n}: maxDiff={max}, meanDiff={(double)sum / reference.Length:F3}");
    }
}

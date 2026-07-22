using JpegSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using Xunit;

namespace JpegSharp.Tests;

/// <summary>
/// Integration tests that round-trip genuine reference images (loaded from PNG via PngSharp)
/// through the JPEG codec, exercising the pipeline on real photographic/graphic content
/// rather than synthetic patterns.
/// </summary>
public class RealImageIntegrationTests
{
    [Theory]
    [InlineData("test_64x64.png")]
    [InlineData("sprite_atlas.png")]
    public void RealImage_HighQuality444_RoundTripsWithGoodFidelity(string asset)
    {
        var (width, height, rgb) = LoadRgb(asset);
        var image = JpegImage.CreateRgb(width, height, rgb);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 95, Subsampling = ChromaSubsampling.Samp444 }));

        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.True(Psnr(rgb, decoded.PixelData) > 32.0, "PSNR too low on real image at q95 4:4:4");
    }

    [Theory]
    [InlineData("test_64x64.png")]
    [InlineData("sprite_atlas.png")]
    public void RealImage_Progressive_MatchesBaseline(string asset)
    {
        var (width, height, rgb) = LoadRgb(asset);
        var image = JpegImage.CreateRgb(width, height, rgb);

        foreach (var sub in new[] { ChromaSubsampling.Samp444, ChromaSubsampling.Samp420 })
        {
            var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 88, Subsampling = sub }));
            var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 88, Subsampling = sub, Progressive = true }));
            Assert.Equal(baseline.PixelData, progressive.PixelData);
        }
    }

    [Fact]
    public void RealImage_HigherQuality_ImprovesFidelity()
    {
        var (width, height, rgb) = LoadRgb("sprite_atlas.png");
        var image = JpegImage.CreateRgb(width, height, rgb);

        var low = Psnr(rgb, Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 40 })).PixelData);
        var high = Psnr(rgb, Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 92 })).PixelData);
        Assert.True(high > low, $"q92 PSNR {high:F1} should exceed q40 PSNR {low:F1}");
    }

    private static (int Width, int Height, byte[] Rgb) LoadRgb(string asset)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", asset);
        var png = PngSharp.Api.Png.DecodeFromFile(path);
        var width = (int)png.Ihdr.Width;
        var height = (int)png.Ihdr.Height;
        var bpp = png.Ihdr.GetBytesPerPixel();
        var src = png.PixelData;

        var rgb = new byte[width * height * 3];
        for (var i = 0; i < width * height; i++)
        {
            switch (png.Ihdr.ColorType)
            {
                case ColorType.TrueColor:
                case ColorType.TrueColorWithAlpha:
                    rgb[i * 3] = src[i * bpp];
                    rgb[i * 3 + 1] = src[i * bpp + 1];
                    rgb[i * 3 + 2] = src[i * bpp + 2];
                    break;
                case ColorType.Grayscale:
                case ColorType.GrayscaleWithAlpha:
                    var g = src[i * bpp];
                    rgb[i * 3] = g;
                    rgb[i * 3 + 1] = g;
                    rgb[i * 3 + 2] = g;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported PNG color type {png.Ihdr.ColorType} for {asset}");
            }
        }

        return (width, height, rgb);
    }

    private static double Psnr(byte[] a, byte[] b)
    {
        double mse = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var d = a[i] - b[i];
            mse += d * d;
        }

        mse /= a.Length;
        return mse <= 0 ? double.PositiveInfinity : 10.0 * Math.Log10(255.0 * 255.0 / mse);
    }
}

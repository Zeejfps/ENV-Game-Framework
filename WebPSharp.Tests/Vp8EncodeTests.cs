using WebPSharp.Api;

namespace WebPSharp.Tests;

public class Vp8EncodeTests
{
    // A smooth photographic-style gradient: compresses well and quantizes with low error, so it is a
    // stable subject for PSNR thresholds.
    private static byte[] Smooth(int w, int h)
    {
        var px = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var o = (y * w + x) * 4;
            px[o] = (byte)(x * 255 / Math.Max(1, w - 1));
            px[o + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
            px[o + 2] = (byte)(128 + 60 * Math.Sin((x + y) * 0.1));
            px[o + 3] = 255;
        }
        return px;
    }

    private static double Psnr(byte[] a, byte[] b)
    {
        double mse = 0;
        long n = 0;
        for (var i = 0; i < a.Length; i += 4)
        for (var k = 0; k < 3; k++)
        {
            var d = a[i + k] - b[i + k];
            mse += (double)d * d;
            n++;
        }
        mse /= n;
        return mse == 0 ? 99 : 10 * Math.Log10(255.0 * 255.0 / mse);
    }

    [Theory]
    [InlineData(16, 16)]
    [InlineData(17, 15)]
    [InlineData(1, 1)]
    [InlineData(31, 33)]
    [InlineData(100, 1)]
    [InlineData(64, 48)]
    [InlineData(65, 49)]
    public void Encode_ThenDecode_PreservesDimensionsAndFormat(int w, int h)
    {
        var img = WebPImage.CreateRgba(w, h, Smooth(w, h));
        var bytes = WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 80 });

        var info = WebP.Identify(bytes);
        Assert.Equal(WebPFormat.Lossy, info.Format);
        Assert.Equal(w, info.Width);
        Assert.Equal(h, info.Height);

        var decoded = WebP.Decode(bytes);
        Assert.Equal(w, decoded.Width);
        Assert.Equal(h, decoded.Height);
        Assert.Equal(WebPColorFormat.Rgba, decoded.Format);
        Assert.Equal(w * h * 4, decoded.PixelData.Length);
    }

    [Fact]
    public void Encode_SmoothImage_MeetsQualityThreshold()
    {
        var px = Smooth(64, 48);
        var img = WebPImage.CreateRgba(64, 48, px);
        var decoded = WebP.Decode(WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 80 }));
        Assert.True(Psnr(px, decoded.PixelData) >= 32, "smooth q80 should decode with PSNR >= 32 dB");
    }

    [Fact]
    public void Encode_HigherQuality_ImprovesFidelity()
    {
        var px = Smooth(80, 64);
        var img = WebPImage.CreateRgba(80, 64, px);

        var low = WebP.Decode(WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 30 }));
        var high = WebP.Decode(WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 95 }));

        Assert.True(Psnr(px, high.PixelData) > Psnr(px, low.PixelData),
            "higher quality must not reduce PSNR");
    }

    [Fact]
    public void Encode_IsDeterministic()
    {
        var img = WebPImage.CreateRgba(48, 32, Smooth(48, 32));
        var a = WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 70 });
        var b = WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 70 });
        Assert.Equal(a, b);
    }

    [Fact]
    public void Encode_RgbInput_Works()
    {
        var rgba = Smooth(32, 32);
        var rgb = new byte[32 * 32 * 3];
        for (var i = 0; i < 32 * 32; i++)
        {
            rgb[i * 3] = rgba[i * 4];
            rgb[i * 3 + 1] = rgba[i * 4 + 1];
            rgb[i * 3 + 2] = rgba[i * 4 + 2];
        }
        var img = WebPImage.CreateRgb(32, 32, rgb);
        var decoded = WebP.Decode(WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 85 }));
        Assert.Equal(32, decoded.Width);
        Assert.True(Psnr(rgba, decoded.PixelData) >= 32);
    }

    [Fact]
    public void Encode_SolidColor_RoundTripsExactly()
    {
        var px = new byte[16 * 16 * 4];
        for (var i = 0; i < px.Length; i += 4)
        {
            px[i] = 40;
            px[i + 1] = 160;
            px[i + 2] = 200;
            px[i + 3] = 255;
        }
        var img = WebPImage.CreateRgba(16, 16, px);
        var decoded = WebP.Decode(WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 90 }));
        // A flat block leaves only DC; at high quality the color must survive within chroma rounding.
        Assert.True(Psnr(px, decoded.PixelData) >= 40);
    }

    [Fact]
    public void Encode_OpaqueRgba_OmitsAlphaChunk()
    {
        var img = WebPImage.CreateRgba(32, 32, Smooth(32, 32)); // Smooth() sets every alpha to 255
        var info = WebP.Identify(WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 75 }));
        Assert.Equal(WebPFormat.Lossy, info.Format);
        Assert.False(info.HasAlpha);
    }

    [Theory]
    [InlineData(32, 32)]
    [InlineData(65, 49)]
    [InlineData(17, 20)]
    public void Encode_WithAlpha_RoundTripsAlphaExactly(int w, int h)
    {
        var px = Smooth(w, h);
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
            px[(y * w + x) * 4 + 3] = (byte)((x / 6 + y / 6) % 2 == 0 ? 255 : 40); // block-checker alpha

        var img = WebPImage.CreateRgba(w, h, px);
        var bytes = WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 85 });

        var info = WebP.Identify(bytes);
        Assert.Equal(WebPFormat.Extended, info.Format);
        Assert.True(info.HasAlpha);

        var decoded = WebP.Decode(bytes);
        // Alpha is stored losslessly, so it must survive bit-exactly even though RGB is lossy.
        for (var i = 3; i < px.Length; i += 4)
            Assert.Equal(px[i], decoded.PixelData[i]);
    }

    [Fact]
    public void Decode_FilteredEncoderOutput_MatchesDwebp()
    {
        // lossy_filtered.webp is our own lossy encoder's output (filter level 21). Because the
        // encoder does not signal per-MB skip, it produces non-skip all-zero macroblocks; the
        // decoder must skip their inner-edge filtering exactly as libwebp does. The reference .rgba
        // is dwebp's decode of the same file, so this guards the loop-filter f_inner rule.
        var asset = Path.Combine(AppContext.BaseDirectory, "Assets");
        var webp = File.ReadAllBytes(Path.Combine(asset, "lossy_filtered.webp"));
        var reference = File.ReadAllBytes(Path.Combine(asset, "lossy_filtered.rgba"));

        var image = WebP.Decode(webp);
        Assert.Equal(reference.Length, image.PixelData.Length);
        var max = 0;
        for (var i = 0; i < reference.Length; i++)
            max = Math.Max(max, Math.Abs(image.PixelData[i] - reference[i]));
        Assert.True(max <= 1, $"decode differs from dwebp: max={max}");
    }

    [Fact]
    public void Encode_WithMetadata_WritesExtendedLossyContainer()
    {
        var img = WebPImage.CreateRgba(32, 32, Smooth(32, 32));
        img.Metadata = new WebPMetadata { Xmp = "<x:xmpmeta/>"u8.ToArray() };
        var bytes = WebP.Encode(img, new WebPEncoderOptions { Lossless = false, Quality = 75 });

        // Carrying metadata forces an extended (VP8X) container wrapping the lossy VP8 image.
        var info = WebP.Identify(bytes);
        Assert.Equal(WebPFormat.Extended, info.Format);

        var decoded = WebP.Decode(bytes);
        Assert.Equal(32, decoded.Width);
        Assert.NotNull(decoded.Metadata);
        Assert.Equal("<x:xmpmeta/>"u8.ToArray(), decoded.Metadata!.Xmp);
    }
}

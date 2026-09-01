using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LCombinatorialTests
{
    private static byte[] ToRgba(WebPImage image)
    {
        if (image.Format == WebPColorFormat.Rgba)
            return image.PixelData;
        var rgba = new byte[image.Width * image.Height * 4];
        var src = image.PixelData;
        for (int i = 0, j = 0; i < src.Length; i += 3, j += 4)
        {
            rgba[j] = src[i]; rgba[j + 1] = src[i + 1]; rgba[j + 2] = src[i + 2]; rgba[j + 3] = 255;
        }
        return rgba;
    }

    private static IEnumerable<(string Name, WebPImage Image)> Images()
    {
        const int w = 19, h = 13;

        var noise = new byte[w * h * 4];
        new Random(1).NextBytes(noise);
        yield return ("noise", WebPImage.CreateRgba(w, h, noise));

        var gradient = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var idx = (y * w + x) * 4;
            gradient[idx] = (byte)(x * 8);
            gradient[idx + 1] = (byte)(y * 8);
            gradient[idx + 2] = (byte)(x + y);
            gradient[idx + 3] = 255;
        }
        yield return ("gradient", WebPImage.CreateRgba(w, h, gradient));

        var solid = new byte[w * h * 4];
        for (var i = 0; i < solid.Length; i += 4) { solid[i] = 33; solid[i + 1] = 66; solid[i + 2] = 99; solid[i + 3] = 255; }
        yield return ("solid", WebPImage.CreateRgba(w, h, solid));

        yield return ("transparent", WebPImage.CreateRgba(w, h, new byte[w * h * 4]));

        var rgb = new byte[w * h * 3];
        new Random(2).NextBytes(rgb);
        yield return ("rgb", WebPImage.CreateRgb(w, h, rgb));
    }

    private static List<Vp8LEncodeSettings> AllSettings()
    {
        var list = new List<Vp8LEncodeSettings>
        {
            new() { Lz77 = false },
            new() { Lz77 = true },
            new() { Lz77 = true, SubtractGreen = true },
            new() { Lz77 = true, CrossColor = true, CrossColorGreenToRed = 30, CrossColorGreenToBlue = 10, CrossColorRedToBlue = 40 },
            new() { Lz77 = true, ColorCacheBits = 6 },
            new() { Lz77 = false, ColorCacheBits = 8 },
            new() { Lz77 = true, MetaHuffman = true, MetaHuffmanGroups = 3 },
            new() { Lz77 = true, SubtractGreen = true, ColorCacheBits = 8 },
            new() { Lz77 = true, CrossColor = true, CrossColorGreenToRed = 12, SubtractGreen = true, ColorCacheBits = 4 },
        };

        // All 14 predictor modes, alone and with a color cache.
        for (var mode = 0; mode < 14; mode++)
        {
            list.Add(new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = mode });
            list.Add(new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = mode, ColorCacheBits = 8 });
        }

        return list;
    }

    public static IEnumerable<object[]> SettingsIndices()
    {
        var count = AllSettings().Count;
        for (var i = 0; i < count; i++)
            yield return new object[] { i };
    }

    [Theory]
    [MemberData(nameof(SettingsIndices))]
    public void EverySettingRoundTripsEveryImage(int settingsIndex)
    {
        var settings = AllSettings()[settingsIndex];
        foreach (var (name, image) in Images())
        {
            var expected = ToRgba(image);
            var payload = Vp8LEncoder.Encode(image, settings);
            var decoded = Vp8LDecoder.Decode(payload);
            Assert.True(expected.AsSpan().SequenceEqual(decoded.PixelData),
                $"Round-trip failed for image '{name}' with settings P={settings.Predictor}/{settings.PredictorMode} " +
                $"X={settings.CrossColor} G={settings.SubtractGreen} Cache={settings.ColorCacheBits} Meta={settings.MetaHuffman} Lz77={settings.Lz77}.");
        }
    }

    [Fact]
    public void PaletteRoundTripsWithCacheAndVariousColorCounts()
    {
        var rng = new Random(7);
        foreach (var colors in new[] { 2, 4, 16, 17, 100, 256 })
        {
            var palette = new uint[colors];
            for (var i = 0; i < colors; i++) palette[i] = (uint)rng.NextInt64(0, 1L << 32);
            const int w = 24, h = 20;
            var pixels = new byte[w * h * 4];
            for (var p = 0; p < w * h; p++)
            {
                var c = palette[p < colors ? p : rng.Next(colors)];
                pixels[p * 4] = (byte)(c >> 16); pixels[p * 4 + 1] = (byte)(c >> 8);
                pixels[p * 4 + 2] = (byte)c; pixels[p * 4 + 3] = (byte)(c >> 24);
            }
            var image = WebPImage.CreateRgba(w, h, pixels);
            var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Palette = true, Lz77 = true, ColorCacheBits = 8 });
            Assert.Equal(pixels, Vp8LDecoder.Decode(payload).PixelData);
        }
    }
}

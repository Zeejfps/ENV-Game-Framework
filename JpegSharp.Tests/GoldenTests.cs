using System.Security.Cryptography;
using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

/// <summary>
/// Golden regression tests pinning the encoder's deterministic byte output for canonical
/// inputs. If a change alters the produced bytes, these fail and must be reviewed.
/// </summary>
public class GoldenTests
{
    [Theory]
    [InlineData("gray16_q75", "131d6dcad3f033dce1cc690fa869b23e632f9ffd13a2d7737456b57680730c61")]
    [InlineData("rgb16_420_q90", "233b8457f11f5d7db54a69f5a6178c487b102a3a15830ca7fe1eeb5c7dd907f1")]
    [InlineData("rgb16_444_prog_q90", "1905b9a44d9ef16288c98d1bea2caaf6e4b4496c78227b1e8e7d068ee971f98e")]
    [InlineData("cmyk16_q85", "62939a315af1e3ac4c2a2caa35c1921ea98ac5e56fbb99cb1fb2eb24d535cbca")]
    public void EncoderOutput_MatchesGoldenHash(string fixture, string expectedHash)
    {
        var bytes = Encode(fixture);
        var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Assert.Equal(expectedHash, actual);
    }

    [Fact]
    public void GoldenBytes_DecodeBackToExpectedPixels()
    {
        // Encoding is deterministic, so the golden bytes must decode to the original within
        // the codec's lossy tolerance.
        var original = GrayGradient(16, 16);
        var decoded = Jpeg.Decode(Encode("gray16_q75"));
        Assert.Equal(16, decoded.Width);
        Assert.Equal(16, decoded.Height);

        long total = 0;
        for (var i = 0; i < original.Length; i++)
            total += Math.Abs(original[i] - decoded.PixelData[i]);
        Assert.True((double)total / original.Length <= 5.0);
    }

    internal static byte[] Encode(string fixture) => fixture switch
    {
        "gray16_q75" => Jpeg.Encode(JpegImage.CreateGrayscale(16, 16, GrayGradient(16, 16)),
            new JpegEncoderOptions { Quality = 75 }),
        "rgb16_420_q90" => Jpeg.Encode(JpegImage.CreateRgb(16, 16, RgbGradient(16, 16)),
            new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp420 }),
        "rgb16_444_prog_q90" => Jpeg.Encode(JpegImage.CreateRgb(16, 16, RgbGradient(16, 16)),
            new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp444, Progressive = true }),
        "cmyk16_q85" => Jpeg.Encode(JpegImage.CreateCmyk(16, 16, CmykGradient(16, 16)),
            new JpegEncoderOptions { Quality = 85 }),
        _ => throw new ArgumentException($"Unknown fixture {fixture}", nameof(fixture)),
    };

    internal static byte[] GrayGradient(int w, int h)
    {
        var data = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                data[y * w + x] = (byte)((x * 255 / (w - 1) + y * 255 / (h - 1)) / 2);
        return data;
    }

    internal static byte[] RgbGradient(int w, int h)
    {
        var data = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                data[i] = (byte)(x * 255 / (w - 1));
                data[i + 1] = (byte)(y * 255 / (h - 1));
                data[i + 2] = (byte)((x + y) * 255 / (w + h - 2));
            }
        return data;
    }

    internal static byte[] CmykGradient(int w, int h)
    {
        var data = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                data[i] = (byte)(x * 255 / (w - 1));
                data[i + 1] = (byte)(y * 255 / (h - 1));
                data[i + 2] = (byte)((x + y) * 255 / (w + h - 2));
                data[i + 3] = (byte)(255 - x * 255 / (w - 1));
            }
        return data;
    }
}

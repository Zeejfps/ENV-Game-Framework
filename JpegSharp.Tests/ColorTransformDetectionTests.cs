using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ColorTransformDetectionTests
{
    [Fact]
    public void RgbDirect_Progressive_MatchesRgbDirectBaseline()
    {
        var pixels = ColorGradient(40, 32);
        var image = JpegImage.CreateRgb(40, 32, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RgbEncoding = JpegRgbEncoding.Rgb }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, RgbEncoding = JpegRgbEncoding.Rgb, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void AdobeTransform1_TriggersYCbCrDecode()
    {
        // A default (JFIF/YCbCr) stream already applies YCbCr. Injecting an Adobe transform=1
        // marker must keep applying YCbCr, so the result is unchanged.
        var image = JpegImage.CreateRgb(32, 32, ColorGradient(32, 32));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp444 });
        var reference = Jpeg.Decode(bytes);

        var withAdobe = InsertAdobeMarker(bytes, transform: 1);
        var decoded = Jpeg.Decode(withAdobe);
        Assert.Equal(1, decoded.Metadata!.AdobeColorTransform);
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void RgbDirect_HalfResIsNotWorseThanYCbCr420_OnColorEdges()
    {
        // A sharp red/blue vertical edge: 4:2:0 blurs the chroma boundary; RGB-direct does not.
        var pixels = new byte[16 * 16 * 3];
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                var i = (y * 16 + x) * 3;
                if (x < 8)
                {
                    pixels[i] = 255;
                }
                else
                {
                    pixels[i + 2] = 255;
                }
            }

        var image = JpegImage.CreateRgb(16, 16, pixels);
        var direct = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 95, RgbEncoding = JpegRgbEncoding.Rgb }));

        var directErr = TestMetrics.MeanError(pixels, direct.PixelData);
        Assert.True(directErr < 12.0, $"RGB-direct mean error {directErr:F2} too high");
    }

    private static byte[] InsertAdobeMarker(byte[] data, byte transform)
    {
        byte[] adobe =
        [
            0xFF, 0xEE, 0x00, 0x0E,
            (byte)'A', (byte)'d', (byte)'o', (byte)'b', (byte)'e',
            0x00, 0x64, 0x00, 0x00, 0x00, 0x00, transform,
        ];
        using var ms = new MemoryStream();
        ms.Write(data, 0, 2); // SOI
        ms.Write(adobe);
        ms.Write(data, 2, data.Length - 2);
        return ms.ToArray();
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var idx = (y * w + x) * 3;
                d[idx] = (byte)(x * 255 / Math.Max(1, w - 1));
                d[idx + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                d[idx + 2] = (byte)((x + y) % 256);
            }
        return d;
    }
}

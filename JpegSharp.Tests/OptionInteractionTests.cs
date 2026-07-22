using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class OptionInteractionTests
{
    [Fact]
    public void Progressive_WithOptimizeHuffman_ProducesValidOutput()
    {
        // OptimizeHuffman currently applies to the baseline path only; progressive uses the
        // standard tables. Setting it must still produce a valid, decodable stream.
        var pixels = ColorGradient(32, 32);
        var image = JpegImage.CreateRgb(32, 32, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 85, Progressive = true, OptimizeHuffman = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void OptimizeHuffman_WithRestart_RoundTrips()
    {
        var pixels = Gradient(48, 48);
        var image = JpegImage.CreateGrayscale(48, 48, pixels);
        var reference = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 }));
        var optimized = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 80, OptimizeHuffman = true, RestartInterval = 4 }));
        Assert.Equal(reference.PixelData, optimized.PixelData);
    }

    [Fact]
    public void CustomQuant_WithProgressive_RoundTrips()
    {
        var pixels = Gradient(40, 40);
        var image = JpegImage.CreateGrayscale(40, 40, pixels);
        var ones = new ushort[64];
        Array.Fill(ones, (ushort)3);
        var table = new JpegSharp.Quantization.QuantizationTable(ones);

        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { LuminanceQuantizationTable = table }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { LuminanceQuantizationTable = table, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void OptimizeHuffman_WithSubsampling_ProducesSmallerOutput()
    {
        var pixels = ColorGradient(64, 64);
        var image = JpegImage.CreateRgb(64, 64, pixels);
        var standard = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, Subsampling = ChromaSubsampling.Samp420 });
        var optimized = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80, Subsampling = ChromaSubsampling.Samp420, OptimizeHuffman = true });
        Assert.True(optimized.Length <= standard.Length);

        // And it still decodes to the same pixels.
        Assert.Equal(Jpeg.Decode(standard).PixelData, Jpeg.Decode(optimized).PixelData);
    }

    private static byte[] Gradient(int w, int h)
    {
        var d = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                d[y * w + x] = (byte)((x * 255 / (w - 1) + y * 255 / (h - 1)) / 2);
        return d;
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                d[i] = (byte)(x * 255 / (w - 1));
                d[i + 1] = (byte)(y * 255 / (h - 1));
                d[i + 2] = (byte)((x + y) % 256);
            }
        return d;
    }
}

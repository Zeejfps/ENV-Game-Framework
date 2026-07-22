using JpegSharp.Api;
using JpegSharp.Huffman;
using Xunit;

namespace JpegSharp.Tests;

public class CombinationTests
{
    private static HuffmanTable CompleteTable()
    {
        var freq = new int[256];
        Array.Fill(freq, 1);
        return HuffmanTable.BuildOptimized(freq);
    }

    [Fact]
    public void CustomHuffman_WithRestart_RoundTrips()
    {
        var image = JpegImage.CreateGrayscale(48, 48, Gray(48, 48));
        var reference = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 }));
        var custom = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions
        {
            Quality = 80,
            RestartInterval = 5,
            LuminanceDcHuffmanTable = CompleteTable(),
            LuminanceAcHuffmanTable = CompleteTable(),
        }));
        Assert.Equal(reference.PixelData, custom.PixelData);
    }

    [Fact]
    public void CustomHuffman_WithProgressive_RoundTrips()
    {
        var image = JpegImage.CreateGrayscale(40, 40, Gray(40, 40));
        var reference = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 82 }));
        var custom = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions
        {
            Quality = 82,
            Progressive = true,
            LuminanceDcHuffmanTable = CompleteTable(),
            LuminanceAcHuffmanTable = CompleteTable(),
        }));
        Assert.Equal(reference.PixelData, custom.PixelData);
    }

    [Fact]
    public void Cmyk_WithMetadata_RoundTrips()
    {
        var metadata = new JpegMetadata { Exif = [1, 2, 3, 4], IccProfile = new byte[300] };
        metadata.Comments.Add("cmyk");
        var image = JpegImage.CreateCmyk(24, 24, Cmyk(24, 24));
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));

        Assert.Equal([1, 2, 3, 4], decoded.Metadata!.Exif);
        Assert.Equal(new byte[300], decoded.Metadata.IccProfile);
        Assert.Contains("cmyk", decoded.Metadata.Comments);
        Assert.Equal(0, decoded.Metadata.AdobeColorTransform);
    }

    [Fact]
    public void RgbDirect_WithSubsamplingOption_IgnoresSubsampling()
    {
        var image = JpegImage.CreateRgb(32, 32, ColorGradient(32, 32));
        // Subsampling is irrelevant for RGB-direct; it must still round-trip.
        var a = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { RgbEncoding = JpegRgbEncoding.Rgb, Subsampling = ChromaSubsampling.Samp420 }));
        var b = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { RgbEncoding = JpegRgbEncoding.Rgb, Subsampling = ChromaSubsampling.Samp444 }));
        Assert.Equal(a.PixelData, b.PixelData);
    }

    [Fact]
    public void RgbDirect_WithOptimizeHuffman_RoundTrips()
    {
        var image = JpegImage.CreateRgb(32, 32, ColorGradient(32, 32));
        var reference = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { RgbEncoding = JpegRgbEncoding.Rgb }));
        var optimized = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { RgbEncoding = JpegRgbEncoding.Rgb, OptimizeHuffman = true }));
        Assert.Equal(reference.PixelData, optimized.PixelData);
    }

    [Fact]
    public void Cmyk_WithOptimizeHuffman_RoundTrips()
    {
        var image = JpegImage.CreateCmyk(24, 24, Cmyk(24, 24));
        var reference = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 }));
        var optimized = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, OptimizeHuffman = true }));
        Assert.Equal(reference.PixelData, optimized.PixelData);
    }

    [Fact]
    public void Ycck_WithOptimizeHuffmanAndRestart_RoundTrips()
    {
        var image = JpegImage.CreateCmyk(32, 24, Cmyk(32, 24));
        var reference = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 88, CmykAsYcck = true }));
        var optimized = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 88, CmykAsYcck = true, OptimizeHuffman = true, RestartInterval = 3 }));
        Assert.Equal(reference.PixelData, optimized.PixelData);
    }

    private static byte[] Gray(int w, int h)
    {
        var d = new byte[w * h];
        for (var i = 0; i < d.Length; i++)
            d[i] = (byte)((i * 11) % 256);
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

    private static byte[] Cmyk(int w, int h)
    {
        var d = new byte[w * h * 4];
        for (var i = 0; i < d.Length; i++)
            d[i] = (byte)((i * 13 + 5) % 256);
        return d;
    }
}

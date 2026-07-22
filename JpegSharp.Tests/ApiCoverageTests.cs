using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ApiCoverageTests
{
    [Fact]
    public void CmykProgressive_MatchesCmykBaseline()
    {
        var pixels = CmykGradient(32, 24);
        var image = JpegImage.CreateCmyk(32, 24, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void YcckProgressive_MatchesYcckBaseline()
    {
        var pixels = CmykGradient(24, 24);
        var image = JpegImage.CreateCmyk(24, 24, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, CmykAsYcck = true }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, CmykAsYcck = true, Progressive = true }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void Metadata_SurvivesProgressiveEncoding()
    {
        var metadata = new JpegMetadata { Exif = [1, 2, 3, 4], Density = new JfifDensity(JpegDensityUnit.DotsPerInch, 150, 150) };
        metadata.Comments.Add("progressive");
        var image = JpegImage.CreateRgb(24, 24, new byte[24 * 24 * 3]);

        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Progressive = true, Metadata = metadata }));
        Assert.Equal([1, 2, 3, 4], decoded.Metadata!.Exif);
        Assert.Equal(new JfifDensity(JpegDensityUnit.DotsPerInch, 150, 150), decoded.Metadata.Density);
        Assert.Contains("progressive", decoded.Metadata.Comments);
    }

    [Fact]
    public void ColorProgressive_WithRestart_MatchesBaseline()
    {
        var pixels = ColorGradient(48, 32);
        var image = JpegImage.CreateRgb(48, 32, pixels);
        var baseline = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Quality = 82, Subsampling = ChromaSubsampling.Samp420 }));
        var progressive = Jpeg.Decode(Jpeg.Encode(image,
            new JpegEncoderOptions { Quality = 82, Subsampling = ChromaSubsampling.Samp420, Progressive = true, RestartInterval = 5 }));
        Assert.Equal(baseline.PixelData, progressive.PixelData);
    }

    [Fact]
    public void SaveAndLoad_FileRoundTrips()
    {
        var pixels = ColorGradient(32, 32);
        var image = JpegImage.CreateRgb(32, 32, pixels);
        var path = Path.Combine(Path.GetTempPath(), $"jpegsharp_{Guid.NewGuid():N}.jpg");
        try
        {
            Jpeg.Save(image, path, new JpegEncoderOptions { Quality = 90 });
            Assert.True(File.Exists(path));

            var loaded = Jpeg.Load(path);
            Assert.Equal(32, loaded.Width);
            Assert.Equal(32, loaded.Height);
            Assert.Equal(JpegColorSpace.Rgb, loaded.ColorSpace);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Decode_FromStream_MatchesDecodeFromBytes()
    {
        var image = JpegImage.CreateGrayscale(20, 20, GrayGradient(20, 20));
        var bytes = Jpeg.Encode(image);

        var fromBytes = Jpeg.Decode(bytes);
        using var ms = new MemoryStream(bytes);
        var fromStream = Jpeg.Decode(ms);

        Assert.Equal(fromBytes.PixelData, fromStream.PixelData);
    }

    [Fact]
    public void Encode_ToStream_MatchesEncodeToBytes()
    {
        var image = JpegImage.CreateRgb(24, 24, ColorGradient(24, 24));
        var options = new JpegEncoderOptions { Quality = 88 };

        var bytes = Jpeg.Encode(image, options);
        using var ms = new MemoryStream();
        Jpeg.Encode(image, ms, options);

        Assert.Equal(bytes, ms.ToArray());
    }

    [Fact]
    public void ImageMetadata_IsUsedWhenOptionsMetadataNull()
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        image.Metadata = new JpegMetadata();
        image.Metadata.Comments.Add("from image");

        var decoded = Jpeg.Decode(Jpeg.Encode(image)); // no options metadata
        Assert.Contains("from image", decoded.Metadata!.Comments);
    }

    private static byte[] GrayGradient(int w, int h)
    {
        var d = new byte[w * h];
        for (var i = 0; i < d.Length; i++)
            d[i] = (byte)(i % 256);
        return d;
    }

    private static byte[] ColorGradient(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 3;
                d[i] = (byte)(x * 255 / Math.Max(1, w - 1));
                d[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                d[i + 2] = (byte)((x + y) % 256);
            }
        return d;
    }

    private static byte[] CmykGradient(int w, int h)
    {
        var d = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                d[i] = (byte)(x * 255 / Math.Max(1, w - 1));
                d[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
                d[i + 2] = (byte)((x + y) % 256);
                d[i + 3] = (byte)(255 - (x + y) % 256);
            }
        return d;
    }
}

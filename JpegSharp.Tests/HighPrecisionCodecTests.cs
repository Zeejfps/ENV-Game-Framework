using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class HighPrecisionCodecTests
{
    private const int Q100 = 100;

    [Fact]
    public void Grayscale12_RoundTrips()
    {
        var image = JpegImage16.CreateGrayscale(32, 24, 12, Gradient12(32, 24, 1));
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, new JpegEncoderOptions { Quality = Q100 }));

        Assert.Equal(12, decoded.Precision);
        Assert.Equal(JpegColorSpace.Grayscale, decoded.ColorSpace);
        Assert.Equal(32, decoded.Width);
        Assert.Equal(24, decoded.Height);
        AssertClose(image.PixelData, decoded.PixelData, 6);
    }

    [Fact]
    public void Encode16_RejectsNon12BitPrecision()
    {
        // JPEG DCT sample precision is 8 or 12 (ITU-T T.81); the JpegImage16 container permits
        // 9–16 bit for packing/interop, but the codec does not encode those.
        var image = JpegImage16.CreateGrayscale(8, 8, 16, new ushort[64]);
        Assert.Throws<NotSupportedException>(() => Jpeg.Encode16(image));
    }

    [Fact]
    public void RgbDirect12_RoundTrips()
    {
        var image = JpegImage16.CreateRgb(24, 24, 12, Gradient12(24, 24, 3));
        var options = new JpegEncoderOptions { Quality = Q100, RgbEncoding = JpegRgbEncoding.Rgb };
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, options));

        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);
        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 6);
    }

    [Fact]
    public void RgbYCbCr12_RoundTrips()
    {
        var image = JpegImage16.CreateRgb(24, 24, 12, Gradient12(24, 24, 3));
        var options = new JpegEncoderOptions { Quality = Q100, Subsampling = ChromaSubsampling.Samp444 };
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, options));

        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);
        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 24); // YCbCr color round-trip, no subsampling
    }

    [Theory]
    [InlineData(ChromaSubsampling.Samp420)]
    [InlineData(ChromaSubsampling.Samp422)]
    [InlineData(ChromaSubsampling.Samp411)]
    public void RgbYCbCr12_Subsampled_RoundTrips(ChromaSubsampling subsampling)
    {
        var image = JpegImage16.CreateRgb(32, 32, 12, Gradient12(32, 32, 3));
        var options = new JpegEncoderOptions { Quality = Q100, Subsampling = subsampling };
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, options));

        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);
        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 96); // chroma downsample/upsample loss (up to 4x for 4:1:1)
    }

    [Fact]
    public void Grayscale12_Progressive_RoundTrips()
    {
        var image = JpegImage16.CreateGrayscale(32, 24, 12, Gradient12(32, 24, 1));
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, new JpegEncoderOptions { Quality = Q100, Progressive = true }));

        Assert.Equal(12, decoded.Precision);
        Assert.Equal(JpegColorSpace.Grayscale, decoded.ColorSpace);
        AssertClose(image.PixelData, decoded.PixelData, 6);
    }

    [Fact]
    public void RgbYCbCr12_Progressive_RoundTrips()
    {
        var image = JpegImage16.CreateRgb(24, 24, 12, Gradient12(24, 24, 3));
        var options = new JpegEncoderOptions { Quality = Q100, Progressive = true, Subsampling = ChromaSubsampling.Samp444 };
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, options));

        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 24);
    }

    [Fact]
    public void Progressive12_Subsampled_RoundTrips()
    {
        var image = JpegImage16.CreateRgb(32, 32, 12, Gradient12(32, 32, 3));
        var options = new JpegEncoderOptions { Quality = Q100, Progressive = true, Subsampling = ChromaSubsampling.Samp420 };
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, options));

        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 64);
    }

    [Fact]
    public void Progressive12_IdentifyReportsProgressive()
    {
        var image = JpegImage16.CreateGrayscale(16, 16, 12, Gradient12(16, 16, 1));
        var info = Jpeg.Identify(Jpeg.Encode16(image, new JpegEncoderOptions { Progressive = true }));

        Assert.Equal(12, info.Precision);
        Assert.True(info.IsProgressive);
    }

    [Fact]
    public void DecodeAny_ReturnsJpegImage16_ForTwelveBitSource()
    {
        var bytes = Jpeg.Encode16(JpegImage16.CreateGrayscale(8, 8, 12, Gradient12(8, 8, 1)));

        IJpegImage image = Jpeg.DecodeAny(bytes);
        Assert.IsType<JpegImage16>(image);
        Assert.Equal(12, image.Precision);
    }

    [Fact]
    public void Identify_ReportsTwelveBitPrecision()
    {
        var bytes = Jpeg.Encode16(JpegImage16.CreateGrayscale(8, 8, 12, Gradient12(8, 8, 1)));
        Assert.Equal(12, Jpeg.Identify(bytes).Precision);
    }

    [Fact]
    public void Decode_RejectsTwelveBitSource()
    {
        var bytes = Jpeg.Encode16(JpegImage16.CreateGrayscale(8, 8, 12, Gradient12(8, 8, 1)));
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes));
    }

    [Fact]
    public void Decode16_RejectsEightBitSource()
    {
        var bytes = Jpeg.Encode(JpegImage.CreateGrayscale(8, 8, new byte[64]));
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode16(bytes));
    }

    [Fact]
    public void Cmyk12_RoundTrips()
    {
        var image = JpegImage16.CreateCmyk(24, 24, 12, Gradient12(24, 24, 4));
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, new JpegEncoderOptions { Quality = Q100 }));

        Assert.Equal(JpegColorSpace.Cmyk, decoded.ColorSpace);
        Assert.Equal(4, decoded.ComponentCount);
        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 6); // direct CMYK, no color transform
    }

    [Fact]
    public void Cmyk12_AsYcck_RoundTrips()
    {
        var image = JpegImage16.CreateCmyk(24, 24, 12, Gradient12(24, 24, 4));
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, new JpegEncoderOptions { Quality = Q100, CmykAsYcck = true }));

        Assert.Equal(JpegColorSpace.Cmyk, decoded.ColorSpace);
        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 24); // YCCK color round-trip
    }

    [Fact]
    public void Cmyk12_Progressive_RoundTrips()
    {
        var image = JpegImage16.CreateCmyk(16, 16, 12, Gradient12(16, 16, 4));
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, new JpegEncoderOptions { Quality = Q100, Progressive = true }));

        Assert.Equal(JpegColorSpace.Cmyk, decoded.ColorSpace);
        Assert.Equal(12, decoded.Precision);
        AssertClose(image.PixelData, decoded.PixelData, 6);
    }

    [Fact]
    public void ToRgba8888_OnDecodedImage_PreviewsWithoutError()
    {
        var image = JpegImage16.CreateGrayscale(8, 8, 12, Gradient12(8, 8, 1));
        var decoded = Jpeg.Decode16(Jpeg.Encode16(image, new JpegEncoderOptions { Quality = Q100 }));

        var preview = decoded.ToRgba8888();
        Assert.Equal(64, preview.Length);
        // Every pixel opaque, R=G=B (grayscale).
        foreach (var px in preview)
        {
            Assert.Equal(255, px & 0xFF);
            Assert.Equal((px >> 24) & 0xFF, (px >> 16) & 0xFF);
        }
    }

    // A smooth ramp keeps DCT ringing low so the round-trip tolerance stays tight.
    private static ushort[] Gradient12(int w, int h, int channels, int precision = 12)
    {
        var max = (1 << precision) - 1;
        var data = new ushort[w * h * channels];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                for (var ch = 0; ch < channels; ch++)
                {
                    var t = (x + y + ch * 5) / (double)(w + h);
                    data[(y * w + x) * channels + ch] = (ushort)Math.Clamp((int)(t * max), 0, max);
                }
        return data;
    }

    private static void AssertClose(ushort[] expected, ushort[] actual, int tolerance)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
            Assert.True(Math.Abs(expected[i] - actual[i]) <= tolerance,
                $"Sample {i}: expected {expected[i]}, got {actual[i]} (tolerance {tolerance}).");
    }
}

using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class ApiContractTests
{
    [Fact]
    public void Encode_NullImage_Throws()
        => Assert.Throws<ArgumentNullException>(() => Jpeg.Encode(null!));

    [Fact]
    public void Decode_NullData_Throws()
        => Assert.Throws<ArgumentNullException>(() => Jpeg.Decode((byte[])null!));

    [Fact]
    public void Identify_NullData_Throws()
        => Assert.Throws<ArgumentNullException>(() => Jpeg.Identify((byte[])null!));

    [Fact]
    public void JpegImage_NullPixels_Throws()
        => Assert.Throws<ArgumentNullException>(() => JpegImage.CreateGrayscale(2, 2, null!));

    [Fact]
    public void JpegImage_WrongLengthPixels_Throws()
        => Assert.Throws<ArgumentException>(() => JpegImage.CreateRgb(2, 2, new byte[10]));

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(200)]
    public void Quality_IsClamped(int requested)
    {
        var options = new JpegEncoderOptions { Quality = requested };
        Assert.InRange(options.Quality, 1, 100);

        // And an extreme value still produces a decodable image.
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, options));
        Assert.Equal(16, decoded.Width);
    }

    [Fact]
    public void BareJpeg_WithoutJfif_DecodesAsYCbCr()
    {
        var image = JpegImage.CreateRgb(24, 24, ColorGradient(24, 24));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp444 });
        var reference = Jpeg.Decode(bytes);

        var stripped = RemoveApp0(bytes);
        Assert.NotEqual(bytes.Length, stripped.Length);

        var decoded = Jpeg.Decode(stripped);
        // No JFIF and no Adobe -> component ids 1,2,3 still imply YCbCr, so result is unchanged.
        Assert.Equal(reference.PixelData, decoded.PixelData);
        Assert.Null(decoded.Metadata!.Density);
    }

    private static byte[] RemoveApp0(byte[] data)
    {
        using var ms = new MemoryStream();
        ms.Write(data, 0, 2); // SOI
        var i = 2;
        while (i < data.Length - 1)
        {
            var code = data[i + 1];
            if (code == 0xDA)
            {
                ms.Write(data, i, data.Length - i);
                break;
            }

            var len = (data[i + 2] << 8) | data[i + 3];
            if (code != 0xE0) // drop APP0
                ms.Write(data, i, 2 + len);
            i += 2 + len;
        }

        return ms.ToArray();
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

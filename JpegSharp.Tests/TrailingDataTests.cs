using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class TrailingDataTests
{
    [Fact]
    public void GarbageAfterEoi_IsIgnored()
    {
        var image = JpegImage.CreateRgb(24, 24, ColorGradient(24, 24));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 });
        var reference = Jpeg.Decode(bytes);

        var rng = new Random(3);
        var trailing = new byte[500];
        rng.NextBytes(trailing);
        var withTrailing = Concat(bytes, trailing);

        var decoded = Jpeg.Decode(withTrailing);
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void FillBytesAfterEoi_AreIgnored()
    {
        var image = JpegImage.CreateGrayscale(16, 16, Gray(16, 16));
        var bytes = Jpeg.Encode(image);
        var reference = Jpeg.Decode(bytes);

        var fill = new byte[64];
        Array.Fill(fill, (byte)0xFF);
        var decoded = Jpeg.Decode(Concat(bytes, fill));
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void TrailingData_OnProgressive_IsIgnored()
    {
        var image = JpegImage.CreateRgb(24, 24, ColorGradient(24, 24));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 82, Progressive = true });
        var reference = Jpeg.Decode(bytes);

        var trailing = new byte[300];
        new Random(9).NextBytes(trailing);
        var decoded = Jpeg.Decode(Concat(bytes, trailing));
        Assert.Equal(reference.PixelData, decoded.PixelData);
    }

    [Fact]
    public void AnotherFullJpegAppended_DecodesTheFirst()
    {
        // Some containers concatenate images; decoding must return the first, ignoring the rest.
        var a = Jpeg.Encode(JpegImage.CreateGrayscale(16, 16, Gray(16, 16)));
        var b = Jpeg.Encode(JpegImage.CreateGrayscale(8, 8, Gray(8, 8)));
        var decoded = Jpeg.Decode(Concat(a, b));
        Assert.Equal(16, decoded.Width);
        Assert.Equal(16, decoded.Height);
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var r = new byte[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }

    private static byte[] Gray(int w, int h)
    {
        var d = new byte[w * h];
        for (var i = 0; i < d.Length; i++)
            d[i] = (byte)((i * 7) % 256);
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

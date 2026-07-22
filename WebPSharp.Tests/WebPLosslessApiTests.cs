using WebPSharp.Api;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Tests;

public class WebPLosslessApiTests
{
    [Fact]
    public void Encode_ProducesValidWebPContainer()
    {
        var img = WebPImage.CreateRgba(4, 4, RandomPixels(4, 4, seed: 3));
        var bytes = WebP.Encode(img);

        // The output must be a recognizable lossless WebP that Identify can read.
        var info = WebP.Identify(bytes);
        Assert.Equal(4, info.Width);
        Assert.Equal(4, info.Height);
        Assert.Equal(WebPFormat.Lossless, info.Format);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(32, 32)]
    [InlineData(17, 9)]
    public void EncodeDecode_RoundTripsPixels(int width, int height)
    {
        var pixels = RandomPixels(width, height, seed: width * 31 + height);
        var img = WebPImage.CreateRgba(width, height, pixels);

        var decoded = WebP.Decode(WebP.Encode(img));

        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.Equal(pixels, decoded.PixelData);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var pixels = RandomPixels(8, 6, seed: 11);
        var img = WebPImage.CreateRgba(8, 6, pixels);
        var path = Path.Combine(Path.GetTempPath(), $"webpsharp_{Guid.NewGuid():N}.webp");
        try
        {
            WebP.Save(img, path);
            var loaded = WebP.Load(path);
            Assert.Equal(pixels, loaded.PixelData);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void EncodeDecode_ViaStream_RoundTrips()
    {
        var pixels = RandomPixels(10, 10, seed: 7);
        var img = WebPImage.CreateRgba(10, 10, pixels);

        using var ms = new MemoryStream();
        WebP.Encode(img, ms);
        ms.Position = 0;
        var decoded = WebP.Decode(ms);
        Assert.Equal(pixels, decoded.PixelData);
    }

    [Fact]
    public void Encode_LossyRequested_Throws()
    {
        var img = WebPImage.CreateRgba(2, 2, new byte[16]);
        Assert.Throws<WebPException>(() => WebP.Encode(img, new WebPEncoderOptions { Lossless = false }));
    }

    [Fact]
    public void Decode_ExceedingMaxPixels_Throws()
    {
        var img = WebPImage.CreateRgba(16, 16, RandomPixels(16, 16, seed: 1));
        var bytes = WebP.Encode(img);
        Assert.Throws<WebPFormatException>(() => WebP.Decode(bytes, new WebPDecoderOptions { MaxPixels = 100 }));
    }

    [Fact]
    public void Decode_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WebP.Decode((byte[])null!));
    }

    private static byte[] RandomPixels(int width, int height, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[width * height * 4];
        rng.NextBytes(pixels);
        return pixels;
    }
}

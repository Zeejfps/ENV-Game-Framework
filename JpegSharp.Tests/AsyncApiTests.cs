using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class AsyncApiTests
{
    [Fact]
    public async Task EncodeAsyncThenDecodeAsync_RoundTrips()
    {
        var pixels = ColorGradient(32, 32);
        var image = JpegImage.CreateRgb(32, 32, pixels);

        using var ms = new MemoryStream();
        await Jpeg.EncodeAsync(image, ms, new JpegEncoderOptions { Quality = 90 });
        ms.Position = 0;

        var decoded = await Jpeg.DecodeAsync(ms);
        Assert.Equal(32, decoded.Width);
        Assert.Equal(32, decoded.Height);
        Assert.Equal(JpegColorSpace.Rgb, decoded.ColorSpace);
    }

    [Fact]
    public async Task EncodeAsync_MatchesSyncBytes()
    {
        var image = JpegImage.CreateRgb(24, 24, ColorGradient(24, 24));
        var options = new JpegEncoderOptions { Quality = 88, Subsampling = ChromaSubsampling.Samp420 };

        var sync = Jpeg.Encode(image, options);
        using var ms = new MemoryStream();
        await Jpeg.EncodeAsync(image, ms, options);
        Assert.Equal(sync, ms.ToArray());
    }

    [Fact]
    public async Task DecodeAsync_MatchesSyncPixels()
    {
        var image = JpegImage.CreateGrayscale(20, 20, Gray(20, 20));
        var bytes = Jpeg.Encode(image);

        var sync = Jpeg.Decode(bytes).PixelData;
        using var ms = new MemoryStream(bytes);
        var async = (await Jpeg.DecodeAsync(ms)).PixelData;
        Assert.Equal(sync, async);
    }

    [Fact]
    public async Task DecodeAsync_HonorsCancellation()
    {
        var image = JpegImage.CreateRgb(16, 16, ColorGradient(16, 16));
        var bytes = Jpeg.Encode(image);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var ms = new MemoryStream(bytes);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await Jpeg.DecodeAsync(ms, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task SaveAsyncThenLoadAsync_FileRoundTrips()
    {
        var image = JpegImage.CreateRgb(32, 24, ColorGradient(32, 24));
        var path = Path.Combine(Path.GetTempPath(), $"jpegsharp_async_{Guid.NewGuid():N}.jpg");
        try
        {
            await Jpeg.SaveAsync(image, path, new JpegEncoderOptions { Quality = 90 });
            Assert.True(File.Exists(path));

            var loaded = await Jpeg.LoadAsync(path);
            Assert.Equal(32, loaded.Width);
            Assert.Equal(24, loaded.Height);
            Assert.Equal(JpegColorSpace.Rgb, loaded.ColorSpace);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
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

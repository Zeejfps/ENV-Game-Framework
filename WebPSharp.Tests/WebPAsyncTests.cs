using WebPSharp.Api;

namespace WebPSharp.Tests;

public class WebPAsyncTests
{
    private static WebPImage Noise(int w, int h, int seed)
    {
        var rng = new Random(seed);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(w, h, pixels);
    }

    [Fact]
    public async Task EncodeAsync_Then_DecodeAsync_RoundTrips()
    {
        var image = Noise(24, 18, 1);
        using var ms = new MemoryStream();
        await WebP.EncodeAsync(image, ms);
        ms.Position = 0;
        var decoded = await WebP.DecodeAsync(ms);
        Assert.Equal(image.PixelData, decoded.PixelData);
    }

    [Fact]
    public async Task SaveAsync_Then_LoadAsync_RoundTrips()
    {
        var image = Noise(16, 12, 2);
        var path = Path.Combine(Path.GetTempPath(), $"webpsharp_async_{Guid.NewGuid():N}.webp");
        try
        {
            await WebP.SaveAsync(image, path);
            var loaded = await WebP.LoadAsync(path);
            Assert.Equal(image.PixelData, loaded.PixelData);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task IdentifyAsync_ReadsInfo()
    {
        var image = Noise(10, 20, 3);
        using var ms = new MemoryStream(WebP.Encode(image));
        var info = await WebP.IdentifyAsync(ms);
        Assert.Equal(10, info.Width);
        Assert.Equal(20, info.Height);
    }

    [Fact]
    public async Task DecodeAsync_CanceledToken_Throws()
    {
        using var ms = new MemoryStream(WebP.Encode(Noise(8, 8, 4)));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await WebP.DecodeAsync(ms, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task EncodeAsync_NullArguments_Throw()
    {
        using var ms = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await WebP.EncodeAsync(null!, ms));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await WebP.EncodeAsync(Noise(2, 2, 1), null!));
    }
}

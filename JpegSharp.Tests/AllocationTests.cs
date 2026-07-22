using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class AllocationTests
{
    [Fact]
    public void Decode_AllocatesProportionalToImageSize()
    {
        var image = JpegImage.CreateRgb(128, 128, Ramp(128 * 128 * 3));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 });

        // Warm up (JIT, static table init) before measuring.
        for (var i = 0; i < 3; i++)
            _ = Jpeg.Decode(bytes);

        var before = GC.GetAllocatedBytesForCurrentThread();
        _ = Jpeg.Decode(bytes);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Output is 128*128*3 = 49152 bytes. A sound decoder allocates a small multiple of
        // that (input buffer, component planes, output) — not tens of thousands of tiny
        // per-block arrays. Guard against gross allocation regressions.
        var outputBytes = 128 * 128 * 3;
        Assert.True(allocated < outputBytes * 20L, $"decode allocated {allocated} bytes (> {outputBytes * 20L})");
    }

    [Fact]
    public void Encode_AllocatesReasonably()
    {
        var image = JpegImage.CreateRgb(128, 128, Ramp(128 * 128 * 3));
        var options = new JpegEncoderOptions { Quality = 85 };

        for (var i = 0; i < 3; i++)
            _ = Jpeg.Encode(image, options);

        var before = GC.GetAllocatedBytesForCurrentThread();
        _ = Jpeg.Encode(image, options);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        var pixelBytes = 128 * 128 * 3;
        Assert.True(allocated < pixelBytes * 40L, $"encode allocated {allocated} bytes (> {pixelBytes * 40L})");
    }

    [Fact]
    public void RepeatedDecode_DoesNotGrowUnbounded()
    {
        var image = JpegImage.CreateGrayscale(64, 64, Ramp(64 * 64));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 });

        for (var i = 0; i < 5; i++)
            _ = Jpeg.Decode(bytes);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10; i++)
            _ = Jpeg.Decode(bytes);
        var perDecode = (GC.GetAllocatedBytesForCurrentThread() - before) / 10;

        // Each independent decode allocates roughly the same amount; no leak/accumulation.
        Assert.True(perDecode < 64 * 64 * 20L, $"per-decode allocation {perDecode} too high");
    }

    private static byte[] Ramp(int length)
    {
        var d = new byte[length];
        for (var i = 0; i < length; i++)
            d[i] = (byte)((i * 17 + 11) % 256);
        return d;
    }
}

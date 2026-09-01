using System.Diagnostics;
using BenchmarkDotNet.Running;
using WebPSharp.Api;
using WebPSharp.Benchmarks;

// `--smoke` runs a quick timing + correctness sanity check without the full BenchmarkDotNet
// harness, so CI can verify the benchmarks and codec execute. Otherwise run the real benchmarks.
if (args.Length > 0 && args[0] == "--smoke")
{
    Smoke.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

internal static class Smoke
{
    public static void Run()
    {
        foreach (var size in new[] { 64, 256, 512 })
        {
            RunOne(size);
        }
        RunAnimation();
        Console.WriteLine("Smoke OK");
    }

    private static void RunOne(int size)
    {
        var pixels = new byte[size * size * 4];
        new Random(1).NextBytes(pixels);
        var image = WebPImage.CreateRgba(size, size, pixels);

        var sw = Stopwatch.StartNew();
        var encoded = WebP.Encode(image);
        sw.Stop();
        var encodeMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        var decoded = WebP.Decode(encoded);
        sw.Stop();

        if (!decoded.PixelData.AsSpan().SequenceEqual(pixels))
            throw new InvalidOperationException($"Round-trip mismatch at {size}x{size}.");

        var ratio = (double)pixels.Length / encoded.Length;
        Console.WriteLine(
            $"{size}x{size}: encode {encodeMs:F1} ms, decode {sw.Elapsed.TotalMilliseconds:F1} ms, " +
            $"{encoded.Length} bytes, ratio {ratio:F2}x");
    }

    private static void RunAnimation()
    {
        var anim = new WebPAnimation(128, 128) { LoopCount = 0 };
        var rng = new Random(2);
        for (var f = 0; f < 8; f++)
        {
            var pixels = new byte[128 * 128 * 4];
            rng.NextBytes(pixels);
            anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(128, 128, pixels), durationMs: 100));
        }

        var sw = Stopwatch.StartNew();
        var encoded = WebP.EncodeAnimation(anim);
        sw.Stop();
        var encodeMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        var decoded = WebP.DecodeAnimation(encoded);
        sw.Stop();

        if (decoded.Frames.Count != 8)
            throw new InvalidOperationException("Animation frame count mismatch.");

        Console.WriteLine(
            $"animation 8x128x128: encode {encodeMs:F1} ms, decode {sw.Elapsed.TotalMilliseconds:F1} ms, " +
            $"{encoded.Length} bytes");
    }
}

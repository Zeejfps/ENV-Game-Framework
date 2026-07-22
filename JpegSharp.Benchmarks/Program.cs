using System.Diagnostics;
using BenchmarkDotNet.Running;
using JpegSharp.Api;
using JpegSharp.Benchmarks;

// `--smoke` runs a quick timing sanity check without the full BenchmarkDotNet harness,
// so CI can verify the benchmarks execute. Otherwise run the real benchmarks.
if (args.Length > 0 && args[0] == "--smoke")
{
    Smoke.Run();
    return;
}

if (args.Length > 0 && args[0] == "--golden")
{
    Golden.Print();
    return;
}

if (args.Length > 0 && args[0] == "--measure")
{
    Measure.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

internal static class Smoke
{
    public static void Run()
    {
        const int size = 256;
        var pixels = new byte[size * size * 3];
        new Random(1).NextBytes(pixels);
        var image = JpegImage.CreateRgb(size, size, pixels);

        var sw = Stopwatch.StartNew();
        var encoded = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85 });
        sw.Stop();
        Console.WriteLine($"Encode {size}x{size}: {sw.Elapsed.TotalMilliseconds:F1} ms, {encoded.Length} bytes");

        sw.Restart();
        var decoded = Jpeg.Decode(encoded);
        sw.Stop();
        Console.WriteLine($"Decode {size}x{size}: {sw.Elapsed.TotalMilliseconds:F1} ms, {decoded.Width}x{decoded.Height}");

        var progressive = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true });
        var pdecoded = Jpeg.Decode(progressive);
        Console.WriteLine($"Progressive round-trip ok: {pdecoded.Width}x{pdecoded.Height}, {progressive.Length} bytes");
    }
}

internal static class Golden
{
    public static void Print()
    {
        foreach (var f in new[] { "gray16_q75", "rgb16_420_q90", "rgb16_444_prog_q90", "cmyk16_q85" })
        {
            var bytes = Encode(f);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            Console.WriteLine($"{f} {bytes.Length} {hash}");
        }
    }

    private static byte[] Encode(string f) => f switch
    {
        "gray16_q75" => Jpeg.Encode(JpegImage.CreateGrayscale(16, 16, GrayGradient(16, 16)), new JpegEncoderOptions { Quality = 75 }),
        "rgb16_420_q90" => Jpeg.Encode(JpegImage.CreateRgb(16, 16, RgbGradient(16, 16)), new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp420 }),
        "rgb16_444_prog_q90" => Jpeg.Encode(JpegImage.CreateRgb(16, 16, RgbGradient(16, 16)), new JpegEncoderOptions { Quality = 90, Subsampling = ChromaSubsampling.Samp444, Progressive = true }),
        "cmyk16_q85" => Jpeg.Encode(JpegImage.CreateCmyk(16, 16, CmykGradient(16, 16)), new JpegEncoderOptions { Quality = 85 }),
        _ => throw new ArgumentException(f),
    };

    private static byte[] GrayGradient(int w, int h)
    {
        var d = new byte[w * h];
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) d[y * w + x] = (byte)((x * 255 / (w - 1) + y * 255 / (h - 1)) / 2);
        return d;
    }

    private static byte[] RgbGradient(int w, int h)
    {
        var d = new byte[w * h * 3];
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) { var i = (y * w + x) * 3; d[i] = (byte)(x * 255 / (w - 1)); d[i + 1] = (byte)(y * 255 / (h - 1)); d[i + 2] = (byte)((x + y) * 255 / (w + h - 2)); }
        return d;
    }

    private static byte[] CmykGradient(int w, int h)
    {
        var d = new byte[w * h * 4];
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) { var i = (y * w + x) * 4; d[i] = (byte)(x * 255 / (w - 1)); d[i + 1] = (byte)(y * 255 / (h - 1)); d[i + 2] = (byte)((x + y) * 255 / (w + h - 2)); d[i + 3] = (byte)(255 - x * 255 / (w - 1)); }
        return d;
    }
}

// A self-contained throughput/allocation measurement that does not rely on BenchmarkDotNet's
// project discovery (robust in any working tree). Reports median-of-N timings.
internal static class Measure
{
    public static void Run()
    {
        Console.WriteLine($"{"Op",-22}{"Size",-8}{"Median ms",-12}{"MP/s",-10}{"Alloc KB",-10}");
        foreach (var size in new[] { 64, 256, 512 })
        {
            var pixels = MakeImage(size);
            var image = JpegImage.CreateRgb(size, size, pixels);
            var opts = new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 };
            var encoded = Jpeg.Encode(image, opts);

            Report("Encode baseline 4:2:0", size, () => Jpeg.Encode(image, opts));
            Report("Decode baseline", size, () => Jpeg.Decode(encoded));

            var prog = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Progressive = true });
            Report("Decode progressive", size, () => Jpeg.Decode(prog));
        }
    }

    private static void Report(string op, int size, Action action)
    {
        for (var i = 0; i < 3; i++)
            action(); // warmup

        const int runs = 9;
        var times = new double[runs];
        var sw = new System.Diagnostics.Stopwatch();
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < runs; i++)
        {
            sw.Restart();
            action();
            sw.Stop();
            times[i] = sw.Elapsed.TotalMilliseconds;
        }

        var allocPerRun = (GC.GetAllocatedBytesForCurrentThread() - allocBefore) / runs / 1024.0;
        Array.Sort(times);
        var median = times[runs / 2];
        var mpps = size * (double)size / 1_000_000.0 / (median / 1000.0);
        Console.WriteLine($"{op,-22}{size + "x" + size,-8}{median,-12:F2}{mpps,-10:F1}{allocPerRun,-10:F0}");
    }

    private static byte[] MakeImage(int size)
    {
        var d = new byte[size * size * 3];
        var rng = new Random(1);
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var i = (y * size + x) * 3;
                d[i] = (byte)((x * 255 / size + rng.Next(20)) & 0xFF);
                d[i + 1] = (byte)((y * 255 / size + rng.Next(20)) & 0xFF);
                d[i + 2] = (byte)(((x + y) * 127 / size + rng.Next(20)) & 0xFF);
            }
        return d;
    }
}

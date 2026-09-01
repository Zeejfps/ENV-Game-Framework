using System.Diagnostics;
using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

/// <summary>
/// Interop regression tests that shell out to libwebp's <c>dwebp</c> to confirm WebPSharp's lossless
/// output is decodable by a third-party decoder — the gate that self-round-trip tests cannot provide.
/// Each test is skipped (returns early) when <c>dwebp</c> is not on the machine, so the suite stays
/// green on hosts without libwebp installed.
/// </summary>
/// <remarks>
/// These cover the VP8L transform sub-image emission (predictor mode image, cross-color image). A
/// prior bug wrote a spurious meta-Huffman present bit into every sub-image; WebPSharp's own decoder
/// mirrored the mistake so round-trips passed, but <c>dwebp</c> rejected the stream with a bitstream
/// error. The default lossless encoder selects the predictor transform for spatially-correlated
/// images, so this exercises the default path end-to-end through dwebp.
/// </remarks>
public sealed class Vp8LDwebpInteropTests
{
    [Fact]
    public void DefaultLossless_SpatiallyCorrelated_DecodedByDwebp()
    {
        var dwebp = TryGetToolPath("dwebp");
        if (dwebp is null)
            return; // dwebp unavailable: skip the interop gate.

        const int w = 64, h = 64;
        var pixels = Gradient(w, h);
        var image = WebPImage.CreateRgb(w, h, pixels);

        // Default lossless: EncodeBest at Effort 4 picks the predictor transform for this content.
        var webp = WebP.Encode(image, new WebPEncoderOptions { Lossless = true });

        AssertDwebpDecodesLossless(dwebp, webp, w, h, pixels);
    }

    [Fact]
    public void PredictorTransform_DecodedByDwebp()
    {
        var dwebp = TryGetToolPath("dwebp");
        if (dwebp is null)
            return;

        const int w = 48, h = 40;
        var pixels = Gradient(w, h);
        var image = WebPImage.CreateRgb(w, h, pixels);

        // Force the predictor path directly so the sub-image emission is exercised regardless of the
        // heuristic candidate selection in EncodeBest.
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = 2 });
        var webp = WrapVp8L(payload);

        AssertDwebpDecodesLossless(dwebp, webp, w, h, pixels);
    }

    [Fact]
    public void PredictorAndCrossColorTransforms_DecodedByDwebp()
    {
        var dwebp = TryGetToolPath("dwebp");
        if (dwebp is null)
            return;

        const int w = 40, h = 32;
        var pixels = Gradient(w, h);
        var image = WebPImage.CreateRgb(w, h, pixels);

        // Cross-color shares the sub-image emission branch with the predictor; chain both so the
        // stream carries two transform sub-images.
        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings
        {
            Lz77 = true,
            Predictor = true,
            PredictorMode = 2,
            CrossColor = true,
        });
        var webp = WrapVp8L(payload);

        AssertDwebpDecodesLossless(dwebp, webp, w, h, pixels);
    }

    private static void AssertDwebpDecodesLossless(string dwebp, byte[] webp, int w, int h, byte[] expectedRgb)
    {
        var dir = Path.Combine(Path.GetTempPath(), "webpsharp-dwebp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var inPath = Path.Combine(dir, "in.webp");
            var outPath = Path.Combine(dir, "out.ppm");
            File.WriteAllBytes(inPath, webp);

            var (exit, stderr) = Run(dwebp, $"\"{inPath}\" -ppm -o \"{outPath}\"");
            Assert.True(exit == 0, $"dwebp rejected WebPSharp output (exit {exit}): {stderr}");

            var (gotW, gotH, rgb) = ReadPpm(File.ReadAllBytes(outPath));
            Assert.Equal(w, gotW);
            Assert.Equal(h, gotH);

            var maxDiff = 0;
            for (var i = 0; i < expectedRgb.Length; i++)
                maxDiff = Math.Max(maxDiff, Math.Abs(expectedRgb[i] - rgb[i]));
            Assert.Equal(0, maxDiff); // lossless: dwebp must reproduce the source exactly.
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static byte[] Gradient(int w, int h)
    {
        var px = new byte[w * h * 3];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var o = (y * w + x) * 3;
            px[o] = (byte)(x * 4);
            px[o + 1] = (byte)(y * 4);
            px[o + 2] = (byte)((x + y) * 2);
        }
        return px;
    }

    // Wraps a raw VP8L chunk payload in a minimal RIFF/WEBP container so dwebp can read it.
    private static byte[] WrapVp8L(byte[] payload)
    {
        var chunkSize = payload.Length;
        var padded = chunkSize + (chunkSize & 1);
        var riffSize = 4 + 8 + padded; // "WEBP" + chunk header + padded payload
        var bytes = new byte[8 + riffSize];
        var w = 0;
        void PutStr(string s) { foreach (var c in s) bytes[w++] = (byte)c; }
        void PutU32(int v) { bytes[w++] = (byte)v; bytes[w++] = (byte)(v >> 8); bytes[w++] = (byte)(v >> 16); bytes[w++] = (byte)(v >> 24); }

        PutStr("RIFF");
        PutU32(riffSize);
        PutStr("WEBP");
        PutStr("VP8L");
        PutU32(chunkSize);
        Array.Copy(payload, 0, bytes, w, payload.Length);
        return bytes;
    }

    private static (int W, int H, byte[] Rgb) ReadPpm(byte[] data)
    {
        // Header: "P6\n<w> <h>\n<maxval>\n<binary rgb>", whitespace-separated tokens.
        var pos = 0;
        string Token()
        {
            while (pos < data.Length && (data[pos] == ' ' || data[pos] == '\n' || data[pos] == '\r' || data[pos] == '\t'))
                pos++;
            var start = pos;
            while (pos < data.Length && data[pos] != ' ' && data[pos] != '\n' && data[pos] != '\r' && data[pos] != '\t')
                pos++;
            return System.Text.Encoding.ASCII.GetString(data, start, pos - start);
        }

        var magic = Token();
        Assert.Equal("P6", magic);
        var width = int.Parse(Token());
        var height = int.Parse(Token());
        _ = Token(); // maxval
        pos++; // single whitespace after maxval precedes the binary body
        var rgb = new byte[width * height * 3];
        Array.Copy(data, pos, rgb, 0, rgb.Length);
        return (width, height, rgb);
    }

    private static (int Exit, string Stderr) Run(string tool, string args)
    {
        var psi = new ProcessStartInfo(tool, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var stderr = p.StandardError.ReadToEnd();
        p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stderr);
    }

    // Resolves a CLI tool from PATH (and a couple of common Homebrew locations), returning null when
    // it cannot be found so the caller can skip. Never throws.
    private static string? TryGetToolPath(string tool)
    {
        var candidates = new List<string>();
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(dir))
                continue;
            candidates.Add(Path.Combine(dir, tool));
        }
        candidates.Add(Path.Combine("/opt/homebrew/bin", tool));
        candidates.Add(Path.Combine("/usr/local/bin", tool));

        foreach (var c in candidates)
        {
            try
            {
                if (File.Exists(c))
                    return c;
            }
            catch
            {
                // ignore malformed PATH entries
            }
        }
        return null;
    }
}

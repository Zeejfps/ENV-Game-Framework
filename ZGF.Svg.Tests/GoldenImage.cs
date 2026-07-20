using System.Runtime.CompilerServices;
using PngSharp.Api;

namespace ZGF.Svg.Tests;

/// <summary>
/// Golden-image comparison against PNGs checked into TestAssets/Golden.
/// A pixel counts as differing when any channel is off by more than
/// <see cref="ChannelTolerance"/>; the image fails when more than
/// <see cref="MaxDifferingPixelFraction"/> of pixels differ. The two knobs are
/// orthogonal: the first absorbs antialiasing noise, the second absorbs the
/// handful of pixels where a curve-flattening decision lands the other way.
/// Set ZGF_SVG_REGEN_GOLDENS=1 to rewrite the goldens in the source tree
/// (the run still fails so a regen can't silently pass CI).
/// On mismatch, *.actual.png and *.diff.png are written next to the golden.
/// </summary>
internal static class GoldenImage
{
    private const int ChannelTolerance = 2;
    private const double MaxDifferingPixelFraction = 0.005;

    public static bool RegenRequested =>
        Environment.GetEnvironmentVariable("ZGF_SVG_REGEN_GOLDENS") == "1";

    public static string GoldenDirectory { get; } =
        Path.Combine(SourceDirectory(), "TestAssets", "Golden");

    public static string IconsDirectory { get; } =
        Path.Combine(SourceDirectory(), "TestAssets", "Icons");

    public static void AssertMatches(byte[] actualRgba, int w, int h, string name)
    {
        var goldenPath = Path.Combine(GoldenDirectory, name + ".png");

        if (RegenRequested)
        {
            Directory.CreateDirectory(GoldenDirectory);
            Png.EncodeToFile(Png.CreateRgba(w, h, actualRgba), goldenPath);
            Assert.Fail($"Regenerated golden {name}.png — rerun without ZGF_SVG_REGEN_GOLDENS to verify.");
        }

        if (!File.Exists(goldenPath))
            Assert.Fail($"Golden {name}.png missing. Run with ZGF_SVG_REGEN_GOLDENS=1 to create it.");

        var golden = Png.DecodeFromFile(goldenPath);
        if (golden.Ihdr.Width != w || golden.Ihdr.Height != h)
            Assert.Fail($"Golden {name}.png is {golden.Ihdr.Width}x{golden.Ihdr.Height}, actual is {w}x{h}.");

        var goldenRgba = golden.PixelData;
        var differing = 0;
        var maxDelta = 0;
        for (var i = 0; i < actualRgba.Length; i += 4)
        {
            var pixelDelta = 0;
            for (var c = 0; c < 4; c++)
                pixelDelta = Math.Max(pixelDelta, Math.Abs(actualRgba[i + c] - goldenRgba[i + c]));
            if (pixelDelta > ChannelTolerance)
                differing++;
            maxDelta = Math.Max(maxDelta, pixelDelta);
        }

        var totalPixels = w * h;
        if (differing <= totalPixels * MaxDifferingPixelFraction)
            return;

        WriteFailureArtifacts(actualRgba, goldenRgba, w, h, name);
        Assert.Fail(
            $"Golden mismatch for {name}: {differing}/{totalPixels} pixels exceed the per-channel tolerance of " +
            $"{ChannelTolerance}, max channel delta {maxDelta} (allowed: {MaxDifferingPixelFraction:P1} of pixels). " +
            $"See {name}.actual.png / {name}.diff.png in TestAssets/Golden.");
    }

    private static void WriteFailureArtifacts(byte[] actual, byte[] golden, int w, int h, string name)
    {
        Directory.CreateDirectory(GoldenDirectory);
        Png.EncodeToFile(Png.CreateRgba(w, h, actual), Path.Combine(GoldenDirectory, name + ".actual.png"));

        var diff = new byte[actual.Length];
        for (var p = 0; p < w * h; p++)
        {
            var i = p * 4;
            var delta = 0;
            for (var c = 0; c < 4; c++)
                delta = Math.Max(delta, Math.Abs(actual[i + c] - golden[i + c]));
            // White where identical, red ramp where different.
            diff[i] = 255;
            diff[i + 1] = (byte)(delta == 0 ? 255 : Math.Max(0, 255 - delta * 8));
            diff[i + 2] = diff[i + 1];
            diff[i + 3] = 255;
        }
        Png.EncodeToFile(Png.CreateRgba(w, h, diff), Path.Combine(GoldenDirectory, name + ".diff.png"));
    }

    private static string SourceDirectory([CallerFilePath] string thisFile = "")
        => Path.GetDirectoryName(thisFile)!;
}

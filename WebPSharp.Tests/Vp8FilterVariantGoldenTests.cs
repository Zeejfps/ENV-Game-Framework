using WebPSharp.Api;

namespace WebPSharp.Tests;

// Golden coverage for VP8 lossy loop-filter decode branches not exercised by the default
// (complex/normal, sharpness=0, single effective segment) Vp8GoldenBatchTests cases:
//   - the SIMPLE loop filter (filter_type == 1),
//   - a non-zero filter SHARPNESS,
//   - an explicit multi-SEGMENT stream.
//
// Assets are real cwebp 1.6.0 output (VP8 lossy, -q 75); references are dwebp 1.6.0 default
// (fancy chroma upsampling) PAM output with the header stripped to raw RGBA — the same
// convention the committed case_*.rgba goldens use (verified: regenerating case_1 this way is
// byte-identical). WebP.Decode uses fancy upsampling by default, so the <=1 tolerance is for
// RGB rounding, matching Vp8GoldenBatchTests.
public class Vp8FilterVariantGoldenTests
{
    private static string Asset(string a) => Path.Combine(AppContext.BaseDirectory, "Assets", a);

    private static void AssertGolden(string name, int expectedWidth, int expectedHeight)
    {
        var webp = File.ReadAllBytes(Asset($"{name}.webp"));
        var reference = File.ReadAllBytes(Asset($"{name}.rgba"));
        var image = WebP.Decode(webp);

        Assert.Equal(expectedWidth, image.Width);
        Assert.Equal(expectedHeight, image.Height);
        Assert.Equal(reference.Length, image.PixelData.Length);

        var max = 0;
        long sum = 0;
        for (var i = 0; i < reference.Length; i++)
        {
            var dd = Math.Abs(image.PixelData[i] - reference[i]);
            if (dd > max) max = dd;
            sum += dd;
        }
        Assert.True(max <= 1, $"{name}: maxDiff={max}, meanDiff={(double)sum / reference.Length:F3}");
    }

    // SIMPLE loop filter. Verified via `webpinfo -bitstream_info vp8_simplefilter.webp`:
    // "Simple filter: 1" (filter_type == 1), "Level: 30", "Sharpness: 0". Encoded with
    // `cwebp -q 75 -nostrong`. Exercises the simple-filter decode path (Vp8Decoder simple
    // edge filtering) rather than the complex/normal filter the case_* goldens all use.
    [Fact]
    public void DecodesSimpleFilter_MatchesDwebp() => AssertGolden("vp8_simplefilter", 128, 128);

    // Non-zero filter SHARPNESS. Verified via `webpinfo -bitstream_info vp8_sharpness.webp`:
    // "Sharpness: 7", "Simple filter: 0" (complex filter). Encoded with `cwebp -q 75 -sharpness 7`.
    // Exercises the sharpness>0 interior-limit adjustment in the loop filter (all case_* goldens
    // are sharpness 0).
    [Fact]
    public void DecodesHighSharpness_MatchesDwebp() => AssertGolden("vp8_sharpness", 128, 128);

    // Multi-SEGMENT stream. Verified via `webpinfo -bitstream_info vp8_segmented.webp`:
    // "Use segment: 1", "Update map: 1", per-segment "Quantizer: 28 21 15 15" (distinct segment
    // quantizers) and "Prob segment: 239 230 255". Encoded with `cwebp -q 75 -m 6 -segments 4`.
    // Exercises per-segment quantizer/filter selection and the segment-map decode path.
    [Fact]
    public void DecodesMultiSegment_MatchesDwebp() => AssertGolden("vp8_segmented", 128, 128);
}

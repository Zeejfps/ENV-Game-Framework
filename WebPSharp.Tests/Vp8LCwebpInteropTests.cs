using WebPSharp.Api;

namespace WebPSharp.Tests;

/// <summary>
/// Golden interop tests for VP8L (lossless) decoding against libwebp 1.6.0.
/// Every asset here is produced by <c>cwebp -lossless -z 9 -m 6</c> (the reference
/// encoder), NOT by the WebPSharp encoder, so it exercises decoder paths that
/// WebPSharp self-round-trip tests can never reach: libwebp-chosen predictor /
/// color-indexing transforms, color cache, meta-Huffman entropy images, Huffman
/// repeat-codes (16/17/18), and LZ77 back-references.
///
/// The <c>.rgba</c> reference for each asset is the raw RGBA output of
/// <c>dwebp -pam</c> (P7 header stripped). Lossless decoding is EXACT, so every
/// case asserts <c>maxDiff == 0</c> (byte-identical), never an approximate bound.
///
/// Feature profile per asset (webpinfo reports only the first transform; the
/// color-cache bits and meta-Huffman group counts below were VERIFIED by
/// parsing the decoded VP8L bitstream headers directly):
///   cwebp_ll_predictor   64x64   Predictor transform (block size 4)
///   cwebp_ll_palette     64x64   Color Indexing transform, 16 colors (pixel bundling)
///   cwebp_ll_colorcache  64x64   Color cache: color_cache_bits = 10 (1024 entries),
///                                3396 cache-index lookups; NO transforms (the color
///                                cache alone carries the repeats), no palette (300
///                                distinct colors drawn in pseudo-random order so LZ77
///                                cannot match)
///   cwebp_ll_metahuffman 256x256 num_htree_groups = 2 (real meta-Huffman entropy
///                                image), color_cache_bits = 3; Predictor + cross-color
///                                (no subtract-green) over a four-region image (noise /
///                                gradient / flat / stripes) whose distinct per-region
///                                statistics force cwebp to split Huffman codes per tile
///   cwebp_ll_photo       128x128 Predictor transform (block size 4); plasma gradient
///   cwebp_ll_alpha       64x64   Predictor transform + intrinsic VP8L alpha channel
/// </summary>
public class Vp8LCwebpInteropTests
{
    private static string Asset(string a) => Path.Combine(AppContext.BaseDirectory, "Assets", a);

    private static void AssertDecodesExact(string name, int width, int height)
    {
        var webp = File.ReadAllBytes(Asset($"{name}.webp"));
        var reference = File.ReadAllBytes(Asset($"{name}.rgba"));

        var image = WebP.Decode(webp);

        Assert.Equal(WebPColorFormat.Rgba, image.Format);
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.Equal(width * height * 4, image.PixelData.Length);
        Assert.Equal(reference.Length, image.PixelData.Length);

        var max = 0;
        var firstDiff = -1;
        for (var i = 0; i < reference.Length; i++)
        {
            var d = Math.Abs(image.PixelData[i] - reference[i]);
            if (d > max) max = d;
            if (d != 0 && firstDiff < 0) firstDiff = i;
        }

        // Lossless decode must be byte-exact with the dwebp reference.
        Assert.True(max == 0,
            $"{name}: maxDiff={max}, first diff at byte {firstDiff} " +
            (firstDiff >= 0 ? $"(pixel {firstDiff / 4}, channel {firstDiff % 4}: got {image.PixelData[firstDiff]}, expected {reference[firstDiff]})" : ""));
    }

    // Predictor transform (spatial prediction).
    [Fact]
    public void Decode_CwebpLossless_Predictor() => AssertDecodesExact("cwebp_ll_predictor", 64, 64);

    // Color Indexing transform with 16-color palette (pixel bundling).
    [Fact]
    public void Decode_CwebpLossless_Palette() => AssertDecodesExact("cwebp_ll_palette", 64, 64);

    // Color cache: color_cache_bits = 10 with 3396 cache-index lookups (verified via
    // bitstream parse), no palette transform.
    [Fact]
    public void Decode_CwebpLossless_ColorCache() => AssertDecodesExact("cwebp_ll_colorcache", 64, 64);

    // Real meta-Huffman entropy image: num_htree_groups = 2 (verified via bitstream parse),
    // exercising the per-tile Huffman-group selection path the WebPSharp encoder never emits.
    [Fact]
    public void Decode_CwebpLossless_MetaHuffman() => AssertDecodesExact("cwebp_ll_metahuffman", 256, 256);

    // Plasma gradient: Predictor transform over a photographic-style image.
    [Fact]
    public void Decode_CwebpLossless_Photo() => AssertDecodesExact("cwebp_ll_photo", 128, 128);

    // Intrinsic VP8L alpha channel (cross-check for alpha reporting).
    [Fact]
    public void Decode_CwebpLossless_Alpha() => AssertDecodesExact("cwebp_ll_alpha", 64, 64);
}

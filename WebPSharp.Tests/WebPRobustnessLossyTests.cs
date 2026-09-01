using WebPSharp.Api;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Tests;

/// <summary>
/// Extends the decoder robustness contract (audit TH-02, §20) beyond lossless (VP8L) to the
/// lossy VP8, ALPH (alpha), VP8X (extended) and animation (ANIM/ANMF) decode paths. The contract
/// is identical to <see cref="WebPRobustnessTests"/>: ANY malformed/truncated/corrupted input must
/// escape the public decode API as ONLY a <see cref="WebPException"/>-family exception — never a
/// raw ArgumentException, IndexOutOfRange, overflow, null-ref, or a hang.
/// </summary>
public class WebPRobustnessLossyTests
{
    private static readonly WebPDecoderOptions SmallLimit = new() { MaxPixels = 1_000_000 };

    private static string Asset(string a) => Path.Combine(AppContext.BaseDirectory, "Assets", a);

    // The lossy VP8 seed corpus: real committed VP8 (fourcc 'VP8 ') files.
    private static readonly string[] Vp8Seeds =
    {
        "case_1.webp", "case_2.webp", "case_3.webp", "case_4.webp",
        "case_5.webp", "case_6.webp", "case_7.webp", "case_8.webp",
        "grad_q80.webp", "lossy_filtered.webp",
    };

    // Both public single-image inspection entry points must honour the contract on the same input:
    // WebP.Decode (auto-detects VP8/VP8L/VP8X) and WebP.Identify (RIFF + header parse only).
    private static void AssertCleanDecodeAndIdentify(byte[] data, string context)
    {
        try
        {
            WebP.Decode(data, SmallLimit);
        }
        catch (WebPException)
        {
        }
        catch (Exception ex)
        {
            Assert.Fail($"Decode: unexpected {ex.GetType().Name} [{context}]: {ex.Message}\n{ex.StackTrace}");
        }

        try
        {
            WebP.Identify(data);
        }
        catch (WebPException)
        {
        }
        catch (Exception ex)
        {
            Assert.Fail($"Identify: unexpected {ex.GetType().Name} [{context}]: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void AssertCleanAnimation(byte[] data, string context)
    {
        try
        {
            WebP.DecodeAnimation(data, SmallLimit);
        }
        catch (WebPException)
        {
        }
        catch (Exception ex)
        {
            Assert.Fail($"DecodeAnimation: unexpected {ex.GetType().Name} [{context}]: {ex.Message}\n{ex.StackTrace}");
        }

        // The animated container must also stay clean through the single-image API and Identify.
        AssertCleanDecodeAndIdentify(data, context);
    }

    // Shared mutation strategy, matching WebPRobustnessTests: truncation at every length,
    // deterministic single-bit flips, and deterministic multi-byte overwrites past the header.
    private static void FuzzSeed(byte[] valid, int seed, string name, Action<byte[], string> assert)
    {
        for (var len = 0; len < valid.Length; len++)
            assert(valid.AsSpan(0, len).ToArray(), $"{name}:truncate@{len}");

        var flipRng = new Random(seed * 31 + 7);
        for (var trial = 0; trial < 400; trial++)
        {
            var mutated = (byte[])valid.Clone();
            var idx = flipRng.Next(mutated.Length);
            var bit = flipRng.Next(8);
            mutated[idx] ^= (byte)(1 << bit);
            assert(mutated, $"{name}:flip idx={idx} bit={bit} trial={trial}");
        }

        var mbRng = new Random(seed * 131 + 13);
        for (var trial = 0; trial < 200; trial++)
        {
            var mutated = (byte[])valid.Clone();
            // Corrupt from a point past the RIFF/WEBP header to hammer the bitstream parsers.
            var start = mutated.Length > 20 ? mbRng.Next(12, mutated.Length) : 0;
            for (var i = start; i < mutated.Length; i++)
                mutated[i] = (byte)mbRng.Next(256);
            assert(mutated, $"{name}:multibyte start={start} trial={trial}");
        }
    }

    // ---- Lossy VP8 -------------------------------------------------------------------------

    [Theory]
    [InlineData("case_1.webp")]
    [InlineData("case_2.webp")]
    [InlineData("case_3.webp")]
    [InlineData("case_4.webp")]
    [InlineData("case_5.webp")]
    [InlineData("case_6.webp")]
    [InlineData("case_7.webp")]
    [InlineData("case_8.webp")]
    [InlineData("grad_q80.webp")]
    [InlineData("lossy_filtered.webp")]
    public void FuzzLossyVp8_FailsCleanly(string file)
    {
        var valid = File.ReadAllBytes(Asset(file));
        FuzzSeed(valid, file.GetHashCode(), file, AssertCleanDecodeAndIdentify);
    }

    // ---- ALPH (alpha) ----------------------------------------------------------------------

    [Fact]
    public void FuzzAlpha_Vp8xAlphAsset_FailsCleanly()
    {
        // alpha_q80.webp is a VP8X container carrying an ALPH alpha chunk + VP8 image.
        var valid = File.ReadAllBytes(Asset("alpha_q80.webp"));
        FuzzSeed(valid, 0x5150, "alpha_q80", AssertCleanDecodeAndIdentify);
    }

    [Fact]
    public void FuzzAlpha_LossyPlusAlphaEncoded_FailsCleanly()
    {
        // Encode an RGBA image lossily: yields VP8X + ALPH + VP8, exercising the VP8+ALPH combo.
        var rng = new Random(717);
        var pixels = new byte[20 * 16 * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(20, 16, pixels);
        var valid = WebP.Encode(image, new WebPEncoderOptions { Lossless = false, Quality = 80 });
        FuzzSeed(valid, 0x414C, "lossy+alph", AssertCleanDecodeAndIdentify);
    }

    // ---- VP8X (extended) -------------------------------------------------------------------

    [Fact]
    public void FuzzVp8x_WithMetadata_FailsCleanly()
    {
        // Lossless RGBA + ICC/EXIF/XMP metadata yields VP8X + ICCP + VP8L + EXIF + XMP: fuzz the
        // VP8X header, flag byte, canvas dims and the surrounding chunk layout.
        var rng = new Random(919);
        var pixels = new byte[18 * 14 * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(18, 14, pixels);
        image.Metadata = new WebPMetadata
        {
            IccProfile = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            Exif = new byte[] { 9, 8, 7, 6 },
            Xmp = new byte[] { 0x3C, 0x78, 0x3E },
        };
        var valid = WebP.Encode(image);
        FuzzSeed(valid, 0x5638, "vp8x+meta", AssertCleanDecodeAndIdentify);
    }

    // ---- Animation (ANIM / ANMF) -----------------------------------------------------------

    [Fact]
    public void FuzzAnimation_MultiFrame_FailsCleanly()
    {
        // A multi-frame animation with a sub-canvas frame + alpha exercises the ANIM/ANMF parser,
        // where a raw-exception overflow bug once lived in the frame-header handling.
        var anim = new WebPAnimation(16, 12) { LoopCount = 2, BackgroundColor = 0x80402010 };
        var p0 = new byte[16 * 12 * 4];
        new Random(31).NextBytes(p0);
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(16, 12, p0)));
        var p1 = new byte[6 * 4 * 4];
        new Random(32).NextBytes(p1);
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(6, 4, p1), 4, 2, 40));
        var valid = WebP.EncodeAnimation(anim);

        FuzzSeed(valid, 0x414E, "anim", AssertCleanAnimation);
    }

    [Fact]
    public void FuzzAnimation_TargetedFrameHeaderMutation_FailsCleanly()
    {
        // Concentrate multi-byte mutations across ANMF frame-header regions (24-bit x/y/w/h fields
        // and inner sub-chunk sizes) — the class of corruption that produced the ANMF overflow.
        var anim = new WebPAnimation(24, 20) { LoopCount = 0 };
        var p0 = new byte[24 * 20 * 4];
        new Random(41).NextBytes(p0);
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(24, 20, p0)));
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(8, 8, new byte[8 * 8 * 4]), 4, 4, 25));
        var valid = WebP.EncodeAnimation(anim);

        var rng = new Random(0xADF);
        for (var trial = 0; trial < 3000; trial++)
        {
            var mutated = (byte[])valid.Clone();
            // Overwrite a short run (1-6 bytes) at a random offset past the RIFF header.
            var start = rng.Next(12, mutated.Length);
            var run = Math.Min(rng.Next(1, 7), mutated.Length - start);
            for (var i = 0; i < run; i++)
                mutated[start + i] = (byte)rng.Next(256);
            AssertCleanAnimation(mutated, $"anmf-run start={start} run={run} trial={trial}");
        }
    }
}

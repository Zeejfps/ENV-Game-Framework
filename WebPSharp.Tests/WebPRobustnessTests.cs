using WebPSharp.Api;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Tests;

public class WebPRobustnessTests
{
    private static byte[] ValidLosslessWebP(int seed = 1)
    {
        var rng = new Random(seed);
        var pixels = new byte[24 * 18 * 4];
        rng.NextBytes(pixels);
        // Use transforms + LZ77 so the stream exercises many decoder paths.
        var image = WebPImage.CreateRgba(24, 18, pixels);
        return WebP.Encode(image);
    }

    private static readonly WebPDecoderOptions SmallLimit = new() { MaxPixels = 1_000_000 };

    // A corrupt/garbage input must fail cleanly with a WebP exception (or, rarely, decode) — never
    // an IndexOutOfRange, overflow, null-ref, or a hang.
    private static void AssertCleanFailureOrSuccess(byte[] data)
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
            Assert.Fail($"Unexpected exception type {ex.GetType().Name}: {ex.Message}");
        }
    }

    [Fact]
    public void Fuzz_TruncationAtEveryLength_FailsCleanly()
    {
        var valid = ValidLosslessWebP();
        for (var len = 0; len < valid.Length; len++)
            AssertCleanFailureOrSuccess(valid.AsSpan(0, len).ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Fuzz_SingleByteFlips_FailCleanly(int seed)
    {
        var valid = ValidLosslessWebP(seed);
        var rng = new Random(seed * 31);
        for (var trial = 0; trial < 2000; trial++)
        {
            var mutated = (byte[])valid.Clone();
            var idx = rng.Next(mutated.Length);
            mutated[idx] ^= (byte)(1 << rng.Next(8));
            AssertCleanFailureOrSuccess(mutated);
        }
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    public void Fuzz_MultiByteCorruption_FailsCleanly(int seed)
    {
        var valid = ValidLosslessWebP(seed);
        var rng = new Random(seed);
        for (var trial = 0; trial < 1000; trial++)
        {
            var mutated = (byte[])valid.Clone();
            // Corrupt everything after the RIFF/WEBP/chunk header to hammer the VP8L decoder.
            var start = rng.Next(20, mutated.Length);
            for (var i = start; i < mutated.Length; i++)
                mutated[i] = (byte)rng.Next(256);
            AssertCleanFailureOrSuccess(mutated);
        }
    }

    [Fact]
    public void Fuzz_RandomGarbage_FailsCleanly()
    {
        var rng = new Random(99);
        for (var trial = 0; trial < 2000; trial++)
        {
            var data = new byte[rng.Next(0, 200)];
            rng.NextBytes(data);
            // Occasionally start with a valid RIFF/WEBP header to reach deeper code.
            if (data.Length >= 16 && trial % 2 == 0)
            {
                "RIFF"u8.CopyTo(data);
                "WEBP"u8.CopyTo(data.AsSpan(8));
            }
            AssertCleanFailureOrSuccess(data);
        }
    }

    [Fact]
    public void CorruptVp8L_BadVersion_Throws()
    {
        var valid = ValidLosslessWebP();
        // The VP8L payload starts after RIFF(12) + chunk header(8) = offset 20; byte 0 is 0x2F,
        // then dims; the version bits are the top 3 bits of the 5th payload byte.
        var mutated = (byte[])valid.Clone();
        mutated[20 + 4] |= 0xE0; // force a non-zero version
        Assert.Throws<WebPFormatException>(() => WebP.Decode(mutated));
    }

    [Fact]
    public void OversizedDimensions_RejectedByMaxPixels()
    {
        var valid = ValidLosslessWebP();
        Assert.Throws<WebPFormatException>(
            () => WebP.Decode(valid, new WebPDecoderOptions { MaxPixels = 4 }));
    }

    [Fact]
    public void CorruptAnimation_MissingImageChunk_Throws()
    {
        // Build a valid animation, then corrupt the inner VP8L fourcc so a frame has no image.
        var anim = new WebPAnimation(8, 8);
        var pixels = new byte[8 * 8 * 4];
        new Random(1).NextBytes(pixels);
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(8, 8, pixels)));
        var bytes = WebP.EncodeAnimation(anim);

        // Corrupt every 'VP8L' occurrence past the header region.
        for (var i = 30; i + 4 <= bytes.Length; i++)
        {
            if (bytes[i] == (byte)'V' && bytes[i + 1] == (byte)'P' && bytes[i + 2] == (byte)'8' && bytes[i + 3] == (byte)'L')
                bytes[i] = (byte)'X';
        }
        Assert.ThrowsAny<WebPException>(() => WebP.DecodeAnimation(bytes));
    }

    [Fact]
    public void AnimationFuzz_FailsCleanly()
    {
        var anim = new WebPAnimation(12, 10) { LoopCount = 3 };
        var pixels = new byte[12 * 10 * 4];
        new Random(2).NextBytes(pixels);
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(12, 10, pixels)));
        anim.Frames.Add(new WebPFrame(WebPImage.CreateRgba(4, 4, new byte[4 * 4 * 4]), 2, 2));
        var valid = WebP.EncodeAnimation(anim);

        var rng = new Random(5);
        for (var trial = 0; trial < 1500; trial++)
        {
            var mutated = (byte[])valid.Clone();
            var idx = rng.Next(mutated.Length);
            mutated[idx] ^= (byte)(1 << rng.Next(8));
            try
            {
                WebP.DecodeAnimation(mutated, SmallLimit);
            }
            catch (WebPException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}

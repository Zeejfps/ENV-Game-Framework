using WebPSharp.Api;
using WebPSharp.Api.Exceptions;
using WebPSharp.Container;

namespace WebPSharp.Tests;

/// <summary>
/// Covers VP8X feature-flag vs actual-chunk handling. Behavior is aligned to libwebp 1.6.0:
/// <list type="bullet">
/// <item>The core decoder (dwebp / WebPDecode) tolerates every flag/chunk disagreement and
/// applies alpha from the actual ALPH chunk, not the flag.</item>
/// <item>WebPGetFeatures reports has_alpha = (VP8X alpha flag) OR (an ALPH chunk exists), except
/// that a wrapped VP8L image OVERWRITES the flag with its intrinsic alpha_is_used bit (VP8LGetInfo
/// assigns has_alpha), so for VP8X+VP8L the flag is ignored in favor of the VP8L bit.</item>
/// <item>ParseVP8X rejects any VP8X payload whose size != 10 (VP8X_CHUNK_SIZE).</item>
/// </list>
/// </summary>
public class WebPVp8XFlagTests
{
    // 10-byte VP8X payload with a raw feature-flag byte.
    private static byte[] Vp8X(int w, int h, byte flags)
    {
        var p = new byte[10];
        p[0] = flags;
        var ww = w - 1;
        var hh = h - 1;
        p[4] = (byte)ww; p[5] = (byte)(ww >> 8); p[6] = (byte)(ww >> 16);
        p[7] = (byte)hh; p[8] = (byte)(hh >> 8); p[9] = (byte)(hh >> 16);
        return p;
    }

    private static WebPImage AlphaGradient(int w, int h)
    {
        var px = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            px[i * 4 + 0] = 200;
            px[i * 4 + 1] = 100;
            px[i * 4 + 2] = 50;
            px[i * 4 + 3] = (byte)(i * 255 / (w * h)); // varies, never uniformly opaque
        }
        return WebPImage.CreateRgba(w, h, px);
    }

    private static byte[] LossyWithAlpha(int w, int h) =>
        WebP.Encode(AlphaGradient(w, h), new WebPEncoderOptions { Lossless = false, Quality = 90 });

    // Clears/sets bits in the VP8X flag byte, which is always at offset 20 (RIFF+WEBP+"VP8X"+size).
    private static byte[] PatchVp8xFlags(byte[] webp, byte and, byte or)
    {
        var b = (byte[])webp.Clone();
        b[20] = (byte)((b[20] & and) | or);
        return b;
    }

    private static byte[] StripChunk(byte[] webp, FourCc target)
    {
        var outp = new List<byte>();
        outp.AddRange(webp[..12]); // "RIFF" + size + "WEBP"
        var i = 12;
        while (i + 8 <= webp.Length)
        {
            var id = FourCc.Read(webp.AsSpan(i, 4));
            var size = BitConverter.ToInt32(webp, i + 4);
            var total = 8 + size + (size & 1);
            if (id != target)
                outp.AddRange(webp[i..(i + total)]);
            i += total;
        }
        var arr = outp.ToArray();
        BitConverter.GetBytes(arr.Length - 8).CopyTo(arr, 4);
        return arr;
    }

    // --- Identify: ICC/EXIF/XMP reported by ACTUAL chunk presence, not the advisory flag ---

    [Theory]
    [InlineData((byte)0x20)] // ICC flag
    [InlineData((byte)0x08)] // EXIF flag
    [InlineData((byte)0x04)] // XMP flag
    public void Identify_MetadataFlagSet_ButChunkAbsent_ReportsFalse(byte flag)
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, flag)),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, false)));
        var info = WebP.Identify(bytes);

        Assert.False(info.HasIccProfile);
        Assert.False(info.HasExif);
        Assert.False(info.HasXmp);
    }

    [Fact]
    public void Identify_IccpPresent_ButFlagClear_ReportsIcc()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x00)),
            (WebPChunkIds.Iccp, new byte[] { 1, 2, 3 }),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, false)));
        Assert.True(WebP.Identify(bytes).HasIccProfile);
    }

    [Fact]
    public void Identify_ExifPresent_ButFlagClear_ReportsExif()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x00)),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, false)),
            (WebPChunkIds.Exif, new byte[] { 9, 9 }));
        Assert.True(WebP.Identify(bytes).HasExif);
    }

    [Fact]
    public void Identify_XmpPresent_ButFlagClear_ReportsXmp()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x00)),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, false)),
            (WebPChunkIds.Xmp, new byte[] { 7 }));
        Assert.True(WebP.Identify(bytes).HasXmp);
    }

    // --- Identify: HasAlpha = flag OR actual ALPH chunk (matches WebPGetFeatures) ---

    // For a VP8X wrapping a LOSSY (VP8) image, the flag drives has_alpha (VP8GetInfo does not
    // touch has_alpha), so a set flag reports alpha even without an ALPH chunk.
    [Fact]
    public void Identify_AlphaFlagSet_NoAlphChunk_LossyInner_ReportsAlpha()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x10)),
            (WebPChunkIds.Vp8, WebPTestData.Vp8Header(8, 8)));
        Assert.True(WebP.Identify(bytes).HasAlpha);
    }

    [Fact]
    public void Identify_AlphaFlagClear_AlphChunkPresent_ReportsAlpha()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x00)),
            (WebPChunkIds.Alph, new byte[] { 0, 1, 2, 3 }),
            (WebPChunkIds.Vp8, WebPTestData.Vp8Header(8, 8)));
        Assert.True(WebP.Identify(bytes).HasAlpha);
    }

    // --- Identify: VP8X wrapping VP8L uses the VP8L intrinsic alpha bit, NOT the VP8X flag ---
    // libwebp ParseHeadersInternal: *has_alpha = flag; VP8LGetInfo OVERWRITES it with the VP8L
    // alpha_is_used bit; then |= (ALPH present). So the final value is (vp8l_bit || alph_present)
    // and the VP8X flag is ignored for lossless inner images. All four flag x vp8l-bit combos:

    [Fact]
    public void Identify_Vp8X_Vp8L_FlagClear_BitClear_ReportsNoAlpha()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x00)),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, false)));
        Assert.False(WebP.Identify(bytes).HasAlpha);
    }

    [Fact]
    public void Identify_Vp8X_Vp8L_FlagClear_BitSet_ReportsAlpha()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x00)),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, true)));
        Assert.True(WebP.Identify(bytes).HasAlpha);
    }

    // Flag SET but VP8L bit CLEAR: VP8LGetInfo overwrites the flag with 0 -> reports NO alpha.
    [Fact]
    public void Identify_Vp8X_Vp8L_FlagSet_BitClear_ReportsNoAlpha()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x10)),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, false)));
        Assert.False(WebP.Identify(bytes).HasAlpha);
    }

    [Fact]
    public void Identify_Vp8X_Vp8L_FlagSet_BitSet_ReportsAlpha()
    {
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, Vp8X(8, 8, 0x10)),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, true)));
        Assert.True(WebP.Identify(bytes).HasAlpha);
    }

    // A bare VP8L (no VP8X) reports HasAlpha straight from its intrinsic bit.
    [Fact]
    public void Identify_BareVp8L_WithAlphaBit_ReportsAlpha()
    {
        var bytes = WebPTestData.Container(
            WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, true));
        Assert.True(WebP.Identify(bytes).HasAlpha);
    }

    // --- Decode: leniency matching dwebp (no rejection of flag/chunk mismatch) ---

    [Fact]
    public void Decode_AlphaFlagClear_ButAlphPresent_StillAppliesAlpha()
    {
        var baseline = LossyWithAlpha(16, 16);
        var patched = PatchVp8xFlags(baseline, and: 0xEF, or: 0x00); // clear 0x10 alpha flag

        var expected = WebP.Decode(baseline);
        var actual = WebP.Decode(patched);

        // The cleared flag must not change the pixels: ALPH is applied from its presence.
        Assert.Equal(expected.PixelData, actual.PixelData);
        // And alpha really was applied (source alpha varies, so not uniformly opaque).
        var anyTransparent = false;
        for (var i = 3; i < actual.PixelData.Length; i += 4)
            if (actual.PixelData[i] != 255) { anyTransparent = true; break; }
        Assert.True(anyTransparent);
    }

    [Fact]
    public void Decode_AlphaFlagSet_ButNoAlphChunk_DecodesOpaque()
    {
        var baseline = LossyWithAlpha(16, 16); // has 0x10 flag set
        var noAlph = StripChunk(baseline, WebPChunkIds.Alph); // flag stays set, ALPH removed

        var img = WebP.Decode(noAlph); // must not throw (matches dwebp)
        for (var i = 3; i < img.PixelData.Length; i += 4)
            Assert.Equal(255, img.PixelData[i]);
    }

    // --- VP8X payload size must be exactly 10 (matches libwebp ParseVP8X) ---

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(20)]
    public void Identify_Vp8XSizeNot10_Throws(int size)
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8X, new byte[size]);
        Assert.Throws<WebPFormatException>(() => WebP.Identify(bytes));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void Decode_Vp8XSizeNot10_Throws(int size)
    {
        var vp8x = new byte[size];
        // Keep a sane canvas in case a reader ever gets that far; the size check fires first.
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Vp8X, vp8x),
            (WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(8, 8, false)));
        Assert.Throws<WebPFormatException>(() => WebP.Decode(bytes));
    }

    [Fact]
    public void Decode_Vp8XSizeExactly10_Succeeds()
    {
        var img = WebP.Decode(LossyWithAlpha(12, 12));
        Assert.Equal(12, img.Width);
        Assert.Equal(12, img.Height);
    }
}

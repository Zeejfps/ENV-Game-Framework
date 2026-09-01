using WebPSharp.Api.Exceptions;
using WebPSharp.Container;
using WebPSharp.Vp8;

namespace WebPSharp.Tests;

// Regression coverage for the VP8 frame-tag version/profile field (RFC 6386 §9.1).
//
// The RFC table maps the version to a reconstruction + loop-filter combination
// (v0 normal, v1 simple, v2/v3 none). libwebp does NOT act on that table: filter_type
// is derived purely from the filter header, and the profile is only range-checked.
// Empirically, dwebp 1.6.0 emits byte-identical output for versions 0-3 of the same
// key frame, so WebPSharp must ignore the version too (not override the filter). These
// tests lock that in and would fail if a version->filter override were ever added.
public class Vp8ProfileTests
{
    // grad_q80 has a non-trivial in-loop filter active (its filtered and -nofilter dwebp
    // goldens differ), so a wrongly-applied v2/v3 "no filter" override would change the output.
    private const string Asset = "grad_q80.webp";
    private const string FilteredGolden = "grad_q80.rgba";

    private static byte[] Vp8Payload(string asset)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", asset));
        var reader = RiffReader.Create(bytes);
        while (reader.MoveNext())
            if (reader.Current.Id == WebPChunkIds.Vp8)
                return reader.Current.Payload.ToArray();
        throw new InvalidOperationException("no VP8 chunk");
    }

    // Patches the 3-bit version field (bits 1-3 of the first frame-tag byte), leaving
    // key_frame (bit 0), show (bit 4) and first_part_size (bits 5+) untouched.
    private static byte[] WithVersion(byte[] payload, int version)
    {
        var patched = (byte[])payload.Clone();
        patched[0] = (byte)((patched[0] & ~0x0E) | ((version & 7) << 1));
        return patched;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Decode_Vp8Version1To3_IgnoresVersion_MatchesDwebp(int version)
    {
        var payload = WithVersion(Vp8Payload(Asset), version);
        var decoder = new Vp8Decoder(payload);
        var rgba = decoder.DecodeToRgba(); // default: in-loop filter + fancy upsampling

        Assert.Equal(version, decoder.Profile);

        var reference = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", FilteredGolden));
        Assert.Equal(reference.Length, rgba.Length);
        var max = 0;
        for (var i = 0; i < reference.Length; i++)
        {
            var d = Math.Abs(rgba[i] - reference[i]);
            if (d > max) max = d;
        }
        // Must equal the dwebp key-frame output, which is identical across versions 0-3.
        Assert.True(max <= 1, $"version {version}: maxDiff={max} (version must not alter key-frame decode)");
    }

    [Fact]
    public void Decode_Vp8Version0_Control_MatchesDwebp()
    {
        var decoder = new Vp8Decoder(Vp8Payload(Asset));
        var rgba = decoder.DecodeToRgba();
        Assert.Equal(0, decoder.Profile);

        var reference = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", FilteredGolden));
        var max = 0;
        for (var i = 0; i < reference.Length; i++)
        {
            var d = Math.Abs(rgba[i] - reference[i]);
            if (d > max) max = d;
        }
        Assert.True(max <= 1, $"control: maxDiff={max}");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Decode_Vp8InvalidVersion_Throws(int version)
    {
        var payload = WithVersion(Vp8Payload(Asset), version);
        var decoder = new Vp8Decoder(payload);
        Assert.Throws<WebPFormatException>(() => decoder.DecodeToRgba());
    }

    // key_frame is bit 0 of the frame tag (0 = key frame in VP8). Setting it to 1 marks the
    // sole VP8 frame as an inter frame, which is a malformed still-image WebP bitstream.
    [Fact]
    public void Decode_Vp8NonKeyFrame_ThrowsFormatException()
    {
        var payload = (byte[])Vp8Payload(Asset).Clone();
        payload[0] |= 0x01;
        var decoder = new Vp8Decoder(payload);
        Assert.Throws<WebPFormatException>(() => decoder.DecodeToRgba());
    }
}

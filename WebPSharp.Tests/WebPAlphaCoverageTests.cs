using System.Diagnostics;
using WebPSharp.Api;
using WebPSharp.Api.Exceptions;
using WebPSharp.Vp8;

namespace WebPSharp.Tests;

/// <summary>
/// Byte-level coverage for the ALPH alpha chunk (audit §17): per-filter unfilter reconstruction with
/// hand-computed vectors, per-filter and edge-dimension round-trips, all-opaque / all-transparent
/// handling, malformed-header and truncated-payload rejection, and a dwebp interop gate.
/// The pre-processing (P) accept/reject and reserved-bit reject paths live in <see cref="WebPAlphaTests"/>.
/// </summary>
public sealed class WebPAlphaCoverageTests
{
    // ---- Header helpers -----------------------------------------------------------------------

    private const int NoCompression = 0;
    private const int LosslessCompression = 1;
    private const int FilterNone = 0;
    private const int FilterHorizontal = 1;
    private const int FilterVertical = 2;
    private const int FilterGradient = 3;

    private static byte Header(int method, int filter) => (byte)((method & 0x03) | ((filter & 0x03) << 2));

    // Builds a raw (C=0) ALPH payload: 1 header byte + the (already-filtered) delta plane.
    private static byte[] RawPayload(int filter, byte[] deltas)
    {
        var p = new byte[1 + deltas.Length];
        p[0] = Header(NoCompression, filter);
        Array.Copy(deltas, 0, p, 1, deltas.Length);
        return p;
    }

    // ---- Per-filter unfilter reconstruction (hand-computed expected values) --------------------
    // These decode a raw C=0 payload whose bytes ARE the filter residuals, and assert the decoder's
    // unfilter output matches values computed by hand from the filter formulas (mod 256), including
    // the first-row / first-column edge rules and 8-bit wraparound.

    [Fact]
    public void Decode_FilterNone_ReturnsResidualsVerbatim()
    {
        var deltas = new byte[] { 10, 20, 30, 40, 50, 60 };
        var alpha = Vp8AlphaDecoder.Decode(RawPayload(FilterNone, deltas), 3, 2);
        Assert.Equal(deltas, alpha);
    }

    [Fact]
    public void Decode_FilterHorizontal_ReconstructsRunningRowSums()
    {
        // 3x2. Row0 pred starts at 0; each later col adds the reconstructed left.
        //   row0: 200, (200+100)=44, (44+50)=94        (44 = 300 & 0xFF)
        //   row1 col0 pred = top(=out[0,0]=200): 230, 234, 235
        var deltas = new byte[] { 200, 100, 50, 30, 4, 1 };
        var expected = new byte[] { 200, 44, 94, 230, 234, 235 };
        Assert.Equal(expected, Vp8AlphaDecoder.Decode(RawPayload(FilterHorizontal, deltas), 3, 2));
    }

    [Fact]
    public void Decode_FilterVertical_AddsPixelAbove()
    {
        // 3x2. Row0 is horizontal (pred 0): 10, 30, 60.
        //   row1 = top + delta: 15, (30+250)=24, 160    (24 = 280 & 0xFF)
        var deltas = new byte[] { 10, 20, 30, 5, 250, 100 };
        var expected = new byte[] { 10, 30, 60, 15, 24, 160 };
        Assert.Equal(expected, Vp8AlphaDecoder.Decode(RawPayload(FilterVertical, deltas), 3, 2));
    }

    [Fact]
    public void Decode_FilterGradient_UsesClampedLeftPlusTopMinusTopLeft()
    {
        // 3x2. Row0 is horizontal (pred 0): 10, 30, 60.
        //   row1 col0: pred = top(=10)                         -> 5+10  = 15
        //   row1 col1: pred = clamp(left15 + top30 - tl10)=35  -> 4+35  = 39
        //   row1 col2: pred = clamp(left39 + top60 - tl30)=69  -> 3+69  = 72
        var deltas = new byte[] { 10, 20, 30, 5, 4, 3 };
        var expected = new byte[] { 10, 30, 60, 15, 39, 72 };
        Assert.Equal(expected, Vp8AlphaDecoder.Decode(RawPayload(FilterGradient, deltas), 3, 2));
    }

    // ---- Per-filter round-trips over a structured (ramp) alpha plane ----------------------------
    // Independent forward filter (an oracle, NOT the library encoder) produces residuals; the decoder
    // must invert each filter exactly. The ramp spans 0..255 so gradient clamping is exercised.

    [Theory]
    [InlineData(FilterNone)]
    [InlineData(FilterHorizontal)]
    [InlineData(FilterVertical)]
    [InlineData(FilterGradient)]
    public void Decode_PerFilter_RoundTripsStructuredAlpha(int filter)
    {
        const int w = 7, h = 5;
        var alpha = RampAlpha(w, h);
        var deltas = ForwardFilter(alpha, w, h, filter);
        var got = Vp8AlphaDecoder.Decode(RawPayload(filter, deltas), w, h);
        Assert.Equal(alpha, got);
    }

    // ---- End-to-end chunk round-trip via the real encoder (covers C=1 + encoder filter choice) --

    [Fact]
    public void EncodeThenDecodeChunk_StructuredAlpha_RoundTripsExact()
    {
        const int w = 24, h = 18;
        var rgba = BuildRgbaRampAlpha(w, h);
        var payload = Vp8AlphaEncoder.Encode(rgba, w, h);
        Assert.NotNull(payload);

        var alpha = Vp8AlphaDecoder.Decode(payload!, w, h);
        for (var i = 0; i < w * h; i++)
            Assert.Equal(rgba[i * 4 + 3], alpha[i]);
    }

    // ---- Full public-API round-trip (VP8X + ALPH + VP8), alpha must be lossless -----------------

    [Fact]
    public void EncodeDecode_LossyImageWithAlpha_AlphaExactAndAlphChunkPresent()
    {
        const int w = 32, h = 20;
        var rgba = BuildRgbaRampAlpha(w, h);
        var webp = WebP.Encode(WebPImage.CreateRgba(w, h, rgba), new WebPEncoderOptions { Lossless = false });

        Assert.True(FindChunkPayloadOffset(webp, "ALPH") > 0, "expected an ALPH chunk");

        var decoded = WebP.Decode(webp);
        Assert.Equal(WebPColorFormat.Rgba, decoded.Format);
        for (var i = 0; i < w * h; i++)
            Assert.Equal(rgba[i * 4 + 3], decoded.PixelData[i * 4 + 3]);
    }

    // ---- Edge dimensions: 1xN and Nx1 exercise first-row / first-column edge handling -----------

    [Theory]
    [InlineData(1, 9)]
    [InlineData(9, 1)]
    [InlineData(1, 1)]
    public void EncodeDecodeChunk_EdgeDimensions_RoundTripExact(int w, int h)
    {
        var rgba = BuildRgbaRampAlpha(w, h);
        var payload = Vp8AlphaEncoder.Encode(rgba, w, h);
        Assert.NotNull(payload);

        var alpha = Vp8AlphaDecoder.Decode(payload!, w, h);
        for (var i = 0; i < w * h; i++)
            Assert.Equal(rgba[i * 4 + 3], alpha[i]);
    }

    // ---- All-opaque omits ALPH; all-transparent round-trips to 0 --------------------------------

    [Fact]
    public void Encode_AllOpaqueAlpha_OmitsAlphChunk()
    {
        const int w = 16, h = 16;
        var rgba = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            rgba[i * 4] = (byte)(i * 3);
            rgba[i * 4 + 1] = (byte)(i * 5);
            rgba[i * 4 + 2] = (byte)(i * 7);
            rgba[i * 4 + 3] = 255;
        }

        Assert.Null(Vp8AlphaEncoder.Encode(rgba, w, h));

        var webp = WebP.Encode(WebPImage.CreateRgba(w, h, rgba), new WebPEncoderOptions { Lossless = false });
        Assert.Equal(-1, FindChunkPayloadOffset(webp, "ALPH"));
    }

    [Fact]
    public void EncodeDecode_AllTransparentAlpha_RoundTripsToZero()
    {
        const int w = 20, h = 12;
        var rgba = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            rgba[i * 4] = (byte)(i * 3);
            rgba[i * 4 + 1] = (byte)(i * 5);
            rgba[i * 4 + 2] = (byte)(i * 7);
            rgba[i * 4 + 3] = 0;
        }

        var payload = Vp8AlphaEncoder.Encode(rgba, w, h);
        Assert.NotNull(payload); // not opaque -> ALPH is emitted

        var alpha = Vp8AlphaDecoder.Decode(payload!, w, h);
        Assert.All(alpha, a => Assert.Equal(0, a));

        var decoded = WebP.Decode(WebP.Encode(WebPImage.CreateRgba(w, h, rgba), new WebPEncoderOptions { Lossless = false }));
        for (var i = 0; i < w * h; i++)
            Assert.Equal(0, decoded.PixelData[i * 4 + 3]);
    }

    // ---- Rejection paths ------------------------------------------------------------------------

    [Theory]
    [InlineData(2)] // C = 2
    [InlineData(3)] // C = 3
    public void Decode_InvalidCompressionMethod_Throws(int method)
    {
        var payload = new byte[] { Header(method, FilterNone), 0, 0, 0, 0 };
        Assert.Throws<WebPFormatException>(() => Vp8AlphaDecoder.Decode(payload, 2, 2));
    }

    [Fact]
    public void Decode_RawTruncatedPayload_ThrowsNotCrashes()
    {
        // C=0 needs width*height = 16 residual bytes; supply only 5.
        var payload = new byte[] { Header(NoCompression, FilterNone), 1, 2, 3, 4, 5 };
        Assert.Throws<WebPFormatException>(() => Vp8AlphaDecoder.Decode(payload, 4, 4));
    }

    [Fact]
    public void Decode_EmptyPayload_Throws()
    {
        Assert.Throws<WebPFormatException>(() => Vp8AlphaDecoder.Decode(Array.Empty<byte>(), 2, 2));
    }

    // ---- dwebp interop: third-party decoder must accept the file and reproduce alpha exactly -----

    [Fact]
    public void EncodeLossyWithAlpha_DecodedByDwebp_AlphaExact()
    {
        var dwebp = TryGetToolPath("dwebp");
        if (dwebp is null)
            return; // dwebp unavailable: skip the interop gate.

        const int w = 40, h = 28;
        var rgba = BuildRgbaRampAlpha(w, h);
        var webp = WebP.Encode(WebPImage.CreateRgba(w, h, rgba), new WebPEncoderOptions { Lossless = false });

        var dir = Path.Combine(Path.GetTempPath(), "webpsharp-alph-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var inPath = Path.Combine(dir, "in.webp");
            var outPath = Path.Combine(dir, "out.pam");
            File.WriteAllBytes(inPath, webp);

            var (exit, stderr) = Run(dwebp, $"\"{inPath}\" -pam -o \"{outPath}\"");
            Assert.True(exit == 0, $"dwebp rejected WebPSharp alpha output (exit {exit}): {stderr}");

            var (gotW, gotH, rgbaOut) = ReadPam(File.ReadAllBytes(outPath));
            Assert.Equal(w, gotW);
            Assert.Equal(h, gotH);

            var maxAlphaDiff = 0;
            for (var i = 0; i < w * h; i++)
                maxAlphaDiff = Math.Max(maxAlphaDiff, Math.Abs(rgba[i * 4 + 3] - rgbaOut[i * 4 + 3]));
            Assert.Equal(0, maxAlphaDiff); // alpha is coded losslessly.
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }

    // ---- Fixtures & oracles ---------------------------------------------------------------------

    // Alpha ramp that spans the full 0..255 range across the plane (structured so filters matter).
    private static byte[] RampAlpha(int w, int h)
    {
        var a = new byte[w * h];
        var last = Math.Max(1, w * h - 1);
        for (var i = 0; i < w * h; i++)
            a[i] = (byte)(i * 255 / last);
        return a;
    }

    private static byte[] BuildRgbaRampAlpha(int w, int h)
    {
        var alpha = RampAlpha(w, h);
        var rgba = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            rgba[i * 4] = (byte)(i * 3);
            rgba[i * 4 + 1] = (byte)(i * 5);
            rgba[i * 4 + 2] = (byte)(i * 7);
            rgba[i * 4 + 3] = alpha[i];
        }
        return rgba;
    }

    private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;

    // Independent forward filter mirroring the ALPH spec: produces residuals d such that unfiltering
    // with the same method reproduces `alpha`. Used purely as a decoder oracle.
    private static byte[] ForwardFilter(byte[] alpha, int w, int h, int filter)
    {
        if (filter == FilterNone)
            return (byte[])alpha.Clone();

        var d = new byte[w * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var idx = y * w + x;
            int pred;
            switch (filter)
            {
                case FilterHorizontal:
                    pred = x > 0 ? alpha[idx - 1] : (y > 0 ? alpha[idx - w] : 0);
                    break;
                case FilterVertical:
                    pred = y > 0 ? alpha[idx - w] : (x > 0 ? alpha[idx - 1] : 0);
                    break;
                default: // FilterGradient
                    if (y == 0)
                        pred = x > 0 ? alpha[idx - 1] : 0;
                    else if (x == 0)
                        pred = alpha[idx - w];
                    else
                        pred = Clamp(alpha[idx - 1] + alpha[idx - w] - alpha[idx - w - 1]);
                    break;
            }
            d[idx] = (byte)(alpha[idx] - pred);
        }
        return d;
    }

    private static int FindChunkPayloadOffset(byte[] webp, string fourCc)
    {
        var pos = 12; // RIFF(4) + size(4) + WEBP(4)
        while (pos + 8 <= webp.Length)
        {
            var cc = System.Text.Encoding.ASCII.GetString(webp, pos, 4);
            var size = (uint)(webp[pos + 4] | (webp[pos + 5] << 8) | (webp[pos + 6] << 16) | (webp[pos + 7] << 24));
            var payload = pos + 8;
            if (cc == fourCc)
                return payload;
            pos = payload + (int)size;
            if ((size & 1) == 1) pos++;
        }
        return -1;
    }

    // Parses a binary PAM (P7) with DEPTH 4 (RGB_ALPHA), as emitted by `dwebp -pam`.
    private static (int W, int H, byte[] Rgba) ReadPam(byte[] data)
    {
        int w = 0, h = 0, depth = 0, pos = 0;
        while (pos < data.Length)
        {
            var eol = Array.IndexOf(data, (byte)'\n', pos);
            if (eol < 0) eol = data.Length;
            var line = System.Text.Encoding.ASCII.GetString(data, pos, eol - pos).Trim();
            pos = eol + 1;
            if (line == "ENDHDR")
                break;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (parts[0] == "WIDTH") w = int.Parse(parts[1]);
                else if (parts[0] == "HEIGHT") h = int.Parse(parts[1]);
                else if (parts[0] == "DEPTH") depth = int.Parse(parts[1]);
            }
        }
        Assert.Equal(4, depth);
        var rgba = new byte[w * h * 4];
        Array.Copy(data, pos, rgba, 0, rgba.Length);
        return (w, h, rgba);
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

    private static string? TryGetToolPath(string tool)
    {
        var candidates = new List<string>();
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (!string.IsNullOrEmpty(dir))
                candidates.Add(Path.Combine(dir, tool));
        }
        candidates.Add(Path.Combine("/opt/homebrew/bin", tool));
        candidates.Add(Path.Combine("/usr/local/bin", tool));
        foreach (var c in candidates)
        {
            try { if (File.Exists(c)) return c; }
            catch { /* ignore */ }
        }
        return null;
    }
}

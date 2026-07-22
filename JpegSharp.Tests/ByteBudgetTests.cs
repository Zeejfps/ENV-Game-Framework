using System.Diagnostics;
using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Markers;
using Xunit;

namespace JpegSharp.Tests;

// FOLLOWUP-BYTEBUDGET: MaxDecodedBytes bounds PEAK decode allocation by bytes (component sample
// planes + progressive coefficient buffers + output buffer), catching amplification vectors
// (multi-component / 12-bit / progressive) that a pixel-count bound (MaxPixels) alone misses.
public class ByteBudgetTests
{
    // For a WxH single-component (grayscale) 8-bit baseline image the estimated peak is:
    //   plane (PlaneWidth*PlaneHeight) + output (W*H) == 4096 + 4096 == 8192 bytes for 64x64.
    // Progressive adds a coefficient buffer (BlocksWide*BlocksHigh*64*2) == 8192 -> 16384 total.
    // 12-bit doubles bytes/sample (plane + output) -> 16384 total.
    private const int Dim = 64;
    private const long BaselineGray8Bytes = 8192;
    private const long ProgressiveGray8Bytes = 16384;
    private const long Gray12Bytes = 16384;

    // A 64x64 RGB 4:2:0 (luma 2x2, two chroma 1x1) 8-bit baseline image:
    //   planes  = luma 64x64 (4096) + 2 chroma 32x32 (1024 each)          == 6144
    //   output  = 64*64*3                                                  == 12288
    //   plane+output (the PRE-fix estimate)                                == 18432
    // Each of the two subsampled chroma components upsamples to a full 64x64 scratch plane during
    // assembly: 2 * 4096 == 8192, so the corrected estimate is 26624.
    private const int Sub = 64;
    private const long Rgb420PlanesOutput = 18432;
    private const long Rgb420EstimateWithScratch = 26624;

    [Fact]
    public void Decode_ExceedsByteBudget_ThrowsJpegException()
    {
        // A small but valid image whose estimated decode bytes exceed a custom low budget must be
        // rejected as a typed JpegFormatException before the large allocation, fast.
        var image = JpegImage.CreateGrayscale(Dim, Dim, Gradient(Dim, Dim, 1));
        var bytes = Jpeg.Encode(image);
        var options = new JpegDecoderOptions { MaxDecodedBytes = 1000 };

        var sw = Stopwatch.StartNew();
        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes, options));
        sw.Stop();

        Assert.Contains("Estimated decode allocation", ex.Message);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"guard took {sw.Elapsed}");
    }

    [Fact]
    public void Decode_ProgressiveCostsMoreThanBaseline_ByteBudget()
    {
        // A budget between the baseline and progressive estimates for the SAME dimensions lets a
        // baseline image through but rejects a progressive one, proving coefficient buffers count.
        var image = JpegImage.CreateGrayscale(Dim, Dim, Gradient(Dim, Dim, 1));
        var budget = (BaselineGray8Bytes + ProgressiveGray8Bytes) / 2; // 12288
        Assert.True(budget > BaselineGray8Bytes && budget < ProgressiveGray8Bytes);

        var baseline = Jpeg.Encode(image);
        var progressive = Jpeg.Encode(image, new JpegEncoderOptions { Progressive = true });
        var options = new JpegDecoderOptions { MaxDecodedBytes = budget };

        var decoded = Jpeg.Decode(baseline, options);
        Assert.Equal(Dim, decoded.Width);
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(progressive, options));
    }

    [Fact]
    public void Decode_12bitCostsMoreThan8bit_ByteBudget()
    {
        // A budget between the 8-bit and 12-bit estimates for the SAME dimensions lets the 8-bit
        // image through but rejects the 12-bit one, proving precision counts.
        var budget = (BaselineGray8Bytes + Gray12Bytes) / 2; // 12288
        Assert.True(budget > BaselineGray8Bytes && budget < Gray12Bytes);

        var image8 = JpegImage.CreateGrayscale(Dim, Dim, Gradient(Dim, Dim, 1));
        var image12 = JpegImage16.CreateGrayscale(Dim, Dim, 12, Gradient12(Dim, Dim, 1));
        var options = new JpegDecoderOptions { MaxDecodedBytes = budget };

        var decoded8 = Jpeg.Decode(Jpeg.Encode(image8), options);
        Assert.Equal(Dim, decoded8.Width);
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode16(Jpeg.Encode16(image12), options));
    }

    [Fact]
    public void Decode_HostileProgressiveHeader_BoundedByByteBudget()
    {
        // A ~40-byte progressive header with dimensions under the default MaxPixels (500M) but
        // whose estimated peak allocation exceeds the default MaxDecodedBytes (1 GiB) must be
        // rejected quickly, typed, without OOM/hang. 22000x22000 = 4.84e8 px < 500M, yet its
        // plane + progressive coefficient + output estimate is ~1.9 GiB.
        var bytes = BuildSofSos(JpegMarkers.StartOfFrameProgressive, 8, 22000, 22000, [0x11]);

        var sw = Stopwatch.StartNew();
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        sw.Stop();

        Assert.NotNull(ex);
        Assert.IsAssignableFrom<JpegException>(ex);
        Assert.IsNotType<OutOfMemoryException>(ex);
        Assert.Contains("Estimated decode allocation", ex.Message);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"guard took {sw.Elapsed}");
    }

    [Fact]
    public void Decode_NormalImages_WithinDefaultBudget()
    {
        // Ordinary images across component counts, precision, subsampling and progressive mode
        // still decode fine under the DEFAULT budget (regression guard).
        var rgb = JpegImage.CreateRgb(48, 32, Gradient(48, 32, 3));
        Assert.Equal(48, Jpeg.Decode(Jpeg.Encode(rgb, new JpegEncoderOptions { Quality = 85 })).Width);
        Assert.Equal(48, Jpeg.Decode(Jpeg.Encode(rgb, new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 })).Width);
        Assert.Equal(32, Jpeg.Decode(Jpeg.Encode(rgb, new JpegEncoderOptions { Quality = 85, Progressive = true })).Height);

        var gray = JpegImage.CreateGrayscale(24, 24, Gradient(24, 24, 1));
        Assert.Equal(24, Jpeg.Decode(Jpeg.Encode(gray)).Width);

        var gray12 = JpegImage16.CreateGrayscale(24, 24, 12, Gradient12(24, 24, 1));
        Assert.Equal(12, Jpeg.Decode16(Jpeg.Encode16(gray12, new JpegEncoderOptions { Quality = 100 })).Precision);
        Assert.Equal(12, Jpeg.Decode16(Jpeg.Encode16(gray12, new JpegEncoderOptions { Quality = 100, Progressive = true })).Precision);
    }

    [Fact]
    public void Decode_CustomHighBudget_AllowsLargerImage()
    {
        // MaxDecodedBytes is a tunable, first-to-trip-wins bound: an image a low budget rejects
        // decodes once the budget is raised above its estimate.
        var image = JpegImage.CreateGrayscale(Dim, Dim, Gradient(Dim, Dim, 1));
        var bytes = Jpeg.Encode(image);

        Assert.Throws<JpegFormatException>(
            () => Jpeg.Decode(bytes, new JpegDecoderOptions { MaxDecodedBytes = 1000 }));

        var decoded = Jpeg.Decode(bytes, new JpegDecoderOptions { MaxDecodedBytes = 1L << 20 });
        Assert.Equal(Dim, decoded.Width);
    }

    [Fact]
    public void Decode_SubsampledUpsampleScratch_CountedInBudget()
    {
        // The chroma-upsampling scratch planes (allocated per subsampled component during assembly)
        // must be part of the byte budget. A budget equal to the OLD plane+output estimate (which
        // omitted the scratch) let the image through pre-fix; post-fix the estimate is larger by
        // exactly the scratch, so the same budget now rejects it — and the reported estimate equals
        // the corrected value, proving the scratch (and only the scratch) was added.
        var image = JpegImage.CreateRgb(Sub, Sub, Gradient(Sub, Sub, 3));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 });

        var atOldEstimate = Assert.Throws<JpegFormatException>(
            () => Jpeg.Decode(bytes, new JpegDecoderOptions { MaxDecodedBytes = Rgb420PlanesOutput }));
        Assert.Contains("Estimated decode allocation", atOldEstimate.Message);
        Assert.Contains(Rgb420EstimateWithScratch.ToString(), atOldEstimate.Message);

        // One byte under the corrected estimate still rejects (tight lower edge)...
        Assert.Throws<JpegFormatException>(
            () => Jpeg.Decode(bytes, new JpegDecoderOptions { MaxDecodedBytes = Rgb420EstimateWithScratch - 1 }));

        // ...and at exactly the corrected estimate the real decode fits and succeeds, so the estimate
        // is a genuine (not grossly over-counted) upper bound of the true peak.
        var decoded = Jpeg.Decode(bytes, new JpegDecoderOptions { MaxDecodedBytes = Rgb420EstimateWithScratch });
        Assert.Equal(Sub, decoded.Width);
        Assert.Equal(Sub, decoded.Height);
    }

    [Fact]
    public void Decode_CrossSubsampling_PeakBounded()
    {
        // Hostile 4-component cross-subsampling [0x41,0x14,0x11,0x11]: hmax=vmax=4, comp0 is H=4,V=1
        // (V<vmax) and comp1 is H=1,V=4 (H<hmax), so ALL four components are subsampled and each
        // allocates a full MCU-padded upsample scratch plane at assembly. At 13000x13000
        // (1.69e8 px < the 5e8 default MaxPixels) the plane+output estimate is ~782 MB (UNDER the
        // 1 GiB default), but the four scratch planes add ~678 MB, pushing the true peak past 1 GiB.
        // Pre-fix the scratch was uncounted, so this decoded UNBOUNDED to ~1.46 GB; post-fix the
        // default budget rejects it. Σ(Hi·Vi)=4+4+1+1=10, the interleaved-MCU maximum, so it parses.
        const long crossPlanesOutput = 782_015_360;                       // 106,015,360 planes + 676,000,000 output
        const long crossScratch = 678_498_304;                            // 4 * (13024*13024)
        const long crossEstimate = crossPlanesOutput + crossScratch;      // 1,460,513,664
        Assert.True(crossPlanesOutput < (1L << 30), "pre-fix plane+output estimate is under the default budget (the vulnerability)");
        Assert.True(crossEstimate > (1L << 30), "corrected estimate exceeds the default budget");

        var bytes = BuildSofSos(JpegMarkers.StartOfFrameBaseline, 8, 13000, 13000, [0x41, 0x14, 0x11, 0x11]);

        var sw = Stopwatch.StartNew();
        var ex = Record.Exception(() => Jpeg.Decode(bytes)); // default options
        sw.Stop();

        Assert.NotNull(ex);
        Assert.IsAssignableFrom<JpegException>(ex);
        Assert.IsNotType<OutOfMemoryException>(ex);
        Assert.Contains("Estimated decode allocation", ex.Message);
        Assert.Contains(crossEstimate.ToString(), ex.Message); // exact value proves cross-subsampling scratch counted
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"guard took {sw.Elapsed}");
    }

    private static byte[] BuildSofSos(byte sofMarker, byte precision, int width, int height, byte[] samplingBytes)
    {
        var frame = new List<byte>
        {
            precision,
            (byte)(height >> 8), (byte)(height & 0xFF),
            (byte)(width >> 8), (byte)(width & 0xFF),
            (byte)samplingBytes.Length,
        };
        byte id = 1;
        foreach (var s in samplingBytes)
        {
            frame.Add(id++);
            frame.Add(s);
            frame.Add(0);
        }

        var scan = new List<byte> { (byte)samplingBytes.Length };
        for (byte i = 0; i < samplingBytes.Length; i++)
        {
            scan.Add((byte)(i + 1));
            scan.Add(0x00);
        }
        scan.Add(0x00); // Ss
        scan.Add(0x3F); // Se
        scan.Add(0x00); // Ah/Al

        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, sofMarker]);
        WriteSegment(ms, frame);
        ms.Write([0xFF, JpegMarkers.StartOfScan]);
        WriteSegment(ms, scan);
        ms.Write([0xFF, 0xD9]);
        return ms.ToArray();
    }

    private static void WriteSegment(MemoryStream ms, IReadOnlyCollection<byte> payload)
    {
        var len = payload.Count + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        foreach (var b in payload)
            ms.WriteByte(b);
    }

    private static byte[] Gradient(int w, int h, int ch)
    {
        var d = new byte[w * h * ch];
        for (var i = 0; i < d.Length; i++)
            d[i] = (byte)(i % 256);
        return d;
    }

    private static ushort[] Gradient12(int w, int h, int ch)
    {
        var d = new ushort[w * h * ch];
        for (var i = 0; i < d.Length; i++)
            d[i] = (ushort)(i % 4096);
        return d;
    }
}

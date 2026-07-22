using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Markers;
using Xunit;

namespace JpegSharp.Tests;

public class SofFrameComplianceTests
{
    // ---- JPEG-SOF-01: Σ(Hi·Vi) ≤ 10 applies only to interleaved scans (Ns>1) ----

    [Theory]
    [InlineData(new byte[] { 0x43 })]  // single component H=4,V=3 => 12
    [InlineData(new byte[] { 0x44 })]  // H=4,V=4 => 16
    public void Sof_SingleComponentHighSamplingFactor_NotRejectedAtFrameHeader(byte[] sampling)
    {
        // T.81 A.2.2 scopes the Σ≤10 limit to interleaved scans; a single-component frame is exempt.
        var bytes = BuildFrame(JpegMarkers.StartOfFrameBaseline, 8, sampling);
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.NotNull(ex);
        Assert.DoesNotContain("exceeds the maximum of 10", ex!.Message);
    }

    [Theory]
    [InlineData(new byte[] { 0x42, 0x11, 0x21 })]  // 8 + 1 + 2 => 11
    [InlineData(new byte[] { 0x44, 0x11, 0x11 })]  // 16 + 1 + 1 => 18
    public void Sof_InterleavedScanSamplingSumExceeds10_Rejected(byte[] sampling)
    {
        var bytes = BuildFrameWithScan(JpegMarkers.StartOfFrameBaseline, 8, sampling);
        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes));
        Assert.Contains("exceeds the maximum of 10", ex.Message);
    }

    [Fact]
    public void Sof_InterleavedScanSamplingSumEqualsTen_Parses()
    {
        // Y=4x2 (8) + Cb=1x1 (1) + Cr=1x1 (1) = 10, exactly the maximum.
        var bytes = BuildFrameWithScan(JpegMarkers.StartOfFrameBaseline, 8, [0x42, 0x11, 0x11]);
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        // Scan parsing must not reject Σ=10; the decode fails later (no entropy data / tables).
        Assert.NotNull(ex);
        Assert.DoesNotContain("exceeds the maximum of 10", ex!.Message);
    }

    [Fact]
    public void Sof_TypicalSubsampledImage_StillDecodes()
    {
        // Real 4:2:0 output has Σ = 4 + 1 + 1 = 6; the new guard must not regress it.
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Subsampling = ChromaSubsampling.Samp420 }));
        Assert.Equal(16, decoded.Width);
    }

    // ---- JPEG-SOF-02: precision tied to frame type ----

    [Theory]
    [InlineData((byte)12)]
    [InlineData((byte)16)]
    public void Sof0_NonEightBitPrecision_Rejected(byte precision)
    {
        var bytes = BuildFrame(JpegMarkers.StartOfFrameBaseline, precision, [0x11]);
        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes));
        Assert.Contains("precision", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(JpegMarkers.StartOfFrameExtendedSequential)]
    [InlineData(JpegMarkers.StartOfFrameProgressive)]
    public void Sof2_TwelveBit_Accepted(byte sofMarker)
    {
        // A hand-built 12-bit SOF1/SOF2 must pass the precision gate (it fails later on missing data).
        var bytes = BuildFrame(sofMarker, 12, [0x11]);
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.NotNull(ex);
        Assert.DoesNotContain("Unsupported sample precision", ex!.Message);
    }

    [Fact]
    public void Sof1_TwelveBit_RealImage_RoundTrips()
    {
        // The 12-bit encoder emits SOF1; the full pipeline must still round-trip.
        var image = JpegImage16.CreateGrayscale(16, 16, 12, new ushort[16 * 16]);
        var decoded = Jpeg.DecodeAnyPrecision(Jpeg.Encode16(image));
        Assert.Equal(12, decoded.Precision);
    }

    [Theory]
    [InlineData(JpegMarkers.StartOfFrameBaseline, (byte)9)]
    [InlineData(JpegMarkers.StartOfFrameBaseline, (byte)10)]
    [InlineData(JpegMarkers.StartOfFrameBaseline, (byte)11)]
    [InlineData(JpegMarkers.StartOfFrameBaseline, (byte)16)]
    [InlineData(JpegMarkers.StartOfFrameExtendedSequential, (byte)9)]
    [InlineData(JpegMarkers.StartOfFrameExtendedSequential, (byte)10)]
    [InlineData(JpegMarkers.StartOfFrameExtendedSequential, (byte)11)]
    [InlineData(JpegMarkers.StartOfFrameExtendedSequential, (byte)16)]
    [InlineData(JpegMarkers.StartOfFrameProgressive, (byte)9)]
    [InlineData(JpegMarkers.StartOfFrameProgressive, (byte)10)]
    [InlineData(JpegMarkers.StartOfFrameProgressive, (byte)11)]
    [InlineData(JpegMarkers.StartOfFrameProgressive, (byte)16)]
    public void Sof_UnsupportedPrecision_Rejected(byte sofMarker, byte precision)
    {
        var bytes = BuildFrame(sofMarker, precision, [0x11]);
        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes));
        Assert.Contains("precision", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- JPEG-SOF-03: DAC (arithmetic conditioning) rejected ----

    [Fact]
    public void Dac_Marker_Rejected()
    {
        // Insert a DAC (0xCC) segment into an otherwise valid stream, right after SOI.
        var image = JpegImage.CreateGrayscale(8, 8, new byte[64]);
        var baseBytes = Jpeg.Encode(image);

        byte[] dac = [0xFF, 0xCC, 0x00, 0x03, 0x00]; // length 3, one conditioning byte
        using var ms = new MemoryStream();
        ms.Write(baseBytes, 0, 2); // SOI
        ms.Write(dac);
        ms.Write(baseBytes, 2, baseBytes.Length - 2);

        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(ms.ToArray()));
        Assert.Contains("arithmetic", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] BuildFrame(byte sofMarker, byte precision, byte[] samplingBytes)
    {
        // SOI + SOF(marker) for a 16x16 frame with one component per sampling byte, then EOI.
        var payload = new List<byte> { precision, 0x00, 0x10, 0x00, 0x10, (byte)samplingBytes.Length };
        byte id = 1;
        foreach (var s in samplingBytes)
        {
            payload.Add(id++);
            payload.Add(s);
            payload.Add(0); // quantization table id
        }

        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, sofMarker]);
        var len = payload.Count + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        ms.Write(payload.ToArray());
        ms.Write([0xFF, 0xD9]);
        return ms.ToArray();
    }

    private static byte[] BuildFrameWithScan(byte sofMarker, byte precision, byte[] samplingBytes)
    {
        // SOI + SOF + SOS interleaving every component, then EOI. Reaches scan-header parsing.
        var frame = new List<byte> { precision, 0x00, 0x10, 0x00, 0x10, (byte)samplingBytes.Length };
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
            scan.Add((byte)(i + 1)); // component selector
            scan.Add(0x00);          // DC/AC table ids
        }
        scan.Add(0x00); // Ss
        scan.Add(0x3F); // Se
        scan.Add(0x00); // Ah/Al

        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, sofMarker]);
        var frameLen = frame.Count + 2;
        ms.WriteByte((byte)(frameLen >> 8));
        ms.WriteByte((byte)(frameLen & 0xFF));
        ms.Write(frame.ToArray());
        ms.Write([0xFF, JpegMarkers.StartOfScan]);
        var scanLen = scan.Count + 2;
        ms.WriteByte((byte)(scanLen >> 8));
        ms.WriteByte((byte)(scanLen & 0xFF));
        ms.Write(scan.ToArray());
        ms.Write([0xFF, 0xD9]);
        return ms.ToArray();
    }
}

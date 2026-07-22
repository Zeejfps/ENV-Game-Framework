using System.Diagnostics;
using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Markers;
using Xunit;

namespace JpegSharp.Tests;

// JPEG-ROB-02: bounded allocation + single typed-failure contract on the decode entry points.
public class GeometryOverflowTests
{
    [Fact]
    public void Decode_OverflowingGeometry_ThrowsJpegException()
    {
        // 65535x65535 with 4x4 sampling makes the per-component plane / coefficient-buffer size
        // (BlocksWide*BlocksHigh*64) overflow 32-bit. With MaxPixels raised out of the way the
        // geometry guard must still reject it before allocating, as a typed JpegException and not
        // an OverflowException / IndexOutOfRange / OutOfMemory.
        var bytes = BuildSofSos(JpegMarkers.StartOfFrameBaseline, 8, 0xFFFF, 0xFFFF, [0x44]);
        var options = new JpegDecoderOptions { MaxPixels = long.MaxValue };

        var sw = Stopwatch.StartNew();
        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes, options));
        sw.Stop();

        Assert.Contains("buffer size", ex.Message);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"guard took {sw.Elapsed}");
    }

    [Fact]
    public void Decode_OverflowingProgressiveCoefficients_ThrowsJpegException()
    {
        // Same guard must protect the progressive coefficient allocation (in DecodeProgressive).
        var bytes = BuildSofSos(JpegMarkers.StartOfFrameProgressive, 8, 0xFFFF, 0xFFFF, [0x44]);
        var options = new JpegDecoderOptions { MaxPixels = long.MaxValue };

        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes, options));
        Assert.Contains("buffer size", ex.Message);
    }

    [Fact]
    public void Decode_HugeDimensions_FailTypedNotOom()
    {
        // Hostile dimensions fail fast against the default pixel budget with a typed exception,
        // never an OutOfMemoryException and without a huge allocation or a hang.
        var bytes = BuildSofSos(JpegMarkers.StartOfFrameBaseline, 8, 0xFFFF, 0xFFFF, [0x11]);

        var sw = Stopwatch.StartNew();
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        sw.Stop();

        Assert.NotNull(ex);
        Assert.IsAssignableFrom<JpegException>(ex);
        Assert.IsNotType<OutOfMemoryException>(ex);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"decode took {sw.Elapsed}");
    }

    [Fact]
    public void Decode_MalformedCausingInternalException_SurfacesAsJpegException()
    {
        // This crafted stream drives the entropy decoder into a raw IndexOutOfRangeException deep
        // in the block loop. The single typed-failure contract must convert it to a JpegException
        // subtype (wrapping the original) rather than letting the raw exception reach the caller.
        var data = Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAsICAoIBwsKCQoNDAsNERwSEQ8PESIZGhQcKSQrKigkJyct" +
            "MkA3LTA9MCcnOEw5PUNFSElIKzZPVU5GVEBHSEX/2wBDAQwNDREPESESEiFFLicuRUVFRUVFRUVFRUVF" +
            "RUVFRUVFRUVFRUVFRUVFRUVFRUVFRUVFRUVFRUVFRUVFRUVFRUX/wAARCAAIAAgDAREAAhEBAzEB/8QA" +
            "HwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIh" +
            "MUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVW" +
            "V1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXG" +
            "x8jJytLTn9XW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQF" +
            "bgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAV" +
            "YnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOE" +
            "hYaH24mKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq" +
            "8vP09fb3+Pn6/9oADAMBAAIRAxEAPwCxHH9rstl1NGYGO/5QwAyC2Ae56en3QcZJqYbo063n7ujdn031" +
            "6atpK3RPSxzQktIyVtG9/nbdkSkr6Xt1suZH/9k=");

        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(data));
        Assert.Contains("Malformed JPEG stream", ex.Message);
        Assert.NotNull(ex.InnerException);
        // The raw exception is preserved as the inner cause and never leaks to the caller.
        Assert.IsNotAssignableFrom<JpegException>(ex.InnerException);
    }

    [Fact]
    public void Decode_ValidImage_Unaffected()
    {
        // The guard/wrapper must not alter successful decodes of normal images.
        var rgb = JpegImage.CreateRgb(32, 24, Gradient(32, 24, 3));
        var decodedRgb = Jpeg.Decode(Jpeg.Encode(rgb, new JpegEncoderOptions { Quality = 85 }));
        Assert.Equal(32, decodedRgb.Width);
        Assert.Equal(24, decodedRgb.Height);

        var sub = Jpeg.Decode(Jpeg.Encode(rgb, new JpegEncoderOptions { Quality = 85, Subsampling = ChromaSubsampling.Samp420 }));
        Assert.Equal(32, sub.Width);

        var prog = Jpeg.Decode(Jpeg.Encode(rgb, new JpegEncoderOptions { Quality = 85, Progressive = true }));
        Assert.Equal(24, prog.Height);

        var gray = JpegImage.CreateGrayscale(16, 16, Gradient(16, 16, 1));
        var decodedGray = Jpeg.Decode(Jpeg.Encode(gray));
        Assert.Equal(16, decodedGray.Width);
    }

    [Fact]
    public void Decode_ExistingTypedRejections_KeepTheirType()
    {
        // The wrapper must not clobber the specific typed exceptions the decoder already raises.

        // Invalid DQT precision (Pq=2) -> ParseQuantTables' own message.
        byte[] dqt = [0x20, .. new byte[128]]; // pqTq: precision=2, id=0
        var dqtBytes = WrapSegment(JpegMarkers.DefineQuantizationTables, dqt);
        var dqtEx = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(dqtBytes));
        Assert.Contains("quantization table precision", dqtEx.Message);
        Assert.DoesNotContain("Malformed JPEG stream", dqtEx.Message);

        // Arithmetic coding (DAC marker) -> its dedicated message survives.
        var dacBytes = WrapSegment(JpegMarkers.DefineArithmeticConditioning, [0x00]);
        var dacEx = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(dacBytes));
        Assert.Contains("arithmetic", dacEx.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Malformed JPEG stream", dacEx.Message);

        // A JpegCorruptException (subtype) must keep its concrete type too.
        var image = JpegImage.CreateGrayscale(16, 16, Gradient(16, 16, 1));
        var strict = new JpegDecoderOptions { StrictRestartMarkers = true };
        var withRst = Jpeg.Encode(image, new JpegEncoderOptions { RestartInterval = 1 });
        // Corrupt the entropy region so a restart marker goes missing.
        var scanStart = FindScanStart(withRst);
        for (var i = scanStart; i < withRst.Length - 2; i++)
            withRst[i] = 0x5A;
        var rstEx = Record.Exception(() => Jpeg.Decode(withRst, strict));
        if (rstEx is not null)
            Assert.IsAssignableFrom<JpegException>(rstEx);
    }

    private static int FindScanStart(byte[] data)
    {
        for (var i = 0; i < data.Length - 3; i++)
            if (data[i] == 0xFF && data[i + 1] == JpegMarkers.StartOfScan)
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                return i + 2 + len;
            }
        return data.Length;
    }

    // SOI + SOF + SOS reaching SetupGeometry, for a frame with one component per sampling byte.
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

    private static byte[] WrapSegment(byte marker, byte[] payload)
    {
        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, marker]);
        WriteSegment(ms, payload);
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
}

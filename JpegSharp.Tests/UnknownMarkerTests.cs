using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class UnknownMarkerTests
{
    [Fact]
    public void UnknownAppSegment_SurvivesRoundTrip()
    {
        var metadata = new JpegMetadata();
        metadata.ApplicationSegments.Add(new JpegApplicationSegment(0xE3, [1, 2, 3, 4, 5]));
        metadata.ApplicationSegments.Add(new JpegApplicationSegment(0xEB, [9, 8, 7]));

        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(bytes);
        var segments = decoded.Metadata!.ApplicationSegments;
        Assert.Equal(2, segments.Count);
        Assert.Equal((byte)0xE3, segments[0].MarkerCode);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, segments[0].Data);
        Assert.Equal((byte)0xEB, segments[1].MarkerCode);
        Assert.Equal(new byte[] { 9, 8, 7 }, segments[1].Data);
    }

    [Fact]
    public void RecognizedSegments_AreNotDuplicatedAsUnknown()
    {
        // JFIF (APP0), Exif (APP1) are parsed into typed fields, not preserved raw.
        var metadata = new JpegMetadata
        {
            Density = new JfifDensity(JpegDensityUnit.DotsPerInch, 72, 72),
            Exif = [0x49, 0x49, 0x2A, 0x00],
        };
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));

        Assert.Empty(decoded.Metadata!.ApplicationSegments);
        Assert.NotNull(decoded.Metadata.Density);
        Assert.NotNull(decoded.Metadata.Exif);
    }

    [Fact]
    public void UnknownAppSegment_InHandBuiltStream_IsPreserved()
    {
        // Build a stream with an APP5 segment the codec does not interpret.
        var image = JpegImage.CreateGrayscale(8, 8, new byte[64]);
        var baseBytes = Jpeg.Encode(image);

        // Insert an APP5 segment right after SOI.
        var app5 = new byte[] { 0xFF, 0xE5, 0x00, 0x05, 0xAA, 0xBB, 0xCC };
        using var ms = new MemoryStream();
        ms.Write(baseBytes, 0, 2); // SOI
        ms.Write(app5);
        ms.Write(baseBytes, 2, baseBytes.Length - 2);

        var decoded = Jpeg.Decode(ms.ToArray());
        var seg = Assert.Single(decoded.Metadata!.ApplicationSegments);
        Assert.Equal((byte)0xE5, seg.MarkerCode);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, seg.Data);
    }

    [Fact]
    public void NonMatchingApp0_IsPreservedRatherThanTreatedAsJfif()
    {
        var metadata = new JpegMetadata();
        // An APP0 that is not "JFIF\0".
        metadata.ApplicationSegments.Add(new JpegApplicationSegment(0xE0, [(byte)'X', (byte)'Y', 0]));

        var image = JpegImage.CreateGrayscale(8, 8, new byte[64]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));

        // The encoder writes its own JFIF APP0; the custom non-JFIF APP0 is preserved too.
        Assert.Contains(decoded.Metadata!.ApplicationSegments, s => s.MarkerCode == 0xE0);
    }
}

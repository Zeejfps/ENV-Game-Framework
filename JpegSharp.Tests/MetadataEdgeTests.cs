using System.Text;
using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class MetadataEdgeTests
{
    [Fact]
    public void UnicodeComment_RoundTrips()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add("café — naïve — 日本語 — 😀");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);

        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));
        Assert.Equal("café — naïve — 日本語 — 😀", decoded.Metadata!.Comments[0]);
    }

    [Fact]
    public void ExifContainingFfAndZeroBytes_SurvivesIntact()
    {
        // Segment payloads are length-prefixed, so 0xFF/0x00 must NOT be byte-stuffed or
        // mistaken for markers.
        var exif = new byte[512];
        for (var i = 0; i < exif.Length; i++)
            exif[i] = (byte)(i % 2 == 0 ? 0xFF : 0x00);
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);

        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = new JpegMetadata { Exif = exif } }));
        Assert.Equal(exif, decoded.Metadata!.Exif);
    }

    [Fact]
    public void IccContainingMarkerLikeBytes_SurvivesIntact()
    {
        var rng = new Random(77);
        var icc = new byte[70_000]; // spans multiple APP2 segments
        rng.NextBytes(icc);
        // Sprinkle explicit marker-like sequences.
        for (var i = 0; i + 1 < icc.Length; i += 100)
        {
            icc[i] = 0xFF;
            icc[i + 1] = 0xD8; // looks like an SOI
        }

        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = new JpegMetadata { IccProfile = icc } }));
        Assert.Equal(icc, decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void EmptyComment_RoundTrips()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add(string.Empty);
        var image = JpegImage.CreateGrayscale(8, 8, new byte[64]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));
        var comment = Assert.Single(decoded.Metadata!.Comments);
        Assert.Equal(string.Empty, comment);
    }

    [Fact]
    public void MaximumSizeSegment_IsAccepted()
    {
        // An application segment payload at the 65533-byte limit must encode and round-trip.
        var data = new byte[65533 - 2]; // minus the identifier is not needed for a raw APP segment
        new Random(5).NextBytes(data);
        var metadata = new JpegMetadata();
        metadata.ApplicationSegments.Add(new JpegApplicationSegment(0xE7, data));

        var image = JpegImage.CreateGrayscale(8, 8, new byte[64]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata }));
        var seg = Assert.Single(decoded.Metadata!.ApplicationSegments);
        Assert.Equal(0xE7, seg.MarkerCode);
        Assert.Equal(data, seg.Data);
    }
}

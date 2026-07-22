using JpegSharp.Api;
using Xunit;

namespace JpegSharp.Tests;

public class MetadataTests
{
    [Fact]
    public void Identify_ReadsDimensionsAndColorSpace_ForRgb()
    {
        var image = JpegImage.CreateRgb(37, 19, new byte[37 * 19 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 80 });

        var info = Jpeg.Identify(bytes);
        Assert.Equal(37, info.Width);
        Assert.Equal(19, info.Height);
        Assert.Equal(3, info.ComponentCount);
        Assert.Equal(JpegColorSpace.Rgb, info.ColorSpace);
        Assert.Equal(8, info.Precision);
        Assert.False(info.IsProgressive);
    }

    [Fact]
    public void Identify_ReadsGrayscaleAndCmyk()
    {
        var gray = Jpeg.Encode(JpegImage.CreateGrayscale(8, 8, new byte[64]));
        Assert.Equal(JpegColorSpace.Grayscale, Jpeg.Identify(gray).ColorSpace);
        Assert.Equal(1, Jpeg.Identify(gray).ComponentCount);

        var cmyk = Jpeg.Encode(JpegImage.CreateCmyk(8, 8, new byte[64 * 4]));
        Assert.Equal(JpegColorSpace.Cmyk, Jpeg.Identify(cmyk).ColorSpace);
        Assert.Equal(4, Jpeg.Identify(cmyk).ComponentCount);
    }

    [Fact]
    public void Exif_SurvivesRoundTrip()
    {
        var exif = new byte[] { 0x49, 0x49, 0x2A, 0x00, 1, 2, 3, 4, 5, 6, 7, 8 };
        var metadata = new JpegMetadata { Exif = exif };
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(bytes);
        Assert.NotNull(decoded.Metadata);
        Assert.Equal(exif, decoded.Metadata!.Exif);
    }

    [Fact]
    public void SmallIccProfile_SurvivesRoundTrip()
    {
        var icc = new byte[500];
        for (var i = 0; i < icc.Length; i++)
            icc[i] = (byte)(i * 7);
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = new JpegMetadata { IccProfile = icc } });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(icc, decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void LargeIccProfile_IsChunkedAndReassembled()
    {
        var icc = new byte[200_000]; // exceeds one APP2 segment; must chunk across several
        var rng = new Random(4);
        rng.NextBytes(icc);
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = new JpegMetadata { IccProfile = icc } });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(icc, decoded.Metadata!.IccProfile);
    }

    [Fact]
    public void Comments_SurviveRoundTrip()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add("Created by JpegSharp");
        metadata.Comments.Add("Second comment");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(new[] { "Created by JpegSharp", "Second comment" }, decoded.Metadata!.Comments);
    }

    [Fact]
    public void Com_BinaryNonUtf8Bytes_RoundTripLossless()
    {
        var binary = new byte[256];
        for (var i = 0; i < binary.Length; i++)
            binary[i] = (byte)i;
        var invalidUtf8 = new byte[] { 0xFF, 0xFE, 0x80, 0xC0, 0x00, 0xFD };

        var metadata = new JpegMetadata();
        metadata.CommentBytes.Add(binary);
        metadata.CommentBytes.Add(invalidUtf8);
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(2, decoded.Metadata!.CommentBytes.Count);
        Assert.Equal(binary, decoded.Metadata.CommentBytes[0]);
        Assert.Equal(invalidUtf8, decoded.Metadata.CommentBytes[1]);
    }

    [Fact]
    public void Com_StringComments_StillRoundTrip()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add("Created by JpegSharp");
        metadata.Comments.Add("Second comment");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        Assert.Equal(2, CountComSegments(bytes));

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(new[] { "Created by JpegSharp", "Second comment" }, decoded.Metadata!.Comments);
    }

    [Fact]
    public void Com_DecodeThenReencode_NoDuplicateSegments()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add("Alpha");
        metadata.Comments.Add("Beta");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var original = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });
        Assert.Equal(2, CountComSegments(original));

        var decoded = Jpeg.Decode(original);
        var reencoded = Jpeg.Encode(decoded, new JpegEncoderOptions { Metadata = decoded.Metadata });

        Assert.Equal(2, CountComSegments(reencoded));
        var redecoded = Jpeg.Decode(reencoded);
        Assert.Equal(decoded.Metadata!.CommentBytes[0], redecoded.Metadata!.CommentBytes[0]);
        Assert.Equal(decoded.Metadata.CommentBytes[1], redecoded.Metadata.CommentBytes[1]);
    }

    [Fact]
    public void Com_MultipleComments_PreservedInOrder()
    {
        var metadata = new JpegMetadata();
        metadata.CommentBytes.Add(new byte[] { 1 });
        metadata.CommentBytes.Add(new byte[] { 2, 2 });
        metadata.CommentBytes.Add(new byte[] { 3, 3, 3 });
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        Assert.Equal(3, CountComSegments(bytes));
        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(new byte[] { 1 }, decoded.Metadata!.CommentBytes[0]);
        Assert.Equal(new byte[] { 2, 2 }, decoded.Metadata.CommentBytes[1]);
        Assert.Equal(new byte[] { 3, 3, 3 }, decoded.Metadata.CommentBytes[2]);
    }

    private static int CountComSegments(byte[] jpeg)
    {
        var count = 0;
        for (var i = 0; i + 1 < jpeg.Length; i++)
        {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == 0xFE)
                count++;
        }

        return count;
    }

    [Fact]
    public void JfifDensity_SurvivesRoundTrip()
    {
        var metadata = new JpegMetadata { Density = new JfifDensity(JpegDensityUnit.DotsPerInch, 300, 300) };
        var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(bytes);
        Assert.Equal(new JfifDensity(JpegDensityUnit.DotsPerInch, 300, 300), decoded.Metadata!.Density);
    }

    [Fact]
    public void AdobeTransform_IsCapturedForCmyk()
    {
        var image = JpegImage.CreateCmyk(8, 8, new byte[64 * 4]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image));
        Assert.Equal(0, decoded.Metadata!.AdobeColorTransform);
    }

    [Fact]
    public void Decode_WithoutMetadata_HasEmptyMetadata()
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var decoded = Jpeg.Decode(Jpeg.Encode(image));
        Assert.NotNull(decoded.Metadata);
        Assert.Null(decoded.Metadata!.Exif);
        Assert.Null(decoded.Metadata.IccProfile);
        Assert.Empty(decoded.Metadata.Comments);
    }
}

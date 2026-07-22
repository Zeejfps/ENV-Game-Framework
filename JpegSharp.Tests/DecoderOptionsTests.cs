using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class DecoderOptionsTests
{
    [Fact]
    public void MaxPixels_RejectsImagesThatExceedTheLimit()
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]); // 256 pixels
        var bytes = Jpeg.Encode(image);

        var options = new JpegDecoderOptions { MaxPixels = 100 };
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes, options));
    }

    [Fact]
    public void MaxPixels_AllowsImagesWithinTheLimit()
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image);

        var options = new JpegDecoderOptions { MaxPixels = 1000 };
        var decoded = Jpeg.Decode(bytes, options);
        Assert.Equal(16, decoded.Width);
    }

    [Fact]
    public void ReadMetadata_False_SkipsMetadata()
    {
        var metadata = new JpegMetadata();
        metadata.Comments.Add("hello");
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Metadata = metadata });

        var decoded = Jpeg.Decode(bytes, new JpegDecoderOptions { ReadMetadata = false });
        Assert.Null(decoded.Metadata);
    }

    [Fact]
    public void ReadMetadata_True_IsDefault()
    {
        var image = JpegImage.CreateGrayscale(16, 16, new byte[256]);
        var bytes = Jpeg.Encode(image);
        var decoded = Jpeg.Decode(bytes, new JpegDecoderOptions());
        Assert.NotNull(decoded.Metadata);
    }
}

using WebPSharp.Api;

namespace WebPSharp.Tests;

public class WebPMetadataTests
{
    private static WebPImage MakeImage(int seed = 1)
    {
        var rng = new Random(seed);
        var pixels = new byte[8 * 8 * 4];
        rng.NextBytes(pixels);
        return WebPImage.CreateRgba(8, 8, pixels);
    }

    [Fact]
    public void EncodeDecode_IccExifXmp_Survives()
    {
        var image = MakeImage();
        image.Metadata = new WebPMetadata
        {
            IccProfile = new byte[] { 1, 2, 3, 4, 5 },
            Exif = new byte[] { 10, 20, 30 },
            Xmp = "<x:xmpmeta/>"u8.ToArray(),
        };

        var decoded = WebP.Decode(WebP.Encode(image));

        Assert.Equal(image.PixelData, decoded.PixelData);
        Assert.NotNull(decoded.Metadata);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, decoded.Metadata!.IccProfile);
        Assert.Equal(new byte[] { 10, 20, 30 }, decoded.Metadata.Exif);
        Assert.Equal("<x:xmpmeta/>"u8.ToArray(), decoded.Metadata.Xmp);
    }

    [Fact]
    public void EncodeWithMetadata_ProducesExtendedContainer()
    {
        var image = MakeImage();
        image.Metadata = new WebPMetadata { Exif = new byte[] { 1 } };
        var bytes = WebP.Encode(image);

        var info = WebP.Identify(bytes);
        Assert.Equal(WebPFormat.Extended, info.Format);
        Assert.True(info.HasAlpha);
        Assert.Equal(8, info.Width);
        Assert.Equal(8, info.Height);
    }

    [Fact]
    public void EncodeWithoutMetadata_ProducesSimpleContainer()
    {
        var bytes = WebP.Encode(MakeImage());
        Assert.Equal(WebPFormat.Lossless, WebP.Identify(bytes).Format);
    }

    [Fact]
    public void UnknownChunks_ArePreserved()
    {
        var image = MakeImage();
        image.Metadata = new WebPMetadata();
        image.Metadata.UnknownChunks.Add(new WebPUnknownChunk("TEST", new byte[] { 7, 7, 7 }));
        image.Metadata.IccProfile = new byte[] { 9 };

        var decoded = WebP.Decode(WebP.Encode(image));

        Assert.NotNull(decoded.Metadata);
        var unknown = Assert.Single(decoded.Metadata!.UnknownChunks);
        Assert.Equal("TEST", unknown.Id);
        Assert.Equal(new byte[] { 7, 7, 7 }, unknown.Data);
    }

    [Fact]
    public void OnlyIcc_SetsIccFlagOnly()
    {
        var image = MakeImage();
        image.Metadata = new WebPMetadata { IccProfile = new byte[] { 1, 2 } };
        var decoded = WebP.Decode(WebP.Encode(image));
        Assert.Equal(new byte[] { 1, 2 }, decoded.Metadata!.IccProfile);
        Assert.Null(decoded.Metadata.Exif);
        Assert.Null(decoded.Metadata.Xmp);
    }

    [Fact]
    public void ReadMetadataFalse_SkipsMetadata()
    {
        var image = MakeImage();
        image.Metadata = new WebPMetadata { Exif = new byte[] { 1, 2, 3 } };
        var bytes = WebP.Encode(image);

        var decoded = WebP.Decode(bytes, new WebPDecoderOptions { ReadMetadata = false });
        Assert.Equal(image.PixelData, decoded.PixelData);
        Assert.Null(decoded.Metadata);
    }

    [Fact]
    public void EmptyMetadata_ProducesSimpleContainer()
    {
        var image = MakeImage();
        image.Metadata = new WebPMetadata(); // present but empty
        Assert.Equal(WebPFormat.Lossless, WebP.Identify(WebP.Encode(image)).Format);
    }
}

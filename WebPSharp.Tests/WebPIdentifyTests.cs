using WebPSharp.Api;
using WebPSharp.Api.Exceptions;
using WebPSharp.Container;

namespace WebPSharp.Tests;

public class WebPIdentifyTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(16, 16)]
    [InlineData(640, 480)]
    [InlineData(16383, 16383)]
    public void Identify_Lossy_ReadsDimensions(int width, int height)
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8, WebPTestData.Vp8Header(width, height));
        var info = WebP.Identify(bytes);

        Assert.Equal(width, info.Width);
        Assert.Equal(height, info.Height);
        Assert.Equal(WebPFormat.Lossy, info.Format);
        Assert.False(info.IsLossless);
        Assert.False(info.HasAlpha);
        Assert.False(info.HasAnimation);
    }

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(320, 240, true)]
    [InlineData(16384, 16384, false)]
    public void Identify_Lossless_ReadsDimensionsAndAlpha(int width, int height, bool alpha)
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(width, height, alpha));
        var info = WebP.Identify(bytes);

        Assert.Equal(width, info.Width);
        Assert.Equal(height, info.Height);
        Assert.Equal(WebPFormat.Lossless, info.Format);
        Assert.True(info.IsLossless);
        Assert.Equal(alpha, info.HasAlpha);
        Assert.False(info.HasAnimation);
    }

    [Theory]
    [InlineData(100, 200, false, false)]
    [InlineData(100, 200, true, false)]
    [InlineData(100, 200, false, true)]
    [InlineData(100, 200, true, true)]
    public void Identify_Extended_ReadsFlags(int width, int height, bool alpha, bool anim)
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8X, WebPTestData.Vp8XHeader(width, height, alpha, anim));
        var info = WebP.Identify(bytes);

        Assert.Equal(width, info.Width);
        Assert.Equal(height, info.Height);
        Assert.Equal(WebPFormat.Extended, info.Format);
        Assert.Equal(alpha, info.HasAlpha);
        Assert.Equal(anim, info.HasAnimation);
    }

    [Fact]
    public void Identify_Stream_MatchesByteArray()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, WebPTestData.Vp8LHeader(7, 9, false));
        using var ms = new MemoryStream(bytes);
        var info = WebP.Identify(ms);
        Assert.Equal(7, info.Width);
        Assert.Equal(9, info.Height);
    }

    [Fact]
    public void Identify_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WebP.Identify((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => WebP.Identify((Stream)null!));
    }

    [Fact]
    public void Identify_UnknownLeadingChunk_Throws()
    {
        var bytes = WebPTestData.Container(new FourCc("JUNK"), new byte[] { 1, 2, 3, 4 });
        Assert.Throws<WebPFormatException>(() => WebP.Identify(bytes));
    }

    [Fact]
    public void Identify_TruncatedVp8Header_Throws()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8, new byte[] { 0, 0, 0 });
        Assert.Throws<WebPFormatException>(() => WebP.Identify(bytes));
    }

    [Fact]
    public void Identify_Vp8BadStartCode_Throws()
    {
        var header = WebPTestData.Vp8Header(10, 10);
        header[3] = 0x00; // corrupt start code
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8, header);
        Assert.Throws<WebPFormatException>(() => WebP.Identify(bytes));
    }

    [Fact]
    public void Identify_Vp8NotKeyFrame_Throws()
    {
        var header = WebPTestData.Vp8Header(10, 10);
        header[0] |= 0x01; // set key-frame bit -> interframe
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8, header);
        Assert.Throws<WebPFormatException>(() => WebP.Identify(bytes));
    }

    [Fact]
    public void Identify_Vp8LBadSignature_Throws()
    {
        var header = WebPTestData.Vp8LHeader(10, 10, false);
        header[0] = 0x00;
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, header);
        Assert.Throws<WebPFormatException>(() => WebP.Identify(bytes));
    }

    [Fact]
    public void Identify_NoChunks_Throws()
    {
        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        writer.Complete();
        Assert.Throws<WebPFormatException>(() => WebP.Identify(ms.ToArray()));
    }
}

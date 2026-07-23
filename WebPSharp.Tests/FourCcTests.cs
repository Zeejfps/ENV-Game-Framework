using WebPSharp.Container;

namespace WebPSharp.Tests;

public class FourCcTests
{
    [Fact]
    public void FromString_RoundTripsToString()
    {
        var code = new FourCc("VP8L");
        Assert.Equal("VP8L", code.ToString());
    }

    [Fact]
    public void FromString_WithTrailingSpace_Preserved()
    {
        var code = new FourCc("VP8 ");
        Assert.Equal("VP8 ", code.ToString());
    }

    [Theory]
    [InlineData("VP8")]
    [InlineData("VP8LL")]
    [InlineData("")]
    public void FromString_WrongLength_Throws(string tag)
    {
        Assert.Throws<ArgumentException>(() => new FourCc(tag));
    }

    [Fact]
    public void FromString_NonAscii_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FourCc("VP8ÿ"));
    }

    [Fact]
    public void ReadAndWrite_AreInverse()
    {
        Span<byte> buffer = stackalloc byte[4];
        var original = new FourCc("ANMF");
        original.Write(buffer);
        var read = FourCc.Read(buffer);
        Assert.Equal(original, read);
        Assert.Equal(new byte[] { (byte)'A', (byte)'N', (byte)'M', (byte)'F' }, buffer.ToArray());
    }

    [Fact]
    public void Packed_IsLittleEndianByteOrder()
    {
        var code = new FourCc((byte)'R', (byte)'I', (byte)'F', (byte)'F');
        var expected = (uint)('R' | ('I' << 8) | ('F' << 16) | ('F' << 24));
        Assert.Equal(expected, code.Packed);
    }

    [Fact]
    public void Equality_MatchesOnBytes()
    {
        Assert.Equal(new FourCc("VP8X"), new FourCc("VP8X"));
        Assert.NotEqual(new FourCc("VP8X"), new FourCc("VP8L"));
        Assert.True(new FourCc("VP8 ") == WebPChunkIds.Vp8);
        Assert.True(new FourCc("VP8L") != WebPChunkIds.Vp8);
    }

    [Fact]
    public void ToString_NonPrintableBytes_ShownAsQuestionMark()
    {
        var code = new FourCc(0x01, (byte)'A', 0x00, (byte)'Z');
        Assert.Equal("?A?Z", code.ToString());
    }
}

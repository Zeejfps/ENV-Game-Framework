using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class UnsupportedFrameTypeTests
{
    [Theory]
    [InlineData(0xC9)] // SOF9  — extended sequential, arithmetic
    [InlineData(0xCA)] // SOF10 — progressive, arithmetic
    [InlineData(0xCB)] // SOF11 — lossless, arithmetic
    [InlineData(0xC3)] // SOF3  — lossless, Huffman
    [InlineData(0xC5)] // SOF5  — differential sequential
    [InlineData(0xC7)] // SOF7  — differential lossless
    [InlineData(0xCD)] // SOF13 — differential arithmetic
    public void UnsupportedFrameType_ThrowsClearFormatException(byte sofMarker)
    {
        var bytes = BuildFrame(sofMarker);
        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes));
        Assert.Contains("SOF", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ArithmeticFrame_MessageMentionsUnsupported()
    {
        var ex = Assert.Throws<JpegFormatException>(() => Jpeg.Decode(BuildFrame(0xC9)));
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0xC0)] // baseline — supported (fails later for missing tables, not frame type)
    [InlineData(0xC1)] // extended sequential Huffman — supported
    [InlineData(0xC2)] // progressive Huffman — supported
    public void SupportedFrameType_DoesNotThrowFrameTypeError(byte sofMarker)
    {
        // These pass frame parsing; a full decode fails only for the missing scan/tables in
        // this minimal stream — but never for an unsupported frame type.
        var bytes = BuildFrame(sofMarker);
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        Assert.NotNull(ex);
        Assert.DoesNotContain("Unsupported frame type", ex!.Message);
    }

    private static byte[] BuildFrame(byte sofMarker)
    {
        // SOI + SOF(marker) for an 8x8 single-component 8-bit frame, then EOI.
        byte[] payload = [8, 0x00, 0x08, 0x00, 0x08, 1, 1, 0x11, 0];
        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, sofMarker]);
        var len = payload.Length + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        ms.Write(payload);
        ms.Write([0xFF, 0xD9]);
        return ms.ToArray();
    }
}

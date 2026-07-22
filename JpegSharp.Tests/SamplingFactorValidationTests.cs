using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using Xunit;

namespace JpegSharp.Tests;

public class SamplingFactorValidationTests
{
    [Theory]
    [InlineData(0x55)] // H=5, V=5 (both above the max of 4)
    [InlineData(0x51)] // H=5, V=1
    [InlineData(0x15)] // H=1, V=5
    [InlineData(0xF1)] // H=15, V=1
    public void SamplingFactorAboveFour_Throws(byte samplingByte)
    {
        var bytes = BuildSof(samplingByte);
        Assert.Throws<JpegFormatException>(() => Jpeg.Decode(bytes));
    }

    [Fact]
    public void ValidSamplingFactors_AreAcceptedByRealEncoderOutput()
    {
        // The encoder produces valid factors (1,2,4) for every subsampling mode; confirm they
        // all still decode after the [1,4] validation was added.
        foreach (var s in new[] { ChromaSubsampling.Samp444, ChromaSubsampling.Samp422, ChromaSubsampling.Samp420, ChromaSubsampling.Samp411 })
        {
            var image = JpegImage.CreateRgb(16, 16, new byte[16 * 16 * 3]);
            var decoded = Jpeg.Decode(Jpeg.Encode(image, new JpegEncoderOptions { Subsampling = s }));
            Assert.Equal(16, decoded.Width);
        }
    }

    private static byte[] BuildSof(byte samplingByte)
    {
        // SOI + SOF0 for an 8x8 single-component image with the given sampling factor byte.
        byte[] sof = [8, 0x00, 0x08, 0x00, 0x08, 1, 1, samplingByte, 0];
        using var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, 0xC0]);
        var len = sof.Length + 2;
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)(len & 0xFF));
        ms.Write(sof);
        // Terminate so Identify has a complete header region to scan (it stops at SOF).
        ms.Write([0xFF, 0xD9]);
        return ms.ToArray();
    }
}

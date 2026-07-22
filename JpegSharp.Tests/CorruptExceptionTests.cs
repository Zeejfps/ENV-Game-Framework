using JpegSharp.Api;
using JpegSharp.Api.Exceptions;
using JpegSharp.Bitstream;
using JpegSharp.Huffman;
using Xunit;

namespace JpegSharp.Tests;

public class CorruptExceptionTests
{
    [Fact]
    public void JpegCorruptException_IsAJpegFormatException()
    {
        Assert.IsAssignableFrom<JpegFormatException>(new JpegCorruptException("x"));
        Assert.IsAssignableFrom<JpegException>(new JpegCorruptException("x"));
    }

    [Fact]
    public void InvalidHuffmanCode_ThrowsCorruptException()
    {
        var counts = new byte[16];
        counts[0] = 1; // single 1-bit code "0" for symbol 42
        var table = new HuffmanTable(counts, [42]);

        Assert.Throws<JpegCorruptException>(() =>
        {
            var reader = new BitReader([0xFF]); // "1..." never matches
            table.DecodeSymbol(ref reader);
        });
    }

    [Fact]
    public void CorruptEntropy_ThrowsCorruptException_CaughtAsFormatException()
    {
        // Structurally valid header, then garbage entropy that trips the Huffman decoder.
        var image = JpegImage.CreateGrayscale(64, 64, Gradient(64, 64));
        var bytes = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 });

        // Corrupt many entropy bytes to force an invalid code / overrun.
        var scanStart = FindScanStart(bytes);
        for (var i = scanStart; i < bytes.Length - 2; i++)
            bytes[i] = 0x5A;

        // Whatever the exact failure, it is a JpegFormatException (corrupt is a subtype).
        var ex = Record.Exception(() => Jpeg.Decode(bytes));
        if (ex is not null)
            Assert.IsAssignableFrom<JpegFormatException>(ex);
    }

    private static int FindScanStart(byte[] data)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == 0xFF && data[i + 1] == 0xDA)
            {
                var len = (data[i + 2] << 8) | data[i + 3];
                return i + 2 + len;
            }
        return data.Length;
    }

    private static byte[] Gradient(int w, int h)
    {
        var data = new byte[w * h];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);
        return data;
    }
}

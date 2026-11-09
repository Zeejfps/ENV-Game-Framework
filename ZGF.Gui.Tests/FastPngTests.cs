using PngSharp.Api;
using ZGF.Gui.Hosting;
using ZGF.Gui.Mcp;

namespace ZGF.Gui.Tests;

public sealed class FastPngTests
{
    [Theory]
    [InlineData(37, 23)]
    [InlineData(2000, 1500)]
    public void A_fast_png_decodes_to_the_pixels_it_was_given_and_passes_the_structure_check(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        new Random(7).NextBytes(pixels);

        var png = FastPng.Encode(width, height, pixels);
        var decoded = Png.DecodeFromByteArray(png);

        Assert.Equal((uint)width, decoded.Ihdr.Width);
        Assert.Equal((uint)height, decoded.Ihdr.Height);
        Assert.Equal(pixels, decoded.PixelData);
        Assert.Equal((width, height), PngStructure.Validate(png));
        Assert.Equal((byte)0, png[16 + 10]);
        Assert.Equal((byte)0, png[16 + 11]);
        Assert.Equal((byte)0, png[16 + 12]);
    }

    [Fact]
    public void A_png_from_png_sharp_passes_the_structure_check()
    {
        var png = Png.EncodeToByteArray(Png.CreateRgba(9, 4, new byte[9 * 4 * 4]));

        Assert.Equal((9, 4), PngStructure.Validate(png));
    }

    [Theory]
    [InlineData("crc")]
    [InlineData("truncated")]
    [InlineData("no-iend")]
    [InlineData("no-idat")]
    [InlineData("trailing")]
    [InlineData("short-data")]
    [InlineData("signature")]
    public void A_broken_png_fails_the_structure_check(string damage)
    {
        var png = Damaged(FastPng.Encode(8, 8, new byte[8 * 8 * 4]), damage);

        Assert.Throws<FormatException>(() => PngStructure.Validate(png));
    }

    [Theory]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("a.b", true)]
    [InlineData("...", true)]
    public void A_path_token_is_never_a_dot_segment(string text, bool accepted)
    {
        Assert.Equal(accepted, McpPathToken.TryParse(text, out _));
    }

    /// <summary><paramref name="png"/>, one IHDR, one IDAT and one IEND chunk, broken as <paramref name="damage"/> says.</summary>
    private static byte[] Damaged(byte[] png, string damage)
    {
        const int ihdrEnd = 8 + 12 + 13;
        var idatLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(ihdrEnd, 4));
        var idatEnd = ihdrEnd + 12 + idatLength;
        switch (damage)
        {
            case "crc":
                var flipped = (byte[])png.Clone();
                flipped[ihdrEnd + 8] ^= 0xFF;
                return flipped;
            case "truncated":
                return png[..(png.Length - 5)];
            case "no-iend":
                return png[..idatEnd];
            case "no-idat":
                return [.. png[..ihdrEnd], .. png[idatEnd..]];
            case "trailing":
                return [.. png, 0];
            case "short-data":
                return Replace(png, 8, 9);
            case "signature":
                var wrong = (byte[])png.Clone();
                wrong[1] = (byte)'X';
                return wrong;
            default:
                throw new ArgumentOutOfRangeException(nameof(damage), damage, null);
        }
    }

    /// <summary>An 8x8 image's data under a header that claims <paramref name="height"/> rows.</summary>
    private static byte[] Replace(byte[] png, int width, int height)
    {
        var taller = FastPng.Encode(width, height, new byte[width * height * 4]);
        const int ihdrEnd = 8 + 12 + 13;
        return [.. taller[..ihdrEnd], .. png[ihdrEnd..]];
    }
}

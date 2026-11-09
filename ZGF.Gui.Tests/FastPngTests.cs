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

    [Fact]
    public void A_png_built_by_these_tests_passes_the_structure_check()
    {
        var png = Build(("IHDR", Header()), ("tEXt", "a\0b"u8.ToArray()), ("IDAT", Deflate(Rows(1))[..10]), ("IDAT", Deflate(Rows(1))[10..]), ("IEND", []));

        Assert.Equal((2, 2), PngStructure.Validate(png));
    }

    [Theory]
    [InlineData("filter-5")]
    [InlineData("second-row-filter-255")]
    [InlineData("indexed-without-plte")]
    [InlineData("indexed-with-plte")]
    [InlineData("grayscale")]
    [InlineData("rgb")]
    [InlineData("16-bit")]
    [InlineData("interlaced")]
    [InlineData("plte")]
    [InlineData("unknown-critical")]
    [InlineData("second-ihdr")]
    [InlineData("split-idat")]
    [InlineData("iend-data")]
    [InlineData("chunk-after-iend")]
    [InlineData("long-data")]
    [InlineData("zero-width")]
    public void A_png_a_capture_never_is_fails_the_structure_check(string kind)
    {
        var png = kind switch
        {
            "filter-5" => Build(("IHDR", Header()), ("IDAT", Deflate(Rows(5))), ("IEND", [])),
            "second-row-filter-255" => Build(("IHDR", Header()), ("IDAT", Deflate(SecondRowFilter(255))), ("IEND", [])),
            "indexed-without-plte" => Build(("IHDR", Header(colorType: 3)), ("IDAT", Deflate(Rows(0, bytesPerPixel: 1))), ("IEND", [])),
            "indexed-with-plte" => Build(("IHDR", Header(colorType: 3)), ("PLTE", [0, 0, 0]), ("IDAT", Deflate(Rows(0, bytesPerPixel: 1))), ("IEND", [])),
            "grayscale" => Build(("IHDR", Header(colorType: 0)), ("IDAT", Deflate(Rows(0, bytesPerPixel: 1))), ("IEND", [])),
            "rgb" => Build(("IHDR", Header(colorType: 2)), ("IDAT", Deflate(Rows(0, bytesPerPixel: 3))), ("IEND", [])),
            "16-bit" => Build(("IHDR", Header(bitDepth: 16)), ("IDAT", Deflate(Rows(0, bytesPerPixel: 8))), ("IEND", [])),
            "interlaced" => Build(("IHDR", Header(interlace: 1)), ("IDAT", Deflate(Rows(0))), ("IEND", [])),
            "plte" => Build(("IHDR", Header()), ("PLTE", [0, 0, 0]), ("IDAT", Deflate(Rows(0))), ("IEND", [])),
            "unknown-critical" => Build(("IHDR", Header()), ("ABCD", []), ("IDAT", Deflate(Rows(0))), ("IEND", [])),
            "second-ihdr" => Build(("IHDR", Header()), ("IHDR", Header()), ("IDAT", Deflate(Rows(0))), ("IEND", [])),
            "split-idat" => Build(("IHDR", Header()), ("IDAT", Deflate(Rows(0))[..10]), ("tEXt", "a\0b"u8.ToArray()), ("IDAT", Deflate(Rows(0))[10..]), ("IEND", [])),
            "iend-data" => Build(("IHDR", Header()), ("IDAT", Deflate(Rows(0))), ("IEND", [0])),
            "chunk-after-iend" => Build(("IHDR", Header()), ("IDAT", Deflate(Rows(0))), ("IEND", []), ("tEXt", "a\0b"u8.ToArray())),
            "long-data" => Build(("IHDR", Header()), ("IDAT", Deflate([.. Rows(0), 0])), ("IEND", [])),
            "zero-width" => Build(("IHDR", Header(width: 0)), ("IDAT", Deflate([])), ("IEND", [])),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

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

    private static byte[] Header(int width = 2, byte bitDepth = 8, byte colorType = 6, byte interlace = 0)
    {
        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), 2);
        header[8] = bitDepth;
        header[9] = colorType;
        header[12] = interlace;
        return header;
    }

    /// <summary>Two rows two pixels wide, each with filter type <paramref name="filter"/>.</summary>
    private static byte[] Rows(byte filter, int bytesPerPixel = 4)
    {
        var rows = new byte[(2 * bytesPerPixel + 1) * 2];
        rows[0] = filter;
        rows[2 * bytesPerPixel + 1] = filter;
        return rows;
    }

    private static byte[] SecondRowFilter(byte filter)
    {
        var rows = Rows(0);
        rows[9] = filter;
        return rows;
    }

    private static byte[] Deflate(byte[] bytes)
    {
        using var compressed = new MemoryStream();
        using (var deflate = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
            deflate.Write(bytes);
        return compressed.ToArray();
    }

    private static byte[] Build(params (string Type, byte[] Data)[] chunks)
    {
        using var png = new MemoryStream();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
        var word = new byte[4];
        foreach (var (type, data) in chunks)
        {
            var name = System.Text.Encoding.ASCII.GetBytes(type);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
            png.Write(word);
            png.Write(name);
            png.Write(data);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(word, FastPng.Crc(FastPng.Crc(0xFFFFFFFFu, name), data) ^ 0xFFFFFFFFu);
            png.Write(word);
        }
        return png.ToArray();
    }

    /// <summary>An 8x8 image's data under a header that claims <paramref name="height"/> rows.</summary>
    private static byte[] Replace(byte[] png, int width, int height)
    {
        var taller = FastPng.Encode(width, height, new byte[width * height * 4]);
        const int ihdrEnd = 8 + 12 + 13;
        return [.. taller[..ihdrEnd], .. png[ihdrEnd..]];
    }
}

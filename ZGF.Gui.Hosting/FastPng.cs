using System.Buffers.Binary;
using System.IO.Compression;

namespace ZGF.Gui.Hosting;

/// <summary>Encodes RGBA8 as a PNG for speed over size: every row Sub-filtered, deflated at the fastest level. For
/// captures a reader looks at once, where PngSharp's adaptive filtering at optimal compression costs ten times as long.</summary>
internal static class FastPng
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(int width, int height, ReadOnlySpan<byte> straightRgba)
    {
        if (straightRgba.Length != width * height * 4)
            throw new ArgumentException($"Expected {width * height * 4} bytes for {width}x{height}.", nameof(straightRgba));
        var stride = width * 4;
        var filtered = new byte[(stride + 1) * height];
        for (var y = 0; y < height; y++)
        {
            var row = straightRgba.Slice(y * stride, stride);
            var into = filtered.AsSpan(y * (stride + 1), stride + 1);
            into[0] = 1;
            for (var x = 0; x < stride; x++)
                into[x + 1] = (byte)(row[x] - (x >= 4 ? row[x - 4] : 0));
        }
        using var compressed = new MemoryStream();
        using (var deflate = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            deflate.Write(filtered);

        using var png = new MemoryStream();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;
        header[9] = 6;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(png, "IHDR"u8, header);
        WriteChunk(png, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        WriteChunk(png, "IEND"u8, []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream png, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
        png.Write(word);
        png.Write(type);
        png.Write(data);
        var crc = Crc(Crc(0xFFFFFFFFu, type), data) ^ 0xFFFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(word, crc);
        png.Write(word);
    }

    internal static uint Crc(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (var n = 0u; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}

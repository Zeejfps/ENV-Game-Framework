using System.Buffers.Binary;
using System.IO.Compression;

namespace ZGF.Gui.Hosting;

/// <summary>Checks that bytes are a whole PNG of the kind a capture is: 8 bits a channel, not interlaced, every chunk's
/// CRC right, IHDR first, at least one IDAT, IEND last with nothing after it, and IDAT inflating to exactly the
/// filtered rows the header promises.</summary>
internal static class PngStructure
{
    private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>The width and height of <paramref name="png"/>.</summary>
    /// <exception cref="FormatException">It is not such a PNG.</exception>
    public static (int Width, int Height) Validate(byte[] png)
    {
        if (png.Length < Signature.Length || !png.AsSpan(0, Signature.Length).SequenceEqual(Signature))
            throw new FormatException("The image is not a PNG: its signature is wrong.");
        var at = Signature.Length;
        var width = 0;
        var height = 0;
        var bytesPerPixel = 0;
        var seenHeader = false;
        var seenEnd = false;
        using var idat = new MemoryStream();
        while (at < png.Length)
        {
            if (seenEnd)
                throw new FormatException("The PNG has bytes after IEND.");
            if (png.Length - at < 12)
                throw new FormatException("The PNG ends inside a chunk header.");
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at, 4));
            if (length < 0 || length > png.Length - at - 12)
                throw new FormatException("A PNG chunk runs past the end of the image.");
            var type = png.AsSpan(at + 4, 4);
            var data = png.AsSpan(at + 8, length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8 + length, 4));
            if ((FastPng.Crc(FastPng.Crc(0xFFFFFFFFu, type), data) ^ 0xFFFFFFFFu) != crc)
                throw new FormatException($"The PNG's {System.Text.Encoding.ASCII.GetString(type)} chunk fails its CRC.");
            if (!seenHeader)
            {
                if (!type.SequenceEqual("IHDR"u8) || length != 13)
                    throw new FormatException("The PNG does not start with IHDR.");
                width = BinaryPrimitives.ReadInt32BigEndian(data);
                height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                bytesPerPixel = data[9] switch
                {
                    0 => 1,
                    2 => 3,
                    3 => 1,
                    4 => 2,
                    6 => 4,
                    _ => throw new FormatException($"The PNG's color type {data[9]} is not one PNG has."),
                };
                if (width < 1 || height < 1 || data[8] != 8 || data[10] != 0 || data[11] != 0 || data[12] != 0)
                    throw new FormatException("The PNG is not an 8-bit, non-interlaced image of some size.");
                seenHeader = true;
            }
            else if (type.SequenceEqual("IDAT"u8))
                idat.Write(data);
            else if (type.SequenceEqual("IEND"u8))
                seenEnd = true;
            at += 12 + length;
        }
        if (!seenEnd)
            throw new FormatException("The PNG has no IEND.");
        if (idat.Length == 0)
            throw new FormatException("The PNG has no IDAT.");
        var expected = ((long)width * bytesPerPixel + 1) * height;
        idat.Position = 0;
        long inflated;
        try
        {
            using var inflate = new ZLibStream(idat, CompressionMode.Decompress);
            inflated = Count(inflate, expected + 1);
        }
        catch (InvalidDataException e)
        {
            throw new FormatException("The PNG's image data does not inflate.", e);
        }
        if (inflated != expected)
            throw new FormatException($"The PNG's image data inflates to {inflated} bytes, not the {expected} its header promises.");
        return (width, height);
    }

    /// <summary>How many bytes <paramref name="stream"/> holds, counting no further than <paramref name="limit"/>.</summary>
    private static long Count(Stream stream, long limit)
    {
        var buffer = new byte[64 * 1024];
        long total = 0;
        int read;
        while (total < limit && (read = stream.Read(buffer, 0, buffer.Length)) > 0)
            total += read;
        return total;
    }
}

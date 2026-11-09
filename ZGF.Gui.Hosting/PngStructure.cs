using System.Buffers.Binary;
using System.IO.Compression;

namespace ZGF.Gui.Hosting;

/// <summary>Checks that bytes are a whole PNG of the kind a capture is: 8-bit RGBA, not interlaced, every chunk's CRC
/// right, exactly one IHDR and it first, one run of IDAT, no other critical chunk, IEND last with nothing after it, and
/// IDAT inflating to exactly the rows the header promises, each starting with a filter type PNG has.</summary>
internal static class PngStructure
{
    private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    private const int BytesPerPixel = 4;

    /// <summary>The width and height of <paramref name="png"/>.</summary>
    /// <exception cref="FormatException">It is not such a PNG.</exception>
    public static (int Width, int Height) Validate(byte[] png)
    {
        if (png.Length < Signature.Length || !png.AsSpan(0, Signature.Length).SequenceEqual(Signature))
            throw new FormatException("The image is not a PNG: its signature is wrong.");
        var at = Signature.Length;
        var width = 0;
        var height = 0;
        var seenHeader = false;
        var seenEnd = false;
        var idatRun = IdatRun.NotStarted;
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
            var name = System.Text.Encoding.ASCII.GetString(type);
            if ((FastPng.Crc(FastPng.Crc(0xFFFFFFFFu, type), data) ^ 0xFFFFFFFFu) != crc)
                throw new FormatException($"The PNG's {name} chunk fails its CRC.");
            if (!seenHeader)
            {
                if (!type.SequenceEqual("IHDR"u8) || length != 13)
                    throw new FormatException("The PNG does not start with IHDR.");
                width = BinaryPrimitives.ReadInt32BigEndian(data);
                height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                if (width < 1 || height < 1)
                    throw new FormatException($"The PNG is {width}x{height}, not an image of some size.");
                if (data[8] != 8 || data[9] != 6)
                    throw new FormatException($"The PNG has bit depth {data[8]} and color type {data[9]}, not 8-bit RGBA.");
                if (data[10] != 0 || data[11] != 0 || data[12] != 0)
                    throw new FormatException("The PNG's compression, filter or interlace method is not 0.");
                seenHeader = true;
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                if (idatRun == IdatRun.Ended)
                    throw new FormatException("The PNG's IDAT chunks are not consecutive.");
                idatRun = IdatRun.Open;
                idat.Write(data);
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                if (length != 0)
                    throw new FormatException("The PNG's IEND has data.");
                seenEnd = true;
            }
            else if (type.SequenceEqual("IHDR"u8))
                throw new FormatException("The PNG has a second IHDR.");
            else if (char.IsAsciiLetterUpper(name[0]))
                throw new FormatException($"The PNG has a critical {name} chunk an 8-bit RGBA image does not use.");
            if (idatRun == IdatRun.Open && !type.SequenceEqual("IDAT"u8))
                idatRun = IdatRun.Ended;
            at += 12 + length;
        }
        if (!seenEnd)
            throw new FormatException("The PNG has no IEND.");
        if (idat.Length == 0)
            throw new FormatException("The PNG has no IDAT.");
        idat.Position = 0;
        try
        {
            using var inflate = new ZLibStream(idat, CompressionMode.Decompress);
            CheckRows(inflate, (long)width * BytesPerPixel + 1, height);
        }
        catch (InvalidDataException e)
        {
            throw new FormatException("The PNG's image data does not inflate.", e);
        }
        return (width, height);
    }

    /// <summary>Checks that <paramref name="rows"/> holds exactly <paramref name="height"/> rows of
    /// <paramref name="rowLength"/> bytes, each starting with filter type 0 to 4.</summary>
    private static void CheckRows(Stream rows, long rowLength, int height)
    {
        var expected = rowLength * height;
        var buffer = new byte[64 * 1024];
        long total = 0;
        long nextFilter = 0;
        int read;
        while (total <= expected && (read = rows.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (; nextFilter < Math.Min(total + read, expected); nextFilter += rowLength)
            {
                var filter = buffer[nextFilter - total];
                if (filter > 4)
                    throw new FormatException($"Row {nextFilter / rowLength} of the PNG has filter type {filter}, which PNG does not have.");
            }
            total += read;
        }
        if (total > expected)
            throw new FormatException($"The PNG's image data inflates to more than the {expected} bytes its header promises.");
        if (total < expected)
            throw new FormatException($"The PNG's image data inflates to {total} bytes, not the {expected} its header promises.");
    }

    private enum IdatRun
    {
        NotStarted,
        Open,
        Ended,
    }
}

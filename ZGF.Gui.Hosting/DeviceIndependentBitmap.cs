using System.Buffers.Binary;
using System.Numerics;

namespace ZGF.Gui.Hosting;

/// <summary>The packed device-independent bitmap Windows keeps a picture on the clipboard as (CF_DIB,
/// CF_DIBV5): a BITMAPINFOHEADER or one of its longer successors, the color masks where they follow
/// it, any color table, then the rows, bottom-up unless the height is negative, each padded to four
/// bytes. Read in its uncompressed 24- and 32-bit forms, which screenshots and copied pictures are.</summary>
internal static class DeviceIndependentBitmap
{
    private const int InfoHeaderSize = 40;
    private const int MasksAt = 40;
    private const int AlphaMaskEnd = 56;
    private const uint Uncompressed = 0;
    private const uint Bitfields = 3;
    private const uint DefaultRed = 0x00FF0000;
    private const uint DefaultGreen = 0x0000FF00;
    private const uint DefaultBlue = 0x000000FF;
    private const uint DefaultAlpha = 0xFF000000;

    /// <summary>The picture <paramref name="dib"/> holds, or null where it is cut short or in a form
    /// this does not read.</summary>
    public static ClipboardImage? Decode(ReadOnlySpan<byte> dib)
    {
        if (dib.Length < InfoHeaderSize)
            return null;
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(dib);
        if (headerSize < InfoHeaderSize || headerSize > dib.Length)
            return null;
        var width = BinaryPrimitives.ReadInt32LittleEndian(dib[4..]);
        var signedHeight = BinaryPrimitives.ReadInt32LittleEndian(dib[8..]);
        var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
        var compression = BinaryPrimitives.ReadUInt32LittleEndian(dib[16..]);
        var colorsUsed = BinaryPrimitives.ReadUInt32LittleEndian(dib[32..]);
        if (width <= 0 || signedHeight == 0 || signedHeight == int.MinValue)
            return null;
        if (bitCount is not (24 or 32))
            return null;
        if (compression != Uncompressed && !(compression == Bitfields && bitCount == 32))
            return null;
        var height = Math.Abs(signedHeight);
        if ((long)width * height * 4 > Array.MaxLength)
            return null;

        var masks = new Masks(DefaultRed, DefaultGreen, DefaultBlue, bitCount == 32 ? DefaultAlpha : 0);
        long pixelsAt = headerSize;
        if (compression == Bitfields)
        {
            // A BITMAPINFOHEADER is followed by the three masks; the longer headers hold them.
            if (headerSize == InfoHeaderSize)
                pixelsAt += 12;
            else if (headerSize < MasksAt + 12)
                return null;
            if (dib.Length < MasksAt + 12)
                return null;
            masks = new Masks(
                BinaryPrimitives.ReadUInt32LittleEndian(dib[MasksAt..]),
                BinaryPrimitives.ReadUInt32LittleEndian(dib[(MasksAt + 4)..]),
                BinaryPrimitives.ReadUInt32LittleEndian(dib[(MasksAt + 8)..]),
                headerSize >= AlphaMaskEnd ? BinaryPrimitives.ReadUInt32LittleEndian(dib[(MasksAt + 12)..]) : 0);
        }
        pixelsAt += colorsUsed * 4L;
        var stride = ((long)width * bitCount + 31) / 32 * 4;
        if (pixelsAt + stride * height > dib.Length)
            return null;

        var rows = dib[(int)pixelsAt..];
        var rgba = new byte[width * height * 4];
        var anyAlpha = false;
        for (var y = 0; y < height; y++)
        {
            var from = signedHeight < 0 ? y : height - 1 - y;
            var row = rows.Slice((int)(from * stride), (int)stride);
            var to = rgba.AsSpan(y * width * 4, width * 4);
            for (var x = 0; x < width; x++)
            {
                var pixel = to.Slice(x * 4, 4);
                if (bitCount == 24)
                {
                    pixel[0] = row[x * 3 + 2];
                    pixel[1] = row[x * 3 + 1];
                    pixel[2] = row[x * 3];
                    pixel[3] = byte.MaxValue;
                    continue;
                }
                var value = BinaryPrimitives.ReadUInt32LittleEndian(row[(x * 4)..]);
                pixel[0] = Channel(value, masks.Red);
                pixel[1] = Channel(value, masks.Green);
                pixel[2] = Channel(value, masks.Blue);
                pixel[3] = masks.Alpha == 0 ? byte.MaxValue : Channel(value, masks.Alpha);
                anyAlpha |= pixel[3] != 0;
            }
        }
        // Most 32-bit bitmaps leave the fourth byte unused, all zeros, rather than meaning every pixel
        // to be clear.
        if (bitCount == 32 && masks.Alpha != 0 && !anyAlpha)
            for (var i = 3; i < rgba.Length; i += 4)
                rgba[i] = byte.MaxValue;
        return new ClipboardImage(width, height, rgba);
    }

    private readonly record struct Masks(uint Red, uint Green, uint Blue, uint Alpha);

    /// <summary>The channel <paramref name="mask"/> picks out of <paramref name="pixel"/>, scaled to
    /// eight bits.</summary>
    private static byte Channel(uint pixel, uint mask)
    {
        if (mask == 0)
            return 0;
        var bits = BitOperations.PopCount(mask);
        var value = (ulong)((pixel & mask) >> BitOperations.TrailingZeroCount(mask));
        if (bits == 8)
            return (byte)value;
        return (byte)(value * 255 / ((1UL << bits) - 1));
    }
}

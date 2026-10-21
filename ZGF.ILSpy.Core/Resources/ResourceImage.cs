using System.Buffers.Binary;
using JpegSharp.Api;
using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>A resource decoded to pixels: straight-alpha RGBA8, rows top-down, and the format
/// it was stored in.</summary>
public sealed class ResourceImage
{
    internal ResourceImage(int width, int height, byte[] rgba, string format)
    {
        Width = width;
        Height = height;
        Rgba = rgba;
        Format = format;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Rgba { get; }

    /// <summary>What it was stored as: PNG, JPEG, BMP, GIF, ICO or CUR.</summary>
    public string Format { get; }

    /// <summary>The formats ILSpy shows as pictures, by the file extension of the resource name.</summary>
    internal static bool IsImageName(string name) =>
        Path.GetExtension(name).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico" or ".cur";

    /// <summary>The picture in <paramref name="data"/>, recognized by its content rather than its
    /// name. Throws <see cref="InvalidDataException"/> when it is no picture this can decode.</summary>
    internal static ResourceImage Decode(byte[] data)
    {
        try
        {
            ReadOnlySpan<byte> span = data;
            if (span.StartsWith((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G']))
                return Png(data);
            if (span.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8]))
                return Jpeg(data);
            if (span.StartsWith("BM"u8))
                return Bitmap(span, "BMP");
            if (span.StartsWith("GIF8"u8))
                return Gif.Decode(span);
            if (span.Length >= 6 && span[0] == 0 && span[1] == 0 && span[2] is 1 or 2)
                return Icon(span);
        }
        catch (Exception e) when (e is not InvalidDataException and not OutOfMemoryException)
        {
            throw new InvalidDataException($"The picture cannot be read: {e.Message}", e);
        }
        throw new InvalidDataException("This is no PNG, JPEG, BMP, GIF, ICO or CUR picture.");
    }

    private static ResourceImage Png(byte[] data)
    {
        var png = PngSharp.Api.Png.DecodeFromByteArray(data);
        var ihdr = png.Ihdr;
        int width = (int)ihdr.Width, height = (int)ihdr.Height;
        var depth = ihdr.BitDepth;
        var src = png.PixelData;
        var bytesPerPixel = ihdr.GetBytesPerPixel();
        var wide = depth == 16;
        var palette = png.Plte?.Entries;
        var transparency = png.Trns?.Data;
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            var at = i * bytesPerPixel;
            byte r, g, b, a = 255;
            switch (ihdr.ColorType)
            {
                case ColorType.Grayscale:
                    r = g = b = Sample(src, at, depth);
                    if (transparency is { Length: >= 2 } && Raw(src, at, wide) == BinaryPrimitives.ReadUInt16BigEndian(transparency))
                        a = 0;
                    break;
                case ColorType.GrayscaleWithAlpha:
                    r = g = b = Sample(src, at, depth);
                    a = Sample(src, at + (wide ? 2 : 1), depth);
                    break;
                case ColorType.TrueColor:
                    r = Sample(src, at, depth);
                    g = Sample(src, at + (wide ? 2 : 1), depth);
                    b = Sample(src, at + (wide ? 4 : 2), depth);
                    if (transparency is { Length: >= 6 }
                        && Raw(src, at, wide) == BinaryPrimitives.ReadUInt16BigEndian(transparency)
                        && Raw(src, at + (wide ? 2 : 1), wide) == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2))
                        && Raw(src, at + (wide ? 4 : 2), wide) == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4)))
                        a = 0;
                    break;
                case ColorType.TrueColorWithAlpha:
                    r = Sample(src, at, depth);
                    g = Sample(src, at + (wide ? 2 : 1), depth);
                    b = Sample(src, at + (wide ? 4 : 2), depth);
                    a = Sample(src, at + (wide ? 6 : 3), depth);
                    break;
                case ColorType.IndexedColor:
                    var index = src[at];
                    if (palette == null || index * 3 + 2 >= palette.Length) throw new InvalidDataException("A palette index is out of range.");
                    (r, g, b) = (palette[index * 3], palette[index * 3 + 1], palette[index * 3 + 2]);
                    if (transparency != null && index < transparency.Length)
                        a = transparency[index];
                    break;
                default:
                    throw new InvalidDataException($"Unknown PNG color type {ihdr.ColorType}.");
            }
            rgba[i * 4] = r;
            rgba[i * 4 + 1] = g;
            rgba[i * 4 + 2] = b;
            rgba[i * 4 + 3] = a;
        }
        return new ResourceImage(width, height, rgba, "PNG");

        static int Raw(byte[] src, int at, bool wide) => wide ? src[at] << 8 | src[at + 1] : src[at];

        static byte Sample(byte[] src, int at, int depth) =>
            depth switch
            {
                16 => src[at],
                8 => src[at],
                _ => (byte)(src[at] * 255 / ((1 << depth) - 1)),
            };
    }

    private static ResourceImage Jpeg(byte[] data)
    {
        var jpeg = JpegSharp.Api.Jpeg.DecodeAnyPrecision(data);
        var pixels = jpeg.ToRgba8888();
        var rgba = new byte[pixels.Length * 4];
        for (var i = 0; i < pixels.Length; i++)
            BinaryPrimitives.WriteInt32BigEndian(rgba.AsSpan(i * 4), pixels[i]);
        return new ResourceImage(jpeg.Width, jpeg.Height, rgba, "JPEG");
    }

    private static ResourceImage Icon(ReadOnlySpan<byte> data)
    {
        var format = data[2] == 2 ? "CUR" : "ICO";
        var count = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        var best = -1;
        var bestArea = -1;
        for (var i = 0; i < count; i++)
        {
            var entry = data.Slice(6 + i * 16, 16);
            var area = (entry[0] == 0 ? 256 : entry[0]) * (entry[1] == 0 ? 256 : entry[1]);
            if (area > bestArea)
                (best, bestArea) = (i, area);
        }
        if (best < 0) throw new InvalidDataException($"The {format} holds no pictures.");
        var chosen = data.Slice(6 + best * 16, 16);
        var size = BinaryPrimitives.ReadInt32LittleEndian(chosen[8..]);
        var offset = BinaryPrimitives.ReadInt32LittleEndian(chosen[12..]);
        var payload = data.Slice(offset, size);
        if (payload.StartsWith((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G']))
        {
            var png = Png(payload.ToArray());
            return new ResourceImage(png.Width, png.Height, png.Rgba, format);
        }
        return Dib(payload, format, isIcon: true);
    }

    private static ResourceImage Bitmap(ReadOnlySpan<byte> data, string format)
    {
        var pixels = BinaryPrimitives.ReadInt32LittleEndian(data[10..]);
        return Dib(data[14..], format, isIcon: false, pixelOffset: pixels - 14);
    }

    /// <summary>A device-independent bitmap: a BMP after its file header, or a picture of an icon,
    /// whose height counts its AND mask too.</summary>
    private static ResourceImage Dib(ReadOnlySpan<byte> dib, string format, bool isIcon, int pixelOffset = -1)
    {
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(dib);
        int width, height, bits, compression = 0, colors = 0;
        if (headerSize == 12)
        {
            width = BinaryPrimitives.ReadInt16LittleEndian(dib[4..]);
            height = BinaryPrimitives.ReadInt16LittleEndian(dib[6..]);
            bits = BinaryPrimitives.ReadUInt16LittleEndian(dib[10..]);
        }
        else
        {
            width = BinaryPrimitives.ReadInt32LittleEndian(dib[4..]);
            height = BinaryPrimitives.ReadInt32LittleEndian(dib[8..]);
            bits = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
            compression = BinaryPrimitives.ReadInt32LittleEndian(dib[16..]);
            colors = BinaryPrimitives.ReadInt32LittleEndian(dib[32..]);
        }
        var topDown = height < 0;
        height = Math.Abs(height);
        if (isIcon)
            height /= 2;
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384) throw new InvalidDataException($"A {width}×{height} picture is out of range.");

        uint redMask = 0x00FF0000, greenMask = 0x0000FF00, blueMask = 0x000000FF, alphaMask = bits == 32 && !isIcon ? 0u : 0xFF000000;
        var tableAt = headerSize;
        if (compression == 3 || compression == 6)
        {
            var masksAt = headerSize >= 52 ? 40 : headerSize;
            redMask = BinaryPrimitives.ReadUInt32LittleEndian(dib[masksAt..]);
            greenMask = BinaryPrimitives.ReadUInt32LittleEndian(dib[(masksAt + 4)..]);
            blueMask = BinaryPrimitives.ReadUInt32LittleEndian(dib[(masksAt + 8)..]);
            alphaMask = headerSize >= 56 || compression == 6 ? BinaryPrimitives.ReadUInt32LittleEndian(dib[(masksAt + 12)..]) : 0;
            if (headerSize == 40)
                tableAt += compression == 6 ? 16 : 12;
        }
        else if (compression != 0)
        {
            throw new InvalidDataException("Compressed BMPs are not supported.");
        }

        var entrySize = headerSize == 12 ? 3 : 4;
        var tableLength = bits <= 8 ? (colors > 0 ? colors : 1 << bits) : 0;
        var start = pixelOffset >= 0 ? pixelOffset : tableAt + tableLength * entrySize;
        var stride = (width * bits + 31) / 32 * 4;
        var rgba = new byte[width * height * 4];
        var anyAlpha = false;
        for (var y = 0; y < height; y++)
        {
            var row = dib.Slice(start + (topDown ? y : height - 1 - y) * stride, stride);
            for (var x = 0; x < width; x++)
            {
                byte r, g, b, a = 255;
                switch (bits)
                {
                    case 1 or 4 or 8:
                        var index = bits switch
                        {
                            1 => row[x >> 3] >> (7 - (x & 7)) & 1,
                            4 => row[x >> 1] >> ((x & 1) == 0 ? 4 : 0) & 0xF,
                            _ => row[x],
                        };
                        var entry = dib.Slice(tableAt + Math.Min(index, tableLength - 1) * entrySize, 3);
                        (b, g, r) = (entry[0], entry[1], entry[2]);
                        break;
                    case 24:
                        (b, g, r) = (row[x * 3], row[x * 3 + 1], row[x * 3 + 2]);
                        break;
                    case 16 or 32:
                        var value = bits == 16 ? BinaryPrimitives.ReadUInt16LittleEndian(row[(x * 2)..]) : BinaryPrimitives.ReadUInt32LittleEndian(row[(x * 4)..]);
                        if (bits == 16 && compression == 0)
                            (redMask, greenMask, blueMask) = (0x7C00, 0x03E0, 0x001F);
                        r = Channel(value, redMask);
                        g = Channel(value, greenMask);
                        b = Channel(value, blueMask);
                        if (alphaMask != 0)
                        {
                            a = Channel(value, alphaMask);
                            anyAlpha |= a != 0;
                        }
                        break;
                    default:
                        throw new InvalidDataException($"{bits}-bit BMPs are not supported.");
                }
                var at = (y * width + x) * 4;
                rgba[at] = r;
                rgba[at + 1] = g;
                rgba[at + 2] = b;
                rgba[at + 3] = a;
            }
        }
        if (alphaMask != 0 && !anyAlpha)
        {
            for (var i = 3; i < rgba.Length; i += 4)
                rgba[i] = 255;
        }
        if (isIcon && (bits != 32 || !anyAlpha))
        {
            var maskStride = (width + 31) / 32 * 4;
            var maskStart = start + stride * height;
            if (maskStart + maskStride * height <= dib.Length)
            {
                for (var y = 0; y < height; y++)
                {
                    var row = dib.Slice(maskStart + (height - 1 - y) * maskStride, maskStride);
                    for (var x = 0; x < width; x++)
                    {
                        if ((row[x >> 3] >> (7 - (x & 7)) & 1) != 0)
                            rgba[(y * width + x) * 4 + 3] = 0;
                    }
                }
            }
        }
        return new ResourceImage(width, height, rgba, format);

        static byte Channel(uint value, uint mask)
        {
            if (mask == 0) return 0;
            var shift = System.Numerics.BitOperations.TrailingZeroCount(mask);
            var max = mask >> shift;
            return (byte)(((value & mask) >> shift) * 255 / max);
        }
    }

    /// <summary>The first frame of a GIF, as ILSpy shows it.</summary>
    private static class Gif
    {
        public static ResourceImage Decode(ReadOnlySpan<byte> data)
        {
            var width = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
            var height = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
            var flags = data[10];
            var at = 13;
            ReadOnlySpan<byte> global = default;
            if ((flags & 0x80) != 0)
            {
                var size = 3 << ((flags & 7) + 1);
                global = data.Slice(at, size);
                at += size;
            }
            var transparent = -1;
            var rgba = new byte[width * height * 4];
            while (at < data.Length)
            {
                switch (data[at])
                {
                    case 0x21:
                        if (data[at + 1] == 0xF9 && (data[at + 3] & 1) != 0)
                            transparent = data[at + 6];
                        at += 2;
                        at = SkipBlocks(data, at);
                        break;
                    case 0x2C:
                        var left = BinaryPrimitives.ReadUInt16LittleEndian(data[(at + 1)..]);
                        var top = BinaryPrimitives.ReadUInt16LittleEndian(data[(at + 3)..]);
                        var frameWidth = BinaryPrimitives.ReadUInt16LittleEndian(data[(at + 5)..]);
                        var frameHeight = BinaryPrimitives.ReadUInt16LittleEndian(data[(at + 7)..]);
                        var frameFlags = data[at + 9];
                        at += 10;
                        var palette = global;
                        if ((frameFlags & 0x80) != 0)
                        {
                            var size = 3 << ((frameFlags & 7) + 1);
                            palette = data.Slice(at, size);
                            at += size;
                        }
                        var minimumCodeSize = data[at++];
                        var compressed = new List<byte>();
                        while (data[at] != 0)
                        {
                            compressed.AddRange(data.Slice(at + 1, data[at]));
                            at += data[at] + 1;
                        }
                        var indices = Lzw(compressed, minimumCodeSize, frameWidth * frameHeight);
                        var interlaced = (frameFlags & 0x40) != 0;
                        for (var i = 0; i < indices.Length; i++)
                        {
                            var row = interlaced ? InterlacedRow(i / frameWidth, frameHeight) : i / frameWidth;
                            int x = left + i % frameWidth, y = top + row;
                            if (x >= width || y >= height) continue;
                            var index = indices[i];
                            var pixel = (y * width + x) * 4;
                            if (index == transparent || index * 3 + 2 >= palette.Length) continue;
                            rgba[pixel] = palette[index * 3];
                            rgba[pixel + 1] = palette[index * 3 + 1];
                            rgba[pixel + 2] = palette[index * 3 + 2];
                            rgba[pixel + 3] = 255;
                        }
                        return new ResourceImage(width, height, rgba, "GIF");
                    default:
                        throw new InvalidDataException("The GIF has no picture.");
                }
            }
            throw new InvalidDataException("The GIF has no picture.");
        }

        private static int SkipBlocks(ReadOnlySpan<byte> data, int at)
        {
            while (data[at] != 0)
                at += data[at] + 1;
            return at + 1;
        }

        private static int InterlacedRow(int pass, int height)
        {
            ReadOnlySpan<(int Start, int Step)> passes = [(0, 8), (4, 8), (2, 4), (1, 2)];
            foreach (var (start, step) in passes)
            {
                var rows = (height - start + step - 1) / step;
                if (pass < rows) return start + pass * step;
                pass -= rows;
            }
            return height;
        }

        private static byte[] Lzw(List<byte> data, int minimumCodeSize, int pixels)
        {
            var output = new byte[pixels];
            var written = 0;
            var clear = 1 << minimumCodeSize;
            var end = clear + 1;
            var prefix = new short[4096];
            var suffix = new byte[4096];
            var length = new short[4096];
            for (var i = 0; i < clear; i++)
            {
                prefix[i] = -1;
                suffix[i] = (byte)i;
                length[i] = 1;
            }
            var next = end + 1;
            var codeSize = minimumCodeSize + 1;
            var previous = -1;
            int bits = 0, buffer = 0, position = 0;
            Span<byte> stack = stackalloc byte[4096];
            while (written < pixels)
            {
                while (bits < codeSize)
                {
                    if (position >= data.Count) return output;
                    buffer |= data[position++] << bits;
                    bits += 8;
                }
                var code = buffer & ((1 << codeSize) - 1);
                buffer >>= codeSize;
                bits -= codeSize;
                if (code == clear)
                {
                    next = end + 1;
                    codeSize = minimumCodeSize + 1;
                    previous = -1;
                    continue;
                }
                if (code == end) break;
                int entry;
                if (previous < 0)
                {
                    entry = code;
                }
                else
                {
                    var first = code < next ? FirstOf(code) : FirstOf(previous);
                    if (next < 4096)
                    {
                        prefix[next] = (short)previous;
                        suffix[next] = first;
                        length[next] = (short)(length[previous] + 1);
                        next++;
                        if (next == 1 << codeSize && codeSize < 12)
                            codeSize++;
                    }
                    entry = code;
                }
                if (entry >= next) return output;
                var count = length[entry];
                for (int c = entry, i = count - 1; c >= 0 && i >= 0; c = prefix[c], i--)
                    stack[i] = suffix[c];
                for (var i = 0; i < count && written < pixels; i++)
                    output[written++] = stack[i];
                previous = code;
            }
            return output;

            byte FirstOf(int code)
            {
                while (prefix[code] >= 0)
                    code = prefix[code];
                return suffix[code];
            }
        }
    }
}

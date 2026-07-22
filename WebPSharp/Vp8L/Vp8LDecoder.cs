using WebPSharp.Api;
using WebPSharp.Api.Exceptions;
using WebPSharp.Vp8L.Transforms;

namespace WebPSharp.Vp8L;

/// <summary>
/// Decodes a VP8L (lossless) bitstream to an RGBA image. Supports the full structural surface: all
/// four transforms (predictor, cross-color, subtract-green, color-indexing with pixel bundling), an
/// optional color cache, LZ77 back-references, and meta-Huffman (multiple Huffman groups selected
/// per tile). Back-reference distances that use the near-distance plane codes (≤ 120) are rejected
/// pending reference-table validation; distances emitted by this library never use them.
/// </summary>
internal static class Vp8LDecoder
{
    private const int Signature = 0x2F;
    private const int LengthCodes = 24;
    private const int LiteralGreenSymbols = 256;
    private const int DistanceSymbols = 40;

    /// <summary>Decodes a VP8L chunk payload to an RGBA image.</summary>
    /// <param name="payload">The <c>VP8L</c> chunk payload, including its 1-byte signature.</param>
    /// <returns>The decoded image (always RGBA).</returns>
    /// <exception cref="WebPFormatException">The header is malformed.</exception>
    /// <exception cref="WebPCorruptException">The compressed data is corrupt or truncated.</exception>
    /// <exception cref="WebPException">A not-yet-supported VP8L feature is present.</exception>
    public static WebPImage Decode(ReadOnlySpan<byte> payload)
    {
        var reader = new Vp8LBitReader(payload);

        if (reader.ReadBits(8) != Signature)
            throw new WebPFormatException("VP8L bitstream is missing its 0x2F signature.");

        var width = (int)reader.ReadBits(14) + 1;
        var height = (int)reader.ReadBits(14) + 1;
        _ = reader.ReadBit(); // alpha_is_used hint; the alpha channel is decoded regardless
        var version = (int)reader.ReadBits(3);
        if (version != 0)
            throw new WebPFormatException($"Unsupported VP8L version {version}.");

        var transforms = ReadTransforms(ref reader, width, height, out var decodeWidth);

        var argb = DecodeImageData(ref reader, decodeWidth, height, allowMeta: true);

        // Inverse transforms are applied in the reverse of their bitstream order. The
        // color-indexing transform expands the working width from bundled back to full.
        var curWidth = decodeWidth;
        for (var i = transforms.Count - 1; i >= 0; i--)
            (argb, curWidth) = ApplyInverseTransform(transforms[i], argb, curWidth, height);

        // width*height <= 16384*16384, so the RGBA byte count fits comfortably in an int.
        var rgba = new byte[argb.Length * 4];
        for (var i = 0; i < argb.Length; i++)
        {
            var p = argb[i];
            var j = i * 4;
            rgba[j] = (byte)(p >> 16);
            rgba[j + 1] = (byte)(p >> 8);
            rgba[j + 2] = (byte)p;
            rgba[j + 3] = (byte)(p >> 24);
        }

        return new WebPImage(width, height, WebPColorFormat.Rgba, rgba);
    }

    private readonly record struct TransformInfo(Vp8LTransformType Type, uint[]? Data, int Bits, int OutputWidth);

    private static List<TransformInfo> ReadTransforms(ref Vp8LBitReader reader, int width, int height, out int decodeWidth)
    {
        var transforms = new List<TransformInfo>(4);
        var seen = 0;
        var curWidth = width;
        while (reader.ReadBit() != 0)
        {
            var type = (Vp8LTransformType)reader.ReadBits(2);
            var mask = 1 << (int)type;
            if ((seen & mask) != 0)
                throw new WebPCorruptException($"VP8L transform {type} appears more than once.");
            seen |= mask;

            switch (type)
            {
                case Vp8LTransformType.SubtractGreen:
                    transforms.Add(new TransformInfo(type, null, 0, curWidth));
                    break;
                case Vp8LTransformType.Predictor:
                case Vp8LTransformType.CrossColor:
                {
                    var bits = (int)reader.ReadBits(3) + 2;
                    var subW = Vp8LSubSample.Size(curWidth, bits);
                    var subH = Vp8LSubSample.Size(height, bits);
                    var data = DecodeImageData(ref reader, subW, subH, allowMeta: false);
                    transforms.Add(new TransformInfo(type, data, bits, curWidth));
                    break;
                }
                case Vp8LTransformType.ColorIndexing:
                {
                    var numColors = (int)reader.ReadBits(8) + 1;
                    var bits = ColorIndexingTransform.BitsForColorCount(numColors);
                    var rawPalette = DecodeImageData(ref reader, numColors, 1, allowMeta: false);
                    var palette = ColorIndexingTransform.ExpandPalette(rawPalette, numColors, bits);
                    // The transform expands from the bundled width back to the current full width.
                    transforms.Add(new TransformInfo(type, palette, bits, curWidth));
                    curWidth = Vp8LSubSample.Size(curWidth, bits);
                    break;
                }
                default:
                    throw new WebPCorruptException($"Unknown VP8L transform type {(int)type}.");
            }
        }

        decodeWidth = curWidth;
        return transforms;
    }

    private static (uint[] Pixels, int Width) ApplyInverseTransform(TransformInfo transform, uint[] argb, int width, int height)
    {
        switch (transform.Type)
        {
            case Vp8LTransformType.SubtractGreen:
                SubtractGreenTransform.Inverse(argb);
                return (argb, width);
            case Vp8LTransformType.Predictor:
                PredictorTransform.Inverse(argb, width, height, transform.Data!, transform.Bits);
                return (argb, width);
            case Vp8LTransformType.CrossColor:
                CrossColorTransform.Inverse(argb, width, height, transform.Data!, transform.Bits);
                return (argb, width);
            case Vp8LTransformType.ColorIndexing:
            {
                var expanded = ColorIndexingTransform.Inverse(argb, width, transform.OutputWidth, height, transform.Data!, transform.Bits);
                return (expanded, transform.OutputWidth);
            }
            default:
                throw new WebPException($"VP8L {transform.Type} transform is not yet supported.");
        }
    }

    private readonly record struct HuffmanGroup(
        HuffmanTree Green, HuffmanTree Red, HuffmanTree Blue, HuffmanTree Alpha, HuffmanTree Distance);

    private static uint[] DecodeImageData(ref Vp8LBitReader reader, int width, int height, bool allowMeta)
    {
        var total = width * height;
        var argb = new uint[total];

        ColorCache? cache = null;
        var cacheSize = 0;
        if (reader.ReadBit() != 0)
        {
            var cacheBits = (int)reader.ReadBits(4);
            if (cacheBits is < 1 or > 11)
                throw new WebPCorruptException($"Invalid VP8L color cache bits {cacheBits}.");
            cache = new ColorCache(cacheBits);
            cacheSize = cache.Size;
        }

        // Meta-Huffman: an entropy image selects a Huffman group per tile.
        uint[]? huffmanImage = null;
        var huffmanBits = 0;
        var huffmanWidth = 0;
        var numGroups = 1;
        if (reader.ReadBit() != 0)
        {
            if (!allowMeta)
                throw new WebPCorruptException("VP8L meta-Huffman is not permitted for this image stream.");
            huffmanBits = (int)reader.ReadBits(3) + 2;
            huffmanWidth = Vp8LSubSample.Size(width, huffmanBits);
            var huffmanHeight = Vp8LSubSample.Size(height, huffmanBits);
            huffmanImage = DecodeImageData(ref reader, huffmanWidth, huffmanHeight, allowMeta: false);
            numGroups = ComputeGroupCount(huffmanImage);
        }

        var greenAlphabet = LiteralGreenSymbols + LengthCodes + cacheSize;
        var groups = new HuffmanGroup[numGroups];
        for (var g = 0; g < numGroups; g++)
        {
            groups[g] = new HuffmanGroup(
                Vp8LHuffman.ReadPrefixCode(ref reader, greenAlphabet),
                Vp8LHuffman.ReadPrefixCode(ref reader, 256),
                Vp8LHuffman.ReadPrefixCode(ref reader, 256),
                Vp8LHuffman.ReadPrefixCode(ref reader, 256),
                Vp8LHuffman.ReadPrefixCode(ref reader, DistanceSymbols));
        }

        var pos = 0;
        while (pos < total)
        {
            var x = pos % width;
            var y = pos / width;
            var groupIndex = huffmanImage is null
                ? 0
                : (int)((huffmanImage[(y >> huffmanBits) * huffmanWidth + (x >> huffmanBits)] >> 8) & 0xFFFF);
            if ((uint)groupIndex >= (uint)numGroups)
                throw new WebPCorruptException($"VP8L meta-Huffman group index {groupIndex} is out of range.");
            ref readonly var group = ref groups[groupIndex];

            var s = group.Green.ReadSymbol(ref reader);
            if (s < LiteralGreenSymbols)
            {
                var r = group.Red.ReadSymbol(ref reader);
                var b = group.Blue.ReadSymbol(ref reader);
                var a = group.Alpha.ReadSymbol(ref reader);
                var pixel = ((uint)a << 24) | ((uint)r << 16) | ((uint)s << 8) | (uint)b;
                argb[pos++] = pixel;
                cache?.Insert(pixel);
            }
            else if (s < LiteralGreenSymbols + LengthCodes)
            {
                var length = LzPrefix.Decode(s - LiteralGreenSymbols, ref reader);
                var distSymbol = group.Distance.ReadSymbol(ref reader);
                var planeCode = LzPrefix.Decode(distSymbol, ref reader);
                var dist = Vp8LDistance.PlaneCodeToDistance(width, planeCode);

                if (dist < 1 || dist > pos)
                    throw new WebPCorruptException($"VP8L back-reference distance {dist} is out of range at pixel {pos}.");
                if (pos + length > total)
                    throw new WebPCorruptException("VP8L back-reference overruns the image.");

                var srcPos = pos - dist;
                for (var i = 0; i < length; i++)
                {
                    var px = argb[srcPos + i];
                    argb[pos++] = px;
                    cache?.Insert(px);
                }
            }
            else
            {
                var index = s - LiteralGreenSymbols - LengthCodes;
                if (cache is null || index >= cacheSize)
                    throw new WebPCorruptException($"VP8L color-cache index {index} is invalid.");
                var px = cache.Lookup(index);
                argb[pos++] = px;
                cache.Insert(px);
            }

            if (reader.IsEndOfStream && pos < total)
                throw new WebPCorruptException("VP8L stream ended before all pixels were decoded.");
        }

        return argb;
    }

    private static int ComputeGroupCount(uint[] huffmanImage)
    {
        var max = 0;
        foreach (var pixel in huffmanImage)
        {
            var index = (int)((pixel >> 8) & 0xFFFF);
            if (index > max)
                max = index;
        }
        return max + 1;
    }
}

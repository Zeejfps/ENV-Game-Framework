using WebPSharp.Api;
using WebPSharp.Vp8L.Transforms;

namespace WebPSharp.Vp8L;

/// <summary>
/// Encodes an RGBA/RGB image to a VP8L (lossless) bitstream. Supports all four transforms
/// (predictor, cross-color, subtract-green, color-indexing) and optional LZ77 back-references,
/// with a single frequency-optimized Huffman group. Distances are emitted as plane codes above the
/// near-distance range, so no near-distance table is required and the output decodes on any
/// compliant decoder. The output is fully specification-compliant and deterministic. An optional
/// color cache and meta-Huffman groups further improve the compression ratio without affecting
/// correctness.
/// </summary>
internal static class Vp8LEncoder
{
    private const int Signature = 0x2F;
    private const int LengthCodes = 24;
    private const int LiteralGreenSymbols = 256;
    private const int DistanceSymbols = 40;
    private const int MaxCodeLength = 15;

    // Distances are emitted directly as plane code (distance + 120), which every decoder maps back
    // to the distance unchanged, avoiding the near-distance lookup table.
    private const int PlaneCodeBias = 120;

    /// <summary>Encodes an image to a VP8L chunk payload with default settings.</summary>
    /// <param name="image">The source image (RGB or RGBA).</param>
    /// <returns>The <c>VP8L</c> chunk payload, including its 1-byte signature.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    public static byte[] Encode(WebPImage image) => Encode(image, new Vp8LEncodeSettings());

    /// <summary>
    /// Encodes an image trying several transform configurations (scaled by <paramref name="effort"/>)
    /// and returns the smallest result. Deterministic: candidates are evaluated in a fixed order and
    /// ties keep the earlier candidate.
    /// </summary>
    /// <param name="image">The source image (RGB or RGBA).</param>
    /// <param name="effort">Compression effort, 0 (fastest) to 9 (smallest).</param>
    /// <returns>The smallest <c>VP8L</c> chunk payload found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    public static byte[] EncodeBest(WebPImage image, int effort)
    {
        ArgumentNullException.ThrowIfNull(image);

        byte[]? best = null;
        foreach (var settings in Candidates(effort))
        {
            byte[] payload;
            try
            {
                payload = Encode(image, settings);
            }
            catch (InvalidOperationException)
            {
                // Palette candidate on an image with more than 256 colors; skip it.
                continue;
            }

            if (best is null || payload.Length < best.Length)
                best = payload;
        }

        // The first candidate (LZ77) never throws, so best is always assigned.
        return best!;
    }

    private static IEnumerable<Vp8LEncodeSettings> Candidates(int effort)
    {
        yield return new Vp8LEncodeSettings { Lz77 = true };

        if (effort >= 2)
        {
            yield return new Vp8LEncodeSettings { Lz77 = true, SubtractGreen = true };
            yield return new Vp8LEncodeSettings { Lz77 = true, Palette = true };
        }

        if (effort >= 4)
        {
            yield return new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = 2 };
            yield return new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = 0 };
        }

        if (effort >= 4)
            yield return new Vp8LEncodeSettings { Lz77 = true, ColorCacheBits = 8 };

        if (effort >= 6)
        {
            yield return new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = 2, SubtractGreen = true };
            yield return new Vp8LEncodeSettings { Lz77 = true, Predictor = true, PredictorMode = 11 };
            yield return new Vp8LEncodeSettings { Lz77 = true, SubtractGreen = true, ColorCacheBits = 8 };
        }
    }

    /// <summary>Encodes an image, optionally applying the subtract-green transform.</summary>
    /// <param name="image">The source image (RGB or RGBA).</param>
    /// <param name="subtractGreen">Whether to apply the subtract-green transform.</param>
    /// <returns>The <c>VP8L</c> chunk payload.</returns>
    public static byte[] Encode(WebPImage image, bool subtractGreen)
        => Encode(image, new Vp8LEncodeSettings { SubtractGreen = subtractGreen });

    /// <summary>Encodes an image to a VP8L chunk payload with the given settings.</summary>
    /// <param name="image">The source image (RGB or RGBA).</param>
    /// <param name="settings">The transform settings.</param>
    /// <returns>The <c>VP8L</c> chunk payload, including its 1-byte signature.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> or <paramref name="settings"/> is null.</exception>
    public static byte[] Encode(WebPImage image, Vp8LEncodeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(settings);

        var width = image.Width;
        var height = image.Height;
        var hasAlpha = image.Format == WebPColorFormat.Rgba;

        var argb = BuildArgb(image);

        // Each entry records the transform header to emit and its optional sub-image, in the order
        // the transforms are applied (the decoder inverts them in reverse).
        var transforms = new List<(Vp8LTransformType Type, uint[]? SubImage, int Bits, int NumColors)>(2);

        // The palette transform replaces the pixel data with a bundled index image and is used
        // exclusively of the channel transforms; otherwise apply the channel transforms in place.
        if (settings.Palette)
            argb = ApplyPalette(argb, width, height, transforms);
        else
            ApplyChannelTransforms(argb, width, height, settings, transforms);

        var writer = new Vp8LBitWriter(Math.Max(256, argb.Length));

        writer.PutBits(Signature, 8);
        writer.PutBits((uint)(width - 1), 14);
        writer.PutBits((uint)(height - 1), 14);
        writer.PutBit(hasAlpha ? 1u : 0u);
        writer.PutBits(0, 3); // version

        foreach (var (type, subImage, bits, numColors) in transforms)
        {
            writer.PutBit(1);
            writer.PutBits((uint)type, 2);
            if (type is Vp8LTransformType.Predictor or Vp8LTransformType.CrossColor)
            {
                writer.PutBits((uint)(bits - 2), 3);
                EncodeImageData(writer, subImage!, false, 0);
            }
            else if (type == Vp8LTransformType.ColorIndexing)
            {
                writer.PutBits((uint)(numColors - 1), 8);
                EncodeImageData(writer, subImage!, false, 0);
            }
        }
        writer.PutBit(0); // end of transforms

        if (settings.MetaHuffman && !settings.Palette)
            EncodeImageDataMeta(writer, argb, width, height, settings.MetaHuffmanBits, settings.MetaHuffmanGroups);
        else
            EncodeImageData(writer, argb, settings.Lz77, settings.ColorCacheBits);

        return writer.ToArray();
    }

    /// <summary>
    /// Encodes an 8-bit plane as a header-less VP8L stream carrying the value in the green channel,
    /// the format the lossy <c>ALPH</c> alpha chunk uses (see <see cref="Vp8LDecoder.DecodeAlpha"/>).
    /// No signature, dimensions, version, or transforms are written — just the entropy-coded image.
    /// </summary>
    /// <param name="plane">The per-pixel values, row-major, length <paramref name="width"/> × <paramref name="height"/>.</param>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="lz77">Whether to emit LZ77 back-references.</param>
    /// <returns>The header-less VP8L stream.</returns>
    public static byte[] EncodeAlpha(byte[] plane, int width, int height, bool lz77)
    {
        ArgumentNullException.ThrowIfNull(plane);
        var argb = new uint[width * height];
        for (var i = 0; i < argb.Length; i++)
            argb[i] = (uint)plane[i] << 8; // value in the green channel

        var writer = new Vp8LBitWriter(Math.Max(256, argb.Length));
        writer.PutBit(0); // no transforms
        EncodeImageData(writer, argb, lz77, 0);
        return writer.ToArray();
    }

    /// <summary>
    /// Writes the main image using meta-Huffman: an entropy image assigns one of several Huffman
    /// groups to each tile, and pixels are coded all-literal with their tile's group. Groups are
    /// distributed across tiles deterministically.
    /// </summary>
    private static void EncodeImageDataMeta(Vp8LBitWriter writer, uint[] argb, int width, int height, int bits, int requestedGroups)
    {
        var hW = Vp8LSubSample.Size(width, bits);
        var hH = Vp8LSubSample.Size(height, bits);
        var entropy = new uint[hW * hH];
        var maxGroup = 0;
        for (var ty = 0; ty < hH; ty++)
        {
            for (var tx = 0; tx < hW; tx++)
            {
                var g = (tx + ty) % Math.Max(1, requestedGroups);
                entropy[ty * hW + tx] = 0xFF000000u | ((uint)g << 8);
                if (g > maxGroup)
                    maxGroup = g;
            }
        }
        var numGroups = maxGroup + 1;

        int GroupOf(int pos)
        {
            var x = pos % width;
            var y = pos / width;
            return (int)((entropy[(y >> bits) * hW + (x >> bits)] >> 8) & 0xFFFF);
        }

        // Per-group literal histograms.
        var greenFreq = NewJagged(numGroups, LiteralGreenSymbols + LengthCodes);
        var redFreq = NewJagged(numGroups, 256);
        var blueFreq = NewJagged(numGroups, 256);
        var alphaFreq = NewJagged(numGroups, 256);
        var distFreq = NewJagged(numGroups, DistanceSymbols);

        for (var pos = 0; pos < argb.Length; pos++)
        {
            var g = GroupOf(pos);
            var p = argb[pos];
            greenFreq[g][(p >> 8) & 0xFF]++;
            redFreq[g][(p >> 16) & 0xFF]++;
            blueFreq[g][p & 0xFF]++;
            alphaFreq[g][(p >> 24) & 0xFF]++;
        }

        writer.PutBit(0); // no color cache
        writer.PutBit(1); // meta-Huffman present
        writer.PutBits((uint)(bits - 2), 3);
        EncodeImageData(writer, entropy, false, 0); // the entropy image (single group, literal)

        var green = new PrefixCodeWriter[numGroups];
        var red = new PrefixCodeWriter[numGroups];
        var blue = new PrefixCodeWriter[numGroups];
        var alpha = new PrefixCodeWriter[numGroups];
        for (var g = 0; g < numGroups; g++)
        {
            if (Array.TrueForAll(distFreq[g], f => f == 0))
                distFreq[g][0] = 1;

            var greenLengths = HuffmanLengthBuilder.Build(greenFreq[g], MaxCodeLength);
            var redLengths = HuffmanLengthBuilder.Build(redFreq[g], MaxCodeLength);
            var blueLengths = HuffmanLengthBuilder.Build(blueFreq[g], MaxCodeLength);
            var alphaLengths = HuffmanLengthBuilder.Build(alphaFreq[g], MaxCodeLength);
            var distLengths = HuffmanLengthBuilder.Build(distFreq[g], MaxCodeLength);

            Vp8LHuffman.WritePrefixCode(writer, greenLengths);
            Vp8LHuffman.WritePrefixCode(writer, redLengths);
            Vp8LHuffman.WritePrefixCode(writer, blueLengths);
            Vp8LHuffman.WritePrefixCode(writer, alphaLengths);
            Vp8LHuffman.WritePrefixCode(writer, distLengths);

            green[g] = new PrefixCodeWriter(greenLengths);
            red[g] = new PrefixCodeWriter(redLengths);
            blue[g] = new PrefixCodeWriter(blueLengths);
            alpha[g] = new PrefixCodeWriter(alphaLengths);
        }

        for (var pos = 0; pos < argb.Length; pos++)
        {
            var g = GroupOf(pos);
            WriteLiteral(writer, green[g], red[g], blue[g], alpha[g], argb[pos]);
        }
    }

    private static int[][] NewJagged(int groups, int size)
    {
        var result = new int[groups][];
        for (var g = 0; g < groups; g++)
            result[g] = new int[size];
        return result;
    }

    /// <summary>
    /// Applies the enabled channel transforms (predictor, cross-color, subtract-green) in place and
    /// records each so the decoder can invert them in reverse order.
    /// </summary>
    private static void ApplyChannelTransforms(uint[] argb, int width, int height, Vp8LEncodeSettings settings,
        List<(Vp8LTransformType Type, uint[]? SubImage, int Bits, int NumColors)> transforms)
    {
        if (settings.Predictor)
        {
            var bits = settings.PredictorBits;
            var modeImage = new uint[Vp8LSubSample.Size(width, bits) * Vp8LSubSample.Size(height, bits)];
            Array.Fill(modeImage, 0xFF000000u | ((uint)(settings.PredictorMode & 0x0F) << 8));
            PredictorTransform.Forward(argb, width, height, modeImage, bits);
            transforms.Add((Vp8LTransformType.Predictor, modeImage, bits, 0));
        }

        if (settings.CrossColor)
        {
            var bits = settings.CrossColorBits;
            var colorImage = new uint[Vp8LSubSample.Size(width, bits) * Vp8LSubSample.Size(height, bits)];
            Array.Fill(colorImage, 0xFF000000u
                                    | ((uint)settings.CrossColorRedToBlue << 16)
                                    | ((uint)settings.CrossColorGreenToBlue << 8)
                                    | settings.CrossColorGreenToRed);
            CrossColorTransform.Forward(argb, width, height, colorImage, bits);
            transforms.Add((Vp8LTransformType.CrossColor, colorImage, bits, 0));
        }

        if (settings.SubtractGreen)
        {
            SubtractGreenTransform.Forward(argb);
            transforms.Add((Vp8LTransformType.SubtractGreen, null, 0, 0));
        }
    }

    /// <summary>
    /// Builds a palette from the image's distinct colors, records the color-indexing transform
    /// (with its delta-coded palette sub-image), and returns the bundled index image to encode.
    /// </summary>
    /// <exception cref="InvalidOperationException">The image has more than 256 distinct colors.</exception>
    private static uint[] ApplyPalette(uint[] argb, int width, int height,
        List<(Vp8LTransformType Type, uint[]? SubImage, int Bits, int NumColors)> transforms)
    {
        var lookup = new Dictionary<uint, int>();
        var palette = new List<uint>();
        var indices = new byte[argb.Length];
        for (var i = 0; i < argb.Length; i++)
        {
            if (!lookup.TryGetValue(argb[i], out var index))
            {
                if (palette.Count >= 256)
                    throw new InvalidOperationException("Palette encoding requires at most 256 distinct colors.");
                index = palette.Count;
                lookup[argb[i]] = index;
                palette.Add(argb[i]);
            }
            indices[i] = (byte)index;
        }

        var numColors = palette.Count;
        var bits = ColorIndexingTransform.BitsForColorCount(numColors);
        var rawPalette = ColorIndexingTransform.DeltaEncodePalette(palette.ToArray());
        transforms.Add((Vp8LTransformType.ColorIndexing, rawPalette, bits, numColors));

        return ColorIndexingTransform.Bundle(indices, width, height, bits);
    }

    private static void EnsureNonEmpty(int[] frequencies)
    {
        if (Array.TrueForAll(frequencies, f => f == 0))
            frequencies[0] = 1;
    }

    private enum EventKind : byte { Literal, Copy, CacheHit }

    private readonly record struct ImageEvent(EventKind Kind, uint Pixel, int Length, int Distance, int Index);

    /// <summary>
    /// Writes one entropy-coded image stream: an optional color cache, a single Huffman group, and
    /// either all-literal or LZ77 tokens. Reused for the main image and transform sub-images
    /// (sub-images pass <paramref name="cacheBits"/> = 0 and are always literal).
    /// </summary>
    private static void EncodeImageData(Vp8LBitWriter writer, ReadOnlySpan<uint> argb, bool useLz77, int cacheBits)
    {
        var cache = cacheBits > 0 ? new ColorCache(cacheBits) : null;
        var cacheSize = cache?.Size ?? 0;
        var events = BuildEvents(argb, useLz77, cache);

        var greenFreq = new int[LiteralGreenSymbols + LengthCodes + cacheSize];
        var redFreq = new int[256];
        var blueFreq = new int[256];
        var alphaFreq = new int[256];
        var distFreq = new int[DistanceSymbols];

        foreach (var e in events)
        {
            switch (e.Kind)
            {
                case EventKind.Literal:
                    greenFreq[(e.Pixel >> 8) & 0xFF]++;
                    redFreq[(e.Pixel >> 16) & 0xFF]++;
                    blueFreq[e.Pixel & 0xFF]++;
                    alphaFreq[(e.Pixel >> 24) & 0xFF]++;
                    break;
                case EventKind.Copy:
                    LzPrefix.Encode(e.Length, out var lenCode, out _, out _);
                    greenFreq[LiteralGreenSymbols + lenCode]++;
                    LzPrefix.Encode(e.Distance + PlaneCodeBias, out var distCode, out _, out _);
                    distFreq[distCode]++;
                    break;
                case EventKind.CacheHit:
                    greenFreq[LiteralGreenSymbols + LengthCodes + e.Index]++;
                    break;
            }
        }

        // Every code must have at least one symbol, even when the stream uses none of it (e.g. an
        // all-cache-hit or all-copy image emits no literals, so red/blue/alpha stay empty).
        EnsureNonEmpty(greenFreq);
        EnsureNonEmpty(redFreq);
        EnsureNonEmpty(blueFreq);
        EnsureNonEmpty(alphaFreq);
        EnsureNonEmpty(distFreq);

        var greenLengths = HuffmanLengthBuilder.Build(greenFreq, MaxCodeLength);
        var redLengths = HuffmanLengthBuilder.Build(redFreq, MaxCodeLength);
        var blueLengths = HuffmanLengthBuilder.Build(blueFreq, MaxCodeLength);
        var alphaLengths = HuffmanLengthBuilder.Build(alphaFreq, MaxCodeLength);
        var distLengths = HuffmanLengthBuilder.Build(distFreq, MaxCodeLength);

        if (cacheBits > 0)
        {
            writer.PutBit(1);
            writer.PutBits((uint)cacheBits, 4);
        }
        else
        {
            writer.PutBit(0); // no color cache
        }
        writer.PutBit(0); // no meta-Huffman (single group)

        Vp8LHuffman.WritePrefixCode(writer, greenLengths);
        Vp8LHuffman.WritePrefixCode(writer, redLengths);
        Vp8LHuffman.WritePrefixCode(writer, blueLengths);
        Vp8LHuffman.WritePrefixCode(writer, alphaLengths);
        Vp8LHuffman.WritePrefixCode(writer, distLengths);

        var green = new PrefixCodeWriter(greenLengths);
        var red = new PrefixCodeWriter(redLengths);
        var blue = new PrefixCodeWriter(blueLengths);
        var alpha = new PrefixCodeWriter(alphaLengths);
        var distance = new PrefixCodeWriter(distLengths);

        foreach (var e in events)
        {
            switch (e.Kind)
            {
                case EventKind.Literal:
                    WriteLiteral(writer, green, red, blue, alpha, e.Pixel);
                    break;
                case EventKind.Copy:
                    LzPrefix.Encode(e.Length, out var lenCode, out var lenExtraBits, out var lenExtra);
                    green.WriteSymbol(writer, LiteralGreenSymbols + lenCode);
                    writer.PutBits((uint)lenExtra, lenExtraBits);
                    LzPrefix.Encode(e.Distance + PlaneCodeBias, out var distCode, out var distExtraBits, out var distExtra);
                    distance.WriteSymbol(writer, distCode);
                    writer.PutBits((uint)distExtra, distExtraBits);
                    break;
                case EventKind.CacheHit:
                    green.WriteSymbol(writer, LiteralGreenSymbols + LengthCodes + e.Index);
                    break;
            }
        }
    }

    /// <summary>
    /// Expands the LZ77/literal token stream into concrete events, simulating the color cache in
    /// lock-step with the decoder so cache hits and insertions match exactly.
    /// </summary>
    private static List<ImageEvent> BuildEvents(ReadOnlySpan<uint> argb, bool useLz77, ColorCache? cache)
    {
        var events = new List<ImageEvent>(argb.Length);

        void EmitPixel(uint pixel)
        {
            if (cache is not null && cache.Lookup(cache.GetIndex(pixel)) == pixel)
                events.Add(new ImageEvent(EventKind.CacheHit, 0, 0, 0, cache.GetIndex(pixel)));
            else
                events.Add(new ImageEvent(EventKind.Literal, pixel, 0, 0, 0));
            cache?.Insert(pixel);
        }

        if (useLz77)
        {
            var tokens = Vp8LLz77.Encode(argb);
            var pos = 0;
            foreach (var t in tokens)
            {
                if (t.IsLiteral)
                {
                    EmitPixel(argb[pos]);
                    pos++;
                }
                else
                {
                    events.Add(new ImageEvent(EventKind.Copy, 0, t.Length, t.Distance, 0));
                    for (var k = 0; k < t.Length; k++)
                        cache?.Insert(argb[pos + k]);
                    pos += t.Length;
                }
            }
        }
        else
        {
            foreach (var p in argb)
                EmitPixel(p);
        }

        return events;
    }

    private static void WriteLiteral(Vp8LBitWriter writer, PrefixCodeWriter green, PrefixCodeWriter red,
        PrefixCodeWriter blue, PrefixCodeWriter alpha, uint p)
    {
        green.WriteSymbol(writer, (int)((p >> 8) & 0xFF));
        red.WriteSymbol(writer, (int)((p >> 16) & 0xFF));
        blue.WriteSymbol(writer, (int)(p & 0xFF));
        alpha.WriteSymbol(writer, (int)((p >> 24) & 0xFF));
    }

    private static uint[] BuildArgb(WebPImage image)
    {
        var total = image.Width * image.Height;
        var argb = new uint[total];
        var src = image.PixelData;
        var components = image.ComponentCount;

        for (var i = 0; i < total; i++)
        {
            var off = i * components;
            uint r = src[off];
            uint g = src[off + 1];
            uint b = src[off + 2];
            uint a = components == 4 ? src[off + 3] : 255u;
            argb[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }

        return argb;
    }
}

using WebPSharp.Api.Exceptions;
using WebPSharp.Vp8L;

namespace WebPSharp.Vp8;

/// <summary>
/// Decodes the WebP <c>ALPH</c> alpha chunk that carries the alpha channel for a lossy (VP8) image.
/// The chunk stores an optionally-filtered alpha plane, either raw or compressed with the lossless
/// (VP8L) format; this reverses both to produce the 8-bit alpha plane. Follows RFC / libwebp.
/// </summary>
internal static class Vp8AlphaDecoder
{
    private const int NoCompression = 0;
    private const int LosslessCompression = 1;

    private const int FilterNone = 0;
    private const int FilterHorizontal = 1;
    private const int FilterVertical = 2;
    private const int FilterGradient = 3;

    /// <summary>Decodes an ALPH chunk into an 8-bit alpha plane.</summary>
    /// <param name="payload">The ALPH chunk payload.</param>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <returns>The alpha plane, <paramref name="width"/> × <paramref name="height"/>, row-major.</returns>
    /// <exception cref="WebPFormatException">The chunk is malformed or uses an unsupported feature.</exception>
    public static byte[] Decode(ReadOnlySpan<byte> payload, int width, int height)
    {
        if (payload.Length < 1)
            throw new WebPFormatException("ALPH chunk is empty.");

        var header = payload[0];
        var method = header & 0x03;
        var filter = (header >> 2) & 0x03;
        var preprocessing = (header >> 4) & 0x03;
        var reserved = (header >> 6) & 0x03;
        if (reserved != 0)
            throw new WebPFormatException("ALPH chunk has non-zero reserved bits.");
        if (preprocessing > 1)
            throw new WebPFormatException($"ALPH chunk has invalid pre-processing value {preprocessing}.");

        var total = width * height;
        var data = payload[1..];

        byte[] deltas;
        if (method == NoCompression)
        {
            if (data.Length < total)
                throw new WebPFormatException("ALPH chunk is truncated.");
            deltas = data[..total].ToArray();
        }
        else if (method == LosslessCompression)
        {
            deltas = Vp8LDecoder.DecodeAlpha(data, width, height);
        }
        else
        {
            throw new WebPFormatException($"Unknown ALPH compression method {method}.");
        }

        return Unfilter(deltas, width, height, filter);
    }

    private static byte[] Unfilter(byte[] deltas, int width, int height, int filter)
    {
        if (filter == FilterNone)
            return deltas;

        var output = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            var prevRow = y == 0 ? -1 : (y - 1) * width;
            switch (filter)
            {
                case FilterHorizontal:
                    HorizontalUnfilter(deltas, output, row, prevRow, width);
                    break;
                case FilterVertical:
                    if (prevRow < 0)
                        HorizontalUnfilter(deltas, output, row, -1, width);
                    else
                        for (var i = 0; i < width; i++)
                            output[row + i] = (byte)(output[prevRow + i] + deltas[row + i]);
                    break;
                case FilterGradient:
                    if (prevRow < 0)
                    {
                        HorizontalUnfilter(deltas, output, row, -1, width);
                    }
                    else
                    {
                        int top = output[prevRow];
                        var topLeft = top;
                        var left = top;
                        for (var i = 0; i < width; i++)
                        {
                            top = output[prevRow + i];
                            left = (deltas[row + i] + GradientPredictor(left, top, topLeft)) & 0xFF;
                            topLeft = top;
                            output[row + i] = (byte)left;
                        }
                    }
                    break;
            }
        }
        return output;
    }

    private static void HorizontalUnfilter(byte[] deltas, byte[] output, int row, int prevRow, int width)
    {
        var pred = prevRow < 0 ? 0 : output[prevRow];
        for (var i = 0; i < width; i++)
        {
            var v = (pred + deltas[row + i]) & 0xFF;
            output[row + i] = (byte)v;
            pred = v;
        }
    }

    private static int GradientPredictor(int a, int b, int c)
    {
        var g = a + b - c;
        return g < 0 ? 0 : g > 255 ? 255 : g;
    }
}

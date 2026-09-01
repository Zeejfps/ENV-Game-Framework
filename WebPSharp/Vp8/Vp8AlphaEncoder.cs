using WebPSharp.Vp8L;

namespace WebPSharp.Vp8;

/// <summary>
/// Encodes the WebP <c>ALPH</c> alpha chunk that carries the alpha channel for a lossy (VP8) image.
/// The alpha plane is optionally spatially filtered (horizontal / vertical / gradient) and then
/// stored either raw or compressed as a header-less lossless (VP8L) stream. Each filter and storage
/// method is the exact forward of <see cref="Vp8AlphaDecoder"/>; the smallest encoding is chosen.
/// </summary>
internal static class Vp8AlphaEncoder
{
    private const int NoCompression = 0;
    private const int LosslessCompression = 1;

    private const int FilterNone = 0;
    private const int FilterHorizontal = 1;
    private const int FilterVertical = 2;
    private const int FilterGradient = 3;

    /// <summary>
    /// Builds the ALPH chunk payload for an image's alpha channel, or returns null when the image is
    /// fully opaque (in which case no alpha chunk is needed).
    /// </summary>
    /// <param name="rgba">The interleaved RGBA pixel data.</param>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <returns>The ALPH chunk payload, or null if every alpha sample is 255.</returns>
    public static byte[]? Encode(byte[] rgba, int width, int height)
    {
        var total = width * height;
        var alpha = new byte[total];
        var opaque = true;
        for (var i = 0; i < total; i++)
        {
            alpha[i] = rgba[i * 4 + 3];
            if (alpha[i] != 255)
                opaque = false;
        }
        if (opaque)
            return null;

        byte[]? best = null;
        foreach (var filter in stackalloc[] { FilterNone, FilterHorizontal, FilterVertical, FilterGradient })
        {
            var deltas = ApplyFilter(alpha, width, height, filter);

            // Compressed (VP8L) candidate.
            var compressed = Vp8LEncoder.EncodeAlpha(deltas, width, height, lz77: true);
            best = Smaller(best, Build(LosslessCompression, filter, compressed));

            // Raw candidate (wins only for tiny or incompressible alpha).
            best = Smaller(best, Build(NoCompression, filter, deltas));
        }
        return best!;
    }

    private static byte[] Build(int method, int filter, byte[] data)
    {
        var payload = new byte[1 + data.Length];
        payload[0] = (byte)((method & 0x03) | ((filter & 0x03) << 2));
        Array.Copy(data, 0, payload, 1, data.Length);
        return payload;
    }

    private static byte[] Smaller(byte[]? a, byte[] b) => a is null || b.Length < a.Length ? b : a;

    // Forward of Vp8AlphaDecoder.Unfilter: produces the residuals such that unfiltering with the
    // same filter reproduces the original alpha plane exactly.
    private static byte[] ApplyFilter(byte[] alpha, int width, int height, int filter)
    {
        if (filter == FilterNone)
            return (byte[])alpha.Clone();

        var deltas = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            var prevRow = (y - 1) * width;
            switch (filter)
            {
                case FilterHorizontal:
                    HorizontalFilter(alpha, deltas, row, y == 0 ? -1 : prevRow, width);
                    break;
                case FilterVertical:
                    if (y == 0)
                        HorizontalFilter(alpha, deltas, row, -1, width);
                    else
                        for (var i = 0; i < width; i++)
                            deltas[row + i] = (byte)(alpha[row + i] - alpha[prevRow + i]);
                    break;
                case FilterGradient:
                    if (y == 0)
                    {
                        HorizontalFilter(alpha, deltas, row, -1, width);
                    }
                    else
                    {
                        for (var i = 0; i < width; i++)
                        {
                            int pred = i == 0
                                ? alpha[prevRow]
                                : GradientPredictor(alpha[row + i - 1], alpha[prevRow + i], alpha[prevRow + i - 1]);
                            deltas[row + i] = (byte)(alpha[row + i] - pred);
                        }
                    }
                    break;
            }
        }
        return deltas;
    }

    private static void HorizontalFilter(byte[] alpha, byte[] deltas, int row, int prevRow, int width)
    {
        var pred = prevRow < 0 ? 0 : alpha[prevRow];
        for (var i = 0; i < width; i++)
        {
            deltas[row + i] = (byte)(alpha[row + i] - pred);
            pred = alpha[row + i];
        }
    }

    private static int GradientPredictor(int a, int b, int c)
    {
        var g = a + b - c;
        return g < 0 ? 0 : g > 255 ? 255 : g;
    }
}

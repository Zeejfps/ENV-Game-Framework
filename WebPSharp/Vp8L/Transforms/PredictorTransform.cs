namespace WebPSharp.Vp8L.Transforms;

/// <summary>
/// The VP8L predictor transform. Each pixel is replaced by the residual between its true value and
/// a spatial prediction chosen per tile by a sub-sampled mode image (the mode is the low nibble of
/// the tile pixel's green channel). Boundary rules follow the specification exactly: pixel (0,0)
/// predicts opaque black, the rest of the first row predicts left, and the first column of every
/// other row predicts top. The top-right neighbor of the last column resolves, by flat indexing,
/// to the first pixel of the current row — matching the reference decoder.
/// </summary>
internal static class PredictorTransform
{
    private const uint OpaqueBlack = 0xFF000000u;

    /// <summary>Reconstructs pixels from residuals in place (decoder side).</summary>
    /// <param name="argb">The residual pixels, replaced with reconstructed pixels.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="modeImage">The sub-sampled predictor mode image.</param>
    /// <param name="bits">The tile size in bits.</param>
    public static void Inverse(uint[] argb, int width, int height, uint[] modeImage, int bits)
    {
        var tilesPerRow = Vp8LSubSample.Size(width, bits);

        argb[0] = Vp8LPredictors.AddPixels(argb[0], OpaqueBlack);
        for (var x = 1; x < width; x++)
            argb[x] = Vp8LPredictors.AddPixels(argb[x], argb[x - 1]);

        for (var y = 1; y < height; y++)
        {
            var rowStart = y * width;
            var modeRow = (y >> bits) * tilesPerRow;
            argb[rowStart] = Vp8LPredictors.AddPixels(argb[rowStart], argb[rowStart - width]);

            for (var x = 1; x < width; x++)
            {
                var idx = rowStart + x;
                var mode = (int)((modeImage[modeRow + (x >> bits)] >> 8) & 0x0F);
                var pred = Vp8LPredictors.Predict(mode, argb[idx - 1], argb[idx - width], argb[idx - width - 1], argb[idx - width + 1]);
                argb[idx] = Vp8LPredictors.AddPixels(argb[idx], pred);
            }
        }
    }

    /// <summary>Computes residuals from pixels in place (encoder side).</summary>
    /// <param name="argb">The pixels, replaced with residuals.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="modeImage">The sub-sampled predictor mode image.</param>
    /// <param name="bits">The tile size in bits.</param>
    public static void Forward(uint[] argb, int width, int height, uint[] modeImage, int bits)
    {
        var tilesPerRow = Vp8LSubSample.Size(width, bits);

        // Process in reverse so each prediction reads still-original neighbors (all at lower indices).
        for (var y = height - 1; y >= 0; y--)
        {
            var rowStart = y * width;
            var modeRow = (y >> bits) * tilesPerRow;
            for (var x = width - 1; x >= 0; x--)
            {
                var idx = rowStart + x;
                uint pred;
                if (y == 0)
                    pred = x == 0 ? OpaqueBlack : argb[idx - 1];
                else if (x == 0)
                    pred = argb[idx - width];
                else
                {
                    var mode = (int)((modeImage[modeRow + (x >> bits)] >> 8) & 0x0F);
                    pred = Vp8LPredictors.Predict(mode, argb[idx - 1], argb[idx - width], argb[idx - width - 1], argb[idx - width + 1]);
                }
                argb[idx] = Vp8LPredictors.SubtractPixels(argb[idx], pred);
            }
        }
    }
}

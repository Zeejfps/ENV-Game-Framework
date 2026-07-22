namespace JpegSharp.Color;

/// <summary>
/// Downsamples and upsamples component planes between full resolution and the reduced
/// resolution implied by a component's sampling factors. Downsampling box-averages each
/// sample block; upsampling replicates samples (nearest-neighbour), which is spec-valid and
/// exact for constant regions.
/// </summary>
/// <remarks>
/// The horizontal and vertical factors express how many full-resolution samples map to one
/// subsampled sample. Common layouts: 4:4:4 → (1,1), 4:2:2 → (2,1), 4:2:0 → (2,2),
/// 4:1:1 → (4,1). Arbitrary factors are supported.
/// </remarks>
internal static class ChromaSampler
{
    /// <summary>Returns the subsampled extent for a full-resolution extent and factor (rounded up).</summary>
    /// <param name="fullSize">The full-resolution dimension.</param>
    /// <param name="factor">The sampling factor (samples averaged per output sample).</param>
    /// <returns>The subsampled dimension.</returns>
    public static int SubsampledSize(int fullSize, int factor) => (fullSize + factor - 1) / factor;

    /// <summary>
    /// Box-averages a full-resolution plane down into a subsampled plane.
    /// </summary>
    /// <param name="src">Full-resolution samples (row-major).</param>
    /// <param name="srcWidth">Full-resolution width.</param>
    /// <param name="srcHeight">Full-resolution height.</param>
    /// <param name="hFactor">Horizontal averaging factor (≥ 1).</param>
    /// <param name="vFactor">Vertical averaging factor (≥ 1).</param>
    /// <param name="dst">Destination subsampled samples (row-major).</param>
    /// <param name="dstWidth">Subsampled width; must equal <see cref="SubsampledSize"/>(srcWidth, hFactor).</param>
    /// <param name="dstHeight">Subsampled height; must equal <see cref="SubsampledSize"/>(srcHeight, vFactor).</param>
    public static void Downsample(
        ReadOnlySpan<byte> src, int srcWidth, int srcHeight,
        int hFactor, int vFactor,
        Span<byte> dst, int dstWidth, int dstHeight)
    {
        ValidateFactors(hFactor, vFactor);
        ValidateDimensions(src.Length, srcWidth, srcHeight, nameof(src));
        ValidateDimensions(dst.Length, dstWidth, dstHeight, nameof(dst));

        for (var dy = 0; dy < dstHeight; dy++)
        {
            var sy0 = dy * vFactor;
            for (var dx = 0; dx < dstWidth; dx++)
            {
                var sx0 = dx * hFactor;
                var sum = 0;
                var count = 0;
                for (var vy = 0; vy < vFactor; vy++)
                {
                    var sy = sy0 + vy;
                    if (sy >= srcHeight)
                        break;
                    var rowStart = sy * srcWidth;
                    for (var vx = 0; vx < hFactor; vx++)
                    {
                        var sx = sx0 + vx;
                        if (sx >= srcWidth)
                            break;
                        sum += src[rowStart + sx];
                        count++;
                    }
                }

                dst[dy * dstWidth + dx] = (byte)((sum + (count >> 1)) / count);
            }
        }
    }

    /// <summary>
    /// Replicates a subsampled plane up to full resolution.
    /// </summary>
    /// <param name="src">Subsampled samples (row-major).</param>
    /// <param name="srcWidth">Subsampled width.</param>
    /// <param name="srcHeight">Subsampled height.</param>
    /// <param name="hFactor">Horizontal replication factor (≥ 1).</param>
    /// <param name="vFactor">Vertical replication factor (≥ 1).</param>
    /// <param name="dst">Destination full-resolution samples (row-major).</param>
    /// <param name="dstWidth">Full-resolution width.</param>
    /// <param name="dstHeight">Full-resolution height.</param>
    public static void Upsample(
        ReadOnlySpan<byte> src, int srcWidth, int srcHeight,
        int hFactor, int vFactor,
        Span<byte> dst, int dstWidth, int dstHeight)
    {
        ValidateFactors(hFactor, vFactor);
        ValidateDimensions(src.Length, srcWidth, srcHeight, nameof(src));
        ValidateDimensions(dst.Length, dstWidth, dstHeight, nameof(dst));

        for (var dy = 0; dy < dstHeight; dy++)
        {
            var sy = dy / vFactor;
            if (sy >= srcHeight)
                sy = srcHeight - 1;
            var srcRow = sy * srcWidth;
            var dstRow = dy * dstWidth;
            for (var dx = 0; dx < dstWidth; dx++)
            {
                var sx = dx / hFactor;
                if (sx >= srcWidth)
                    sx = srcWidth - 1;
                dst[dstRow + dx] = src[srcRow + sx];
            }
        }
    }

    /// <summary>
    /// Upsamples a subsampled plane to a target resolution using centered bilinear
    /// interpolation, producing smoother chroma than nearest-neighbour replication. Handles
    /// arbitrary ratios and is exact when the dimensions already match.
    /// </summary>
    /// <param name="src">Subsampled samples (row-major).</param>
    /// <param name="srcWidth">Subsampled width.</param>
    /// <param name="srcHeight">Subsampled height.</param>
    /// <param name="dst">Destination full-resolution samples (row-major).</param>
    /// <param name="dstWidth">Full-resolution width.</param>
    /// <param name="dstHeight">Full-resolution height.</param>
    public static void UpsampleLinear(
        ReadOnlySpan<byte> src, int srcWidth, int srcHeight,
        Span<byte> dst, int dstWidth, int dstHeight)
    {
        ValidateDimensions(src.Length, srcWidth, srcHeight, nameof(src));
        ValidateDimensions(dst.Length, dstWidth, dstHeight, nameof(dst));
        if (srcWidth == 0 || srcHeight == 0)
            return;

        var scaleX = (double)srcWidth / dstWidth;
        var scaleY = (double)srcHeight / dstHeight;

        for (var dy = 0; dy < dstHeight; dy++)
        {
            var sy = (dy + 0.5) * scaleY - 0.5;
            var y0 = (int)Math.Floor(sy);
            var fy = sy - y0;
            var y0c = Math.Clamp(y0, 0, srcHeight - 1);
            var y1c = Math.Clamp(y0 + 1, 0, srcHeight - 1);
            var row0 = y0c * srcWidth;
            var row1 = y1c * srcWidth;
            var dstRow = dy * dstWidth;

            for (var dx = 0; dx < dstWidth; dx++)
            {
                var sx = (dx + 0.5) * scaleX - 0.5;
                var x0 = (int)Math.Floor(sx);
                var fx = sx - x0;
                var x0c = Math.Clamp(x0, 0, srcWidth - 1);
                var x1c = Math.Clamp(x0 + 1, 0, srcWidth - 1);

                var top = src[row0 + x0c] * (1 - fx) + src[row0 + x1c] * fx;
                var bottom = src[row1 + x0c] * (1 - fx) + src[row1 + x1c] * fx;
                var value = (int)Math.Round(top * (1 - fy) + bottom * fy);
                dst[dstRow + dx] = (byte)Math.Clamp(value, 0, 255);
            }
        }
    }

    /// <summary>
    /// High-precision counterpart of <see cref="Downsample(ReadOnlySpan{byte},int,int,int,int,Span{byte},int,int)"/>,
    /// box-averaging <see cref="ushort"/> samples.
    /// </summary>
    /// <param name="src">Full-resolution samples (row-major).</param>
    /// <param name="srcWidth">Full-resolution width.</param>
    /// <param name="srcHeight">Full-resolution height.</param>
    /// <param name="hFactor">Horizontal averaging factor (≥ 1).</param>
    /// <param name="vFactor">Vertical averaging factor (≥ 1).</param>
    /// <param name="dst">Destination subsampled samples (row-major).</param>
    /// <param name="dstWidth">Subsampled width.</param>
    /// <param name="dstHeight">Subsampled height.</param>
    public static void Downsample(
        ReadOnlySpan<ushort> src, int srcWidth, int srcHeight,
        int hFactor, int vFactor,
        Span<ushort> dst, int dstWidth, int dstHeight)
    {
        ValidateFactors(hFactor, vFactor);
        ValidateDimensions(src.Length, srcWidth, srcHeight, nameof(src));
        ValidateDimensions(dst.Length, dstWidth, dstHeight, nameof(dst));

        for (var dy = 0; dy < dstHeight; dy++)
        {
            var sy0 = dy * vFactor;
            for (var dx = 0; dx < dstWidth; dx++)
            {
                var sx0 = dx * hFactor;
                var sum = 0;
                var count = 0;
                for (var vy = 0; vy < vFactor; vy++)
                {
                    var sy = sy0 + vy;
                    if (sy >= srcHeight)
                        break;
                    var rowStart = sy * srcWidth;
                    for (var vx = 0; vx < hFactor; vx++)
                    {
                        var sx = sx0 + vx;
                        if (sx >= srcWidth)
                            break;
                        sum += src[rowStart + sx];
                        count++;
                    }
                }

                dst[dy * dstWidth + dx] = (ushort)((sum + (count >> 1)) / count);
            }
        }
    }

    /// <summary>
    /// High-precision counterpart of <see cref="UpsampleLinear(ReadOnlySpan{byte},int,int,Span{byte},int,int)"/>,
    /// operating on <see cref="ushort"/> samples and clamping to <paramref name="maxValue"/>.
    /// </summary>
    /// <param name="src">Subsampled samples (row-major).</param>
    /// <param name="srcWidth">Subsampled width.</param>
    /// <param name="srcHeight">Subsampled height.</param>
    /// <param name="dst">Destination full-resolution samples (row-major).</param>
    /// <param name="dstWidth">Full-resolution width.</param>
    /// <param name="dstHeight">Full-resolution height.</param>
    /// <param name="maxValue">The maximum sample value, e.g. 4095 for 12-bit.</param>
    public static void UpsampleLinear(
        ReadOnlySpan<ushort> src, int srcWidth, int srcHeight,
        Span<ushort> dst, int dstWidth, int dstHeight, int maxValue)
    {
        ValidateDimensions(src.Length, srcWidth, srcHeight, nameof(src));
        ValidateDimensions(dst.Length, dstWidth, dstHeight, nameof(dst));
        if (srcWidth == 0 || srcHeight == 0)
            return;

        var scaleX = (double)srcWidth / dstWidth;
        var scaleY = (double)srcHeight / dstHeight;

        for (var dy = 0; dy < dstHeight; dy++)
        {
            var sy = (dy + 0.5) * scaleY - 0.5;
            var y0 = (int)Math.Floor(sy);
            var fy = sy - y0;
            var y0c = Math.Clamp(y0, 0, srcHeight - 1);
            var y1c = Math.Clamp(y0 + 1, 0, srcHeight - 1);
            var row0 = y0c * srcWidth;
            var row1 = y1c * srcWidth;
            var dstRow = dy * dstWidth;

            for (var dx = 0; dx < dstWidth; dx++)
            {
                var sx = (dx + 0.5) * scaleX - 0.5;
                var x0 = (int)Math.Floor(sx);
                var fx = sx - x0;
                var x0c = Math.Clamp(x0, 0, srcWidth - 1);
                var x1c = Math.Clamp(x0 + 1, 0, srcWidth - 1);

                var top = src[row0 + x0c] * (1 - fx) + src[row0 + x1c] * fx;
                var bottom = src[row1 + x0c] * (1 - fx) + src[row1 + x1c] * fx;
                var value = (int)Math.Round(top * (1 - fy) + bottom * fy);
                dst[dstRow + dx] = (ushort)Math.Clamp(value, 0, maxValue);
            }
        }
    }

    private static void ValidateFactors(int hFactor, int vFactor)
    {
        if (hFactor < 1)
            throw new ArgumentOutOfRangeException(nameof(hFactor), "Sampling factor must be at least 1.");
        if (vFactor < 1)
            throw new ArgumentOutOfRangeException(nameof(vFactor), "Sampling factor must be at least 1.");
    }

    private static void ValidateDimensions(int length, int width, int height, string name)
    {
        if (width < 0 || height < 0)
            throw new ArgumentOutOfRangeException(name, "Dimensions must be non-negative.");
        if (length != width * height)
            throw new ArgumentException($"Buffer length {length} does not match {width}x{height}.", name);
    }
}

using System.Runtime.CompilerServices;

namespace SoftwareRendererModule;

public sealed class Bitmap
{
    public int Width { get; }
    public int Height { get; }
    public Span<uint> Pixels => _pixels;

    private readonly uint[] _pixels;

    public Bitmap(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new uint[width * height];
    }

    public void Fill(uint color)
    {
        Array.Fill(_pixels, color);
    }

    public void FillLine(int x, int y, int width, uint color)
    {
        var index = y * Width + x;
        Array.Fill(_pixels, color, index, width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPixel(int x, int y, uint color)
    {
        var index = y * Width + x;
        _pixels[index] = color;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPixel(int index, uint color)
    {
        _pixels[index] = color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public uint GetPixel(int x, int y)
    {
        var index = y * Width + x;
        return _pixels[index];
    }

    public void Blit(Bitmap srcBmp, int dstX, int dstY, int dstW, int dstH, int srcX, int srcY, int srcW, int srcH)
    {
        if (dstW <= 0 || dstH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return;
        }

        // --- Clipping ---
        // We calculate the final rectangle to iterate over, which is the intersection of
        // the destination rectangle and the destination bitmap's bounds.

        // Start with the destination rectangle
        var clipX1 = dstX;
        var clipY1 = dstY;
        var clipX2 = dstX + dstW;
        var clipY2 = dstY + dstH;

        // Clip it against the destination bitmap's dimensions (0, 0, Width, Height)
        clipX1 = Math.Max(0, clipX1);
        clipY1 = Math.Max(0, clipY1);
        clipX2 = Math.Min(Width, clipX2);
        clipY2 = Math.Min(Height, clipY2);

        // If the clipped rectangle has no area, there's nothing to draw.
        if (clipX1 >= clipX2 || clipY1 >= clipY2)
        {
            return;
        }

        // --- Fast Path for No Scaling ---
        // If the source and destination dimensions are the same, we can use a much faster
        // row-by-row copy instead of a pixel-by-pixel calculation.
        if (srcW == dstW && srcH == dstH)
        {
            // The width of the data to copy per row is the width of the clipped rectangle.
            var copyWidth = clipX2 - clipX1;

            // Calculate the starting source X coordinate, adjusted for any clipping on the left.
            var finalSrcX = srcX + (clipX1 - dstX);

            // Iterate over each visible row of the destination rectangle.
            for (var y = clipY1; y < clipY2; y++)
            {
                // Calculate the corresponding source Y coordinate for this row.
                var finalSrcY = srcY + (y - dstY);

                // --- Source Bounds Check ---
                // Ensure the source coordinates are within the source bitmap's bounds.
                if (finalSrcX < 0 || finalSrcX + copyWidth > srcBmp.Width ||
                    finalSrcY < 0 || finalSrcY >= srcBmp.Height) continue;

                // Get Spans representing the source and destination slices for this row.
                var srcSpan = srcBmp.Pixels.Slice(finalSrcY * srcBmp.Width + finalSrcX, copyWidth);
                var dstSpan = Pixels.Slice(y * this.Width + clipX1, copyWidth);

                // Copy the entire row segment in one highly optimized operation.
                srcSpan.CopyTo(dstSpan);
            }
            return;
        }

        // --- General Path for Scaling ---
        // This path is used when the source and destination rectangles have different sizes.
        // It uses nearest-neighbor scaling.

        // Pre-calculate scaling ratios.
        var x_ratio = (float)srcW / dstW;
        var y_ratio = (float)srcH / dstH;

        // Get direct access to pixel arrays for performance.
        var srcPixels = srcBmp._pixels;
        var srcBmpWidth = srcBmp.Width;
        var srcBmpHeight = srcBmp.Height;

        // Iterate over every pixel in the *clipped* destination rectangle.
        for (var y = clipY1; y < clipY2; y++)
        {
            // For each destination pixel, calculate the corresponding source pixel.
            // dy is the pixel's y-offset from the top of the *original* destination rectangle.
            var dy = y - dstY;
            // Map this offset to the source rectangle's coordinate space.
            var sy = srcY + (int)(dy * y_ratio);

            // --- Source Row Bounds Check ---
            // If the calculated source row is out of bounds, we can skip this entire destination row.
            if (sy < 0 || sy >= srcBmpHeight)
            {
                continue;
            }

            // Pre-calculate destination and source row start indices to optimize the inner loop.
            var dstRowIndex = y * Width;
            var srcRowIndex = sy * srcBmpWidth;

            for (var x = clipX1; x < clipX2; x++)
            {
                // dx is the pixel's x-offset from the left of the *original* destination rectangle.
                var dx = x - dstX;
                // Map this offset to the source rectangle's coordinate space.
                var sx = srcX + (int)(dx * x_ratio);

                // --- Source Column Bounds Check ---
                if (sx >= 0 && sx < srcBmpWidth)
                {
                    // If the source pixel is valid, copy it.
                    var color = srcPixels[srcRowIndex + sx];
                    _pixels[dstRowIndex + x] = color;
                }
            }
        }
    }
}
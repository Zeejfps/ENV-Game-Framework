namespace SoftwareRendererModule;

public static class Graphics
{
    public static void DrawRect(Bitmap bitmap, int x0, int y0, int width, int height, uint color)
    {
        if (bitmap.Width == 0 || bitmap.Height == 0)
            return;
        
        if (width == 0 || height == 0)
            return;
        
        DrawLineH(bitmap, x0, y0, width, color);
        DrawLineV(bitmap, x0, y0, height, color);
        DrawLineV(bitmap, x0 + width-1, y0, height, color);
        DrawLineH(bitmap, x0, y0 + height-1, width, color);
    }

    public static void FillRect(Bitmap bitmap, int x0, int y0, int width, int height, uint color)
    {
        if (bitmap.Width == 0 || bitmap.Height == 0)
            return;
        
        if (width == 0 || height == 0)
            return;
            
        var sx = x0;
        if (sx >= bitmap.Width)
            return;

        if (sx < 0)
            sx = 0;
        
        var ex = x0 + width;
        if (ex < 0)
            return;

        if (ex > bitmap.Width)
            ex = bitmap.Width;

        var sy = y0;
        if (sy >= bitmap.Height)
            return;

        if (sy < 0)
            sy = 0;
        
        var ey = y0 + height;
        if (ey < 0)
            return;

        if (ey > bitmap.Height)
            ey = bitmap.Height;

        var lineLength = ex - sx;
        if (lineLength <= 0)
        {
            return;
        }
        
        for (var y = sy; y < ey; y++)
        {
            bitmap.FillLine(sx, y, lineLength, color);
        }
    }

    private static void DrawLineH(Bitmap bitmap, int x0, int y0, int width, uint color)
    {
        var sx = x0;
        var ex = sx + width;
        var sy = y0;
        var ey = y0;
        
        var isVisible = ClipLine(bitmap, ref sx, ref sy, ref ex, ref ey);
        if (!isVisible)
        {
            // Console.WriteLine($"Clipped: sx: {sx}, ex: {ex}, sy: {sy}, ey: {ey}");
            return;
        }

        bitmap.FillLine(sx, y0, ex - sx, color);
    }

    private static void DrawLineV(Bitmap bitmap, int x0, int y0, int height, uint color)
    {
        var sx = x0;
        var ex = x0;
        var sy = y0;
        var ey = y0 + height;
        
        var isVisible = ClipLine(bitmap, ref sx, ref sy, ref ex, ref ey);
        if (!isVisible)
            return;
        
        for (var y = sy; y < ey; y++)
        {
            bitmap.SetPixel(x0, y, color);
        }
    }

    public static void DrawLine(Bitmap bitmap, int x0, int y0, int x1, int y1, uint color)
    {
        if (y0 == y1)
        {
            var x = x0;
            var width = x1 - x0;
            if (x > x1)
            {
                x = x1;
                width = x0 - x1;
            }
            
            DrawLineH(bitmap, x, y0, width, color);
            return;
        }

        if (x0 == x1)
        {
            var y = y0;
            var height = y1 - y0;
            if (height < 0)
            {
                y = y1;
                height = -height;
            }
            
            DrawLineV(bitmap, x0, y, height, color);
            return;
        }
        
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);

        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;

        var err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < bitmap.Width && y0 >= 0 && y0 < bitmap.Height)
            {
                bitmap.SetPixel(x0, y0, color);
            }

            if (x0 == x1 && y0 == y1) break;

            var e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static bool ClipLine(Bitmap bitmap, ref int sx, ref int sy, ref int ex, ref int ey)
    {
        if (sx >= bitmap.Width)
            return false;
        
        if (sy >= bitmap.Height)
            return false;
        
        if (ex < 0)
            return false;
        
        if (ey < 0)
            return false;
        
        if (sx < 0)
            sx = 0;
        
        if (sy < 0)
            sy = 0;
        
        if (ex > bitmap.Width)
            ex = bitmap.Width;
        
        if (ey > bitmap.Height)
            ey = bitmap.Height;

        return true;
    }

    public static void BlitTransparent(
        Bitmap dstBmp,
        int dstX, int dstY,
        int dstW, int dstH,
        Bitmap srcBmp,
        int srcX, int srcY,
        int srcW, int srcH,
        uint tintColor = 0xFFFFFFFF // Default to white (no tint)
    )
    {
        // --- 1. Initial Checks and Variable Hoisting ---
        if (dstW <= 0 || dstH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return; // Nothing to draw.
        }

        var dstPixels = dstBmp.Pixels;
        int dstBmpWidth = dstBmp.Width;
        int dstBmpHeight = dstBmp.Height;

        var srcPixels = srcBmp.Pixels;
        int srcBmpWidth = srcBmp.Width;
        int srcBmpHeight = srcBmp.Height;

        // --- 2. Clipping ---
        // Calculate the final loop boundaries by clipping the destination rectangle
        // against the destination bitmap's dimensions.
        int loopStartX = Math.Max(0, dstX);
        int loopStartY = Math.Max(0, dstY);
        int loopEndX = Math.Min(dstBmpWidth, dstX + dstW);
        int loopEndY = Math.Min(dstBmpHeight, dstY + dstH);

        // If the clipped rectangle has no area, there's nothing to draw.
        if (loopEndX <= loopStartX || loopEndY <= loopStartY)
        {
            return;
        }

        // --- 3. Pre-calculate Scaling Ratios ---
        // Using floating-point ratios provides more accurate "nearest-neighbor" scaling
        // and avoids integer division issues.
        float x_ratio = (float)srcW / dstW;
        float y_ratio = (float)srcH / dstH;
        bool applyTint = (tintColor != 0xFFFFFFFF); // Check once before the loop

        // --- 4. Main Loop ---
        // Iterate over every pixel in the *clipped* destination area.
        for (int y = loopStartY; y < loopEndY; y++)
        {
            // Calculate the 1D index for the start of the destination row.
            int dstRowIndex = y * dstBmpWidth;

            for (int x = loopStartX; x < loopEndX; x++)
            {
                // --- 5. Map Destination Pixel to Source Pixel (Scaling) ---
                // This maps the destination pixel (x, y) back to a source pixel (sx, sy).
                int sx = srcX + (int)((x - dstX) * x_ratio);
                int sy = srcY + (int)((y - dstY) * y_ratio);

                // --- 6. Source Bounds Check ---
                // Ensure the calculated source pixel is within the source bitmap's bounds.
                // This is a critical check.
                if (sx < 0 || sx >= srcBmpWidth || sy < 0 || sy >= srcBmpHeight)
                {
                    continue; // This source pixel is outside the source bitmap.
                }

                // --- 7. Blending ---
                // Calculate the 1D indices for source and destination pixels.
                var srcIndex = sy * srcBmpWidth + sx;
                var dstIndex = dstRowIndex + x;

                var srcColor = srcPixels[srcIndex];

                if (applyTint)
                {
                    srcColor = TintPixel(srcColor, tintColor);
                }

                var dstColor = dstPixels[dstIndex];
                dstPixels[dstIndex] = BlendPixel(dstColor, srcColor);
            }
        }
    }

    public static uint TintPixel(uint source, uint tint)
    {
        // Extract alpha from the source, as it determines the final transparency
        uint src_a = source >> 24;

        // If the source is fully transparent, tinting has no effect.
        if (src_a == 0) return 0;

        // Extract RGB components from both colors
        uint src_r = (source >> 16) & 0xFF;
        uint src_g = (source >> 8) & 0xFF;
        uint src_b = source & 0xFF;

        uint tint_r = (tint >> 16) & 0xFF;
        uint tint_g = (tint >> 8) & 0xFF;
        uint tint_b = tint & 0xFF;

        // Modulate (multiply) the components and scale back to 0-255 range
        uint final_r = (src_r * tint_r) / 255;
        uint final_g = (src_g * tint_g) / 255;
        uint final_b = (src_b * tint_b) / 255;

        // Recombine into the final ARGB color
        return (src_a << 24) | (final_r << 16) | (final_g << 8) | final_b;
    }

    public static uint BlendPixel(uint dstColor, uint srcColor)
    {
        var srcA = (byte)((srcColor >> 24) & 0xFF);
        if (srcA == 0)
            return dstColor;

        if (srcA == 255)
            return srcColor;

        var srcR = (byte)((srcColor >> 16) & 0xFF);
        var srcG = (byte)((srcColor >> 8) & 0xFF);
        var srcB = (byte)((srcColor) & 0xFF);

        var dstR = (byte)((dstColor >> 16) & 0xFF);
        var dstG = (byte)((dstColor >> 8) & 0xFF);
        var dstB = (byte)((dstColor) & 0xFF);

        var outR = (byte)(((srcR * srcA) + (dstR * (255 - srcA)) + 128) / 255);
        var outG = (byte)(((srcG * srcA) + (dstG * (255 - srcA)) + 128) / 255);
        var outB = (byte)(((srcB * srcA) + (dstB * (255 - srcA)) + 128) / 255);

        // Combine the alpha channels correctly
        var dstA = (dstColor >> 24) & 0xFF;
        var outA = (byte)(((srcA * 255) + (dstA * (255 - srcA)) + 128) / 255);

        // Pack back into an ARGB uint
        return ((uint)outA << 24) | ((uint)outR << 16) | ((uint)outG << 8) | outB;
    }
}
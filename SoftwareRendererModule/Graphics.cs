using System.Runtime.CompilerServices;
using ZGF.Geometry;

namespace SoftwareRendererModule;

public static class Graphics
{
    public static void DrawRect(Bitmap bitmap, int x0, int y0, int width, int height, uint color)
    {
        if (bitmap.Width == 0 || bitmap.Height == 0)
            return;
        
        if (width == 0 || height == 0)
            return;
        
        var clip = new RectF(0, 0, bitmap.Width, bitmap.Height);;
        DrawLineH(bitmap, x0, y0, width, color, clip);
        DrawLineV(bitmap, x0, y0, height, color, clip);
        DrawLineV(bitmap, x0 + width-1, y0, height, color, clip);
        DrawLineH(bitmap, x0, y0 + height-1, width, color, clip);
    }

    public static void FillRect(Bitmap bitmap, int x0, int y0, int width, int height, uint color, in RectF clip)
    {
        if (bitmap.Width == 0 || bitmap.Height == 0)
            return;
        
        if (width == 0 || height == 0)
            return;
            
        var sx = x0;
        if (sx >= clip.Right)
            return;

        if (sx < clip.Left)
            sx = (int)clip.Left;
        
        var ex = x0 + width;
        if (ex < clip.Left)
            return;

        if (ex > clip.Right)
            ex = (int)clip.Right;

        var sy = y0;
        if (sy >= clip.Top)
            return;

        if (sy < clip.Bottom)
            sy = (int)clip.Bottom;
        
        var ey = y0 + height;
        if (ey < clip.Bottom)
            return;

        if (ey > clip.Top)
            ey = (int)clip.Top;

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

    private static void DrawLineH(Bitmap bitmap, int x0, int y0, int width, uint color, RectF clip)
    {
        var sx = x0;
        var ex = sx + width;
        var sy = y0;
        var ey = y0;
        
        var isVisible = ClipLine(clip, ref sx, ref sy, ref ex, ref ey);
        if (!isVisible)
        {
            // Console.WriteLine($"Clipped: sx: {sx}, ex: {ex}, sy: {sy}, ey: {ey}");
            return;
        }

        bitmap.FillLine(sx, y0, ex - sx, color);
    }

    private static void DrawLineV(Bitmap bitmap, int x0, int y0, int height, uint color, RectF clip)
    {
        var sx = x0;
        var ex = x0;
        var sy = y0;
        var ey = y0 + height;
        
        var isVisible = ClipLine(clip, ref sx, ref sy, ref ex, ref ey);
        if (!isVisible)
            return;
        
        for (var y = sy; y < ey; y++)
        {
            bitmap.SetPixel(x0, y, color);
        }
    }

    public static void DrawLine(Bitmap bitmap, int x0, int y0, int x1, int y1, uint color, RectF clip)
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
            
            DrawLineH(bitmap, x, y0, width, color, clip);
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
            
            DrawLineV(bitmap, x0, y, height, color, clip);
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

    private static bool ClipLine(RectF clip, ref int sx, ref int sy, ref int ex, ref int ey)
    {
        if (sx >= clip.Right)
            return false;
        
        if (sy >= clip.Top)
            return false;
        
        if (ex < clip.Left)
            return false;
        
        if (ey < clip.Bottom)
            return false;
        
        if (sx < clip.Left)
            sx = (int)clip.Left;
        
        if (sy < clip.Bottom)
            sy = (int)clip.Bottom;
        
        if (ex > clip.Right)
            ex = (int)clip.Right;
        
        if (ey > clip.Top)
            ey = (int)clip.Top;

        return true;
    }

    public static unsafe void BlitTransparent(
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
        if (dstW <= 0 || dstH <= 0 || srcW <= 0 || srcH <= 0) return;

        int dstBmpWidth = dstBmp.Width;
        int dstBmpHeight = dstBmp.Height;
        int srcBmpWidth = srcBmp.Width;
        int srcBmpHeight = srcBmp.Height;

        // --- 2. Clipping ---
        int loopStartX = Math.Max(0, dstX);
        int loopStartY = Math.Max(0, dstY);
        int loopEndX = Math.Min(dstBmpWidth, dstX + dstW);
        int loopEndY = Math.Min(dstBmpHeight, dstY + dstH);

        if (loopEndX <= loopStartX || loopEndY <= loopStartY) return;
        
        // --- 3. Pre-calculate Ratios and Tint Flag ---
        float x_ratio = (float)srcW / dstW;
        float y_ratio = (float)srcH / dstH;
        var applyTint = (tintColor != 0xFFFFFFFF);

        // --- 4. Pin Memory and Get Pointers ---
        // The 'fixed' keyword "pins" the arrays in memory, preventing the Garbage Collector
        // from moving them. This gives us a stable memory address (pointer) to work with.
        // This is the core of the 'unsafe' optimization.
        fixed (uint* srcStartPtr = srcBmp.Pixels)
        fixed (uint* dstStartPtr = dstBmp.Pixels)
        {
            // --- 5. Main Loop (Optimized) ---
            for (int y = loopStartY; y < loopEndY; y++)
            {
                // --- 6. Pre-calculate Source Y and Destination Row Start ---
                // Calculate the source 'sy' coordinate once per row.
                int sy = srcY + (int)((y - dstY) * y_ratio);

                // OPTIMIZATION: Get a pointer to the start of the current destination row.
                // This avoids a 'y * width' multiplication in the inner loop.
                uint* dstRowPtr = dstStartPtr + (y * dstBmpWidth) + loopStartX;

                // CRITICAL: Source bounds check for the entire row.
                // If the calculated 'sy' is outside the source bitmap, we can skip the whole row.
                if (sy < 0 || sy >= srcBmpHeight)
                {
                    continue;
                }
                
                // Get a pointer to the start of the source row.
                uint* srcRowPtr = srcStartPtr + sy * srcBmpWidth;

                // --- 7. Inner Loop (Optimized with Pointers) ---
                for (int x = loopStartX; x < loopEndX; x++)
                {
                    // Map destination x to source x
                    int sx = srcX + (int)((x - dstX) * x_ratio);

                    // We still need to check the source 'sx' bound inside the inner loop,
                    // as 'x_ratio' can cause 'sx' to be out of bounds even if 'x' is valid.
                    if (sx >= 0 && sx < srcBmpWidth)
                    {
                        // Read source pixel using pointer arithmetic (no bounds check).
                        uint srcColor = *(srcRowPtr + sx);

                        // Skip fully transparent pixels (common optimization).
                        if ((srcColor >> 24) == 0)
                        {
                            dstRowPtr++; // Still need to advance the destination pointer
                            continue;
                        }
                        
                        if (applyTint)
                        {
                            srcColor = TintPixel(srcColor, tintColor);
                        }

                        // Read destination pixel, blend, and write back using pointers.
                        uint dstColor = *dstRowPtr;
                        *dstRowPtr = BlendPixel(dstColor, srcColor);
                    }
                    
                    // Advance the destination pointer to the next pixel in the row.
                    dstRowPtr++;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint TintPixel(uint source, uint tint)
    {
        // Source alpha is preserved. If fully transparent, the result is black transparent.
        var src_a = source >> 24;
        if (src_a == 0) return 0;

        // Extract tint components once
        var tint_r = (tint >> 16) & 0xFF;
        var tint_g = (tint >> 8) & 0xFF;
        var tint_b = tint & 0xFF;

        // Extract source components
        var src_r = (source >> 16) & 0xFF;
        var src_g = (source >> 8) & 0xFF;
        var src_b = source & 0xFF;
        
        // This is the optimized multiplication/division part
        // It's an exact and fast replacement for (x * y) / 255
        var final_r = FastMultiply(src_r, tint_r);
        var final_g = FastMultiply(src_g, tint_g);
        var final_b = FastMultiply(src_b, tint_b);

        return (src_a << 24) | (final_r << 16) | (final_g << 8) | final_b;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint FastMultiply(uint c1, uint c2)
    {
        // This is a common and highly efficient way to compute (c1 * c2) / 255
        uint temp = c1 * c2 + 128;
        return (temp + (temp >> 8)) >> 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint BlendPixel(uint dstColor, uint srcColor)
    {
        var src_a = (srcColor >> 24);

        // Fast path for common cases
        if (src_a == 0) return dstColor;
        if (src_a == 255) return srcColor;

        var dst_a = (dstColor >> 24);

        // Pre-calculate the inverse alpha
        var inv_a = 255 - src_a;

        // This is the core of the fast division trick.
        // We calculate `(a * b + 128)` in the original, so we just need to add the rest.
        // `out = (t + (t >> 8)) >> 8` is equivalent to `(t + 128) / 255` for `t=a*b`.
        // The `+128` is already in your formula for rounding, so we use `(val + (val >> 8)) >> 8`.
        var src_r = (srcColor >> 16) & 0xFF;
        var src_g = (srcColor >> 8) & 0xFF;
        var src_b = srcColor & 0xFF;

        var dst_r = (dstColor >> 16) & 0xFF;
        var dst_g = (dstColor >> 8) & 0xFF;
        var dst_b = dstColor & 0xFF;

        // Blend R, G, B channels
        var t = (src_r * src_a) + (dst_r * inv_a);
        var out_r = (t + (t >> 8)) >> 8;

        t = (src_g * src_a) + (dst_g * inv_a);
        var out_g = (t + (t >> 8)) >> 8;
        
        t = (src_b * src_a) + (dst_b * inv_a);
        var out_b = (t + (t >> 8)) >> 8;

        // Blend Alpha channel
        t = (src_a * 255) + (dst_a * inv_a);
        var out_a = (t + (t >> 8)) >> 8;
        
        return (out_a << 24) | (out_r << 16) | (out_g << 8) | out_b;
    }
}
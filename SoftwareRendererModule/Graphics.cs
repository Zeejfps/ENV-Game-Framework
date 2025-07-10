namespace SoftwareRendererModule;

public static class Graphics
{
    public static void FillRect(Bitmap bitmap, int x0, int y0, int width, int height, int color)
    {
        var sx = x0;
        if (sx < 0)
            sx = 0;
        
        var ex = x0 + width;
        if (ex > bitmap.Width)
            ex = bitmap.Width;

        var sy = y0;
        if (sy < 0)
            sy = 0;
        
        var ey = y0 + height;
        if (ey > bitmap.Height)
            ey = bitmap.Height;

        var lineLength = ex - sx;
        for (var y = sy; y < ey; y++)
        {
            bitmap.FillLine(sx, y, lineLength, color);
        }
    }
    
    public static void DrawLineH(Bitmap bitmap, int x0, int y1, int width, int color)
    {
        var sx = x0;
        if (sx < 0)
            sx = 0;
        
        var ex = x0 + width;
        if (ex > bitmap.Width)
            ex = bitmap.Width;

        bitmap.FillLine(sx, y1, ex - sx, color);
    }
    
    public static void DrawLineV(Bitmap bitmap, int x0, int y0, int height, int color)
    {
        var sy = y0;
        if (sy < 0)
            sy = 0;
        
        var ey = y0 + height;
        if (ey > bitmap.Height)
            ey = bitmap.Height;

        for (var y = sy; y < ey; y++)
        {
            bitmap.SetPixel(x0, y, color);
        }
    }
    
    public static void DrawLine(Bitmap bitmap, int x0, int y0, int x1, int y1, int color)
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
}
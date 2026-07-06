using System.Numerics;

namespace ZGF.Svg.Raster;

/// <summary>
/// Analytic-coverage scanline rasterizer in the signed-area accumulation style
/// (FreeType-smooth / font-rs family). Each edge deposits per-pixel signed area
/// deltas; a left-to-right prefix sum per row recovers the winding number, from
/// which either fill rule derives exact 8-bit coverage — no supersampling.
/// </summary>
internal sealed class CellRasterizer
{
    private float[] _area = [];
    private int _w;
    private int _h;
    private int _stride;   // w + 2: edge splitting may touch one column past the right edge
    private int _dirtyMinY;
    private int _dirtyMaxY; // exclusive

    public void Begin(int w, int h)
    {
        _w = w;
        _h = h;
        _stride = w + 2;
        var needed = _stride * h;
        if (_area.Length < needed)
        {
            _area = new float[needed];
        }
        else
        {
            // Buffer is kept zeroed between shapes by ResetDirty.
        }
        _dirtyMinY = h;
        _dirtyMaxY = 0;
    }

    public void AddContour(ReadOnlySpan<Vector2> points, bool close)
    {
        if (points.Length < 2)
            return;
        for (var i = 1; i < points.Length; i++)
            AddLine(points[i - 1], points[i]);
        if (close && points[^1] != points[0])
            AddLine(points[^1], points[0]);
    }

    /// <summary>Adds a closed polygon, reversed if needed so all pieces share one winding direction.</summary>
    public void AddPolygonConsistent(ReadOnlySpan<Vector2> points)
    {
        if (points.Length < 3)
            return;

        var area2 = 0f;
        var prev = points[^1];
        foreach (var p in points)
        {
            area2 += prev.X * p.Y - p.X * prev.Y;
            prev = p;
        }

        if (area2 >= 0f)
        {
            for (var i = 1; i < points.Length; i++)
                AddLine(points[i - 1], points[i]);
            AddLine(points[^1], points[0]);
        }
        else
        {
            for (var i = points.Length - 1; i > 0; i--)
                AddLine(points[i], points[i - 1]);
            AddLine(points[0], points[^1]);
        }
    }

    public void AddLine(Vector2 p0, Vector2 p1)
    {
        if (p0.Y == p1.Y)
            return;

        float dir;
        if (p0.Y > p1.Y)
        {
            (p0, p1) = (p1, p0);
            dir = -1f;
        }
        else
        {
            dir = 1f;
        }

        if (p1.Y <= 0f || p0.Y >= _h)
            return;

        var dxdy = (p1.X - p0.X) / (p1.Y - p0.Y);
        var x = p0.X;
        if (p0.Y < 0f)
        {
            x -= p0.Y * dxdy;
            p0 = new Vector2(x, 0f);
        }

        var yEnd = MathF.Min(p1.Y, _h);
        var y0 = (int)p0.Y;
        var y1 = (int)MathF.Ceiling(yEnd);
        _dirtyMinY = Math.Min(_dirtyMinY, y0);
        _dirtyMaxY = Math.Max(_dirtyMaxY, y1);

        var area = _area.AsSpan();
        for (var y = y0; y < y1; y++)
        {
            var rowStart = y * _stride;
            var dy = MathF.Min(y + 1, yEnd) - MathF.Max(y, p0.Y);
            var xNext = x + dxdy * dy;
            var d = dy * dir;

            var (x0, x1) = x < xNext ? (x, xNext) : (xNext, x);
            x = xNext;

            // Winding left of the canvas affects every visible pixel: pin to column 0.
            // Winding right of the canvas affects nothing: skip.
            x0 = Math.Clamp(x0, 0f, _w);
            x1 = Math.Clamp(x1, 0f, _w);
            var x0Floor = MathF.Floor(x0);
            var x0i = (int)x0Floor;
            if (x0i >= _w && x1 >= _w)
                continue;

            var x1Ceil = MathF.Ceiling(x1);
            var x1i = (int)x1Ceil;
            if (x1i <= x0i + 1)
            {
                // The edge crosses this scanline within a single pixel column.
                var xMidFrac = 0.5f * (x0 + x1) - x0Floor;
                area[rowStart + x0i] += d - d * xMidFrac;
                area[rowStart + x0i + 1] += d * xMidFrac;
            }
            else
            {
                var s = 1f / (x1 - x0);
                var x0Frac = x0 - x0Floor;
                var a0 = 0.5f * s * (1f - x0Frac) * (1f - x0Frac);
                var x1Frac = x1 - x1Ceil + 1f;
                var aLast = 0.5f * s * x1Frac * x1Frac;
                area[rowStart + x0i] += d * a0;
                if (x1i == x0i + 2)
                {
                    area[rowStart + x0i + 1] += d * (1f - a0 - aLast);
                }
                else
                {
                    var a1 = s * (1.5f - x0Frac);
                    area[rowStart + x0i + 1] += d * (a1 - a0);
                    for (var xi = x0i + 2; xi < x1i - 1; xi++)
                        area[rowStart + xi] += d * s;
                    var a2 = a1 + (x1i - x0i - 3) * s;
                    area[rowStart + x1i - 1] += d * (1f - a2 - aLast);
                }
                area[rowStart + x1i] += d * aLast;
            }
        }
    }

    /// <summary>Sweeps accumulated coverage, blends the color, and re-zeroes the touched region.</summary>
    public void Fill(SvgFillRule rule, uint colorArgb, PixelBlender blender)
    {
        var yStart = Math.Max(0, _dirtyMinY);
        var yEnd = Math.Min(_h, _dirtyMaxY);
        var area = _area.AsSpan();

        for (var y = yStart; y < yEnd; y++)
        {
            var row = area.Slice(y * _stride, _stride);
            var acc = 0f;
            for (var xi = 0; xi < _w; xi++)
            {
                acc += row[xi];
                float coverage;
                if (rule == SvgFillRule.NonZero)
                {
                    coverage = MathF.Min(MathF.Abs(acc), 1f);
                }
                else
                {
                    var t = acc - 2f * MathF.Floor(acc * 0.5f);  // wrap to [0, 2)
                    coverage = 1f - MathF.Abs(t - 1f);
                }

                var c8 = (int)(coverage * 255f + 0.5f);
                if (c8 > 0)
                    blender.BlendPixel(xi, y, colorArgb, (byte)Math.Min(c8, 255));
            }
            row.Clear();
        }

        _dirtyMinY = _h;
        _dirtyMaxY = 0;
    }
}

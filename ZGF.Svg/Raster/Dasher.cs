using System.Numerics;

namespace ZGF.Svg.Raster;

/// <summary>
/// Splits a flattened contour into "on" runs by arc length and strokes each run
/// as an open polyline with the command's caps.
/// </summary>
internal sealed class Dasher
{
    private Vector2[] _run = new Vector2[64];
    private int _runCount;

    public void DashAndStroke(
        ReadOnlySpan<Vector2> points,
        bool closed,
        ReadOnlySpan<float> pattern,
        float offset,
        float halfWidth,
        SvgLineCap cap,
        SvgLineJoin join,
        float miterLimit,
        Stroker stroker,
        CellRasterizer raster)
    {
        if (points.Length < 2)
        {
            stroker.StrokeContour(points, closed: false, halfWidth, cap, join, miterLimit, raster);
            return;
        }

        var total = 0f;
        foreach (var d in pattern)
            total += d;
        if (total <= 0f)
        {
            stroker.StrokeContour(points, closed, halfWidth, cap, join, miterLimit, raster);
            return;
        }

        // Resolve the starting position in the pattern from the (possibly negative) offset.
        var phase = offset % total;
        if (phase < 0f)
            phase += total;
        var patternIndex = 0;
        var remaining = pattern[0];
        while (phase > 0f)
        {
            if (phase < remaining)
            {
                remaining -= phase;
                break;
            }
            phase -= remaining;
            patternIndex = (patternIndex + 1) % pattern.Length;
            remaining = pattern[patternIndex];
        }

        var on = patternIndex % 2 == 0;
        _runCount = 0;
        if (on)
            AddRunPoint(points[0]);

        var segmentEnd = closed ? points.Length : points.Length - 1;
        for (var i = 0; i < segmentEnd; i++)
        {
            var p0 = points[i];
            var p1 = points[(i + 1) % points.Length];
            var segVec = p1 - p0;
            var segLen = segVec.Length();
            if (segLen == 0f)
                continue;
            var dir = segVec / segLen;

            var traveled = 0f;
            while (segLen - traveled > remaining)
            {
                traveled += remaining;
                var split = p0 + dir * traveled;
                if (on)
                {
                    AddRunPoint(split);
                    FlushRun(stroker, halfWidth, cap, join, miterLimit, raster);
                }
                else
                {
                    AddRunPoint(split);
                }
                on = !on;
                patternIndex = (patternIndex + 1) % pattern.Length;
                remaining = pattern[patternIndex];
                if (!on)
                    _runCount = 0;
            }
            remaining -= segLen - traveled;
            if (on)
                AddRunPoint(p1);
        }

        FlushRun(stroker, halfWidth, cap, join, miterLimit, raster);
    }

    private void AddRunPoint(Vector2 p)
    {
        if (_runCount > 0 && _run[_runCount - 1] == p)
            return;
        if (_runCount == _run.Length)
            Array.Resize(ref _run, _runCount * 2);
        _run[_runCount++] = p;
    }

    private void FlushRun(Stroker stroker, float halfWidth, SvgLineCap cap, SvgLineJoin join, float miterLimit, CellRasterizer raster)
    {
        if (_runCount >= 2)
        {
            stroker.StrokeContour(_run.AsSpan(0, _runCount), closed: false, halfWidth, cap, join, miterLimit, raster);
        }
        else if (_runCount == 1 && cap != SvgLineCap.Butt)
        {
            // A zero-length dash still paints with round/square caps.
            stroker.StrokeContour(_run.AsSpan(0, 1), closed: false, halfWidth, cap, join, miterLimit, raster);
        }
        _runCount = 0;
    }
}

using System.Numerics;

namespace ZGF.Svg.Raster;

/// <summary>
/// Pooled storage for flattened contours: a shared point buffer plus per-contour
/// ranges. Reused across rasterize calls — steady state allocates nothing.
/// </summary>
internal sealed class PathBuffer
{
    private Vector2[] _points = new Vector2[256];
    private int[] _contourStart = new int[16];
    private bool[] _contourClosed = new bool[16];
    private int _pointCount;
    private int _contourCount;

    public int ContourCount => _contourCount;
    public ReadOnlySpan<Vector2> Points => _points.AsSpan(0, _pointCount);

    public void Clear()
    {
        _pointCount = 0;
        _contourCount = 0;
    }

    public void BeginContour()
    {
        TrimEmptyTail();
        if (_contourCount == _contourStart.Length)
        {
            Array.Resize(ref _contourStart, _contourCount * 2);
            Array.Resize(ref _contourClosed, _contourCount * 2);
        }
        _contourStart[_contourCount] = _pointCount;
        _contourClosed[_contourCount] = false;
        _contourCount++;
    }

    public void Add(Vector2 p)
    {
        if (_contourCount == 0)
            BeginContour();
        if (_pointCount == _points.Length)
            Array.Resize(ref _points, _pointCount * 2);
        _points[_pointCount++] = p;
    }

    public void CloseContour()
    {
        if (_contourCount > 0)
            _contourClosed[_contourCount - 1] = true;
    }

    public void EndPath()
    {
        TrimEmptyTail();
    }

    public ReadOnlySpan<Vector2> GetContour(int index, out bool closed)
    {
        var start = _contourStart[index];
        var end = index + 1 < _contourCount ? _contourStart[index + 1] : _pointCount;
        closed = _contourClosed[index];
        return _points.AsSpan(start, end - start);
    }

    private void TrimEmptyTail()
    {
        // Drop a trailing contour that never received points (e.g. Move immediately
        // followed by another Move or by the end of the path).
        if (_contourCount > 0 && _contourStart[_contourCount - 1] == _pointCount)
            _contourCount--;
    }
}

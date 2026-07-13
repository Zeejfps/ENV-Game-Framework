namespace ZGF.Fonts;

internal sealed class GlyphAtlas
{
    private byte[] _pixels;
    private readonly int _width;
    private int _height;
    private readonly int _maxHeight;
    private int _version;
    private bool _exhausted;

    private readonly List<SkylineNode> _skyline = new();

    private int _dirtyMinX;
    private int _dirtyMinY;
    private int _dirtyMaxX;
    private int _dirtyMaxY;
    private bool _dirty;

    private const int Padding = 1;

    public GlyphAtlas(int width, int height, int maxHeight = 8192)
    {
        _width = width;
        _height = height;
        _maxHeight = Math.Max(height, maxHeight);
        _pixels = new byte[width * height];

        _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = width });
    }

    public int Width => _width;
    public int Height => _height;
    public ReadOnlySpan<byte> Pixels => _pixels;

    /// <summary>Bumped whenever the atlas grows. A renderer holding a texture of the old size
    /// compares this against the version it uploaded and reallocates when they differ.</summary>
    public int Version => _version;

    /// <summary>Raised the first time a glyph cannot be placed even at the maximum size — the atlas
    /// is out of room and glyphs are being dropped. Silence here renders as tofu, which reads as a
    /// font-coverage bug rather than an atlas one.</summary>
    public event Action? Exhausted;

    public bool IsExhausted => _exhausted;

    public bool Dirty => _dirty;

    public AtlasDirtyRect DirtyRect => _dirty
        ? new AtlasDirtyRect(_dirtyMinX, _dirtyMinY, _dirtyMaxX - _dirtyMinX, _dirtyMaxY - _dirtyMinY)
        : default;

    public void ClearDirty()
    {
        _dirty = false;
        _dirtyMinX = int.MaxValue;
        _dirtyMinY = int.MaxValue;
        _dirtyMaxX = int.MinValue;
        _dirtyMaxY = int.MinValue;
    }

    /// <summary>Reserves space for a glyph, growing the atlas when it no longer fits. Only fails once
    /// the atlas is at its maximum size — and then it says so through <see cref="Exhausted"/>.</summary>
    public bool TryReserve(int w, int h, out int outX, out int outY)
    {
        outX = 0;
        outY = 0;

        // Growth adds rows, so a glyph wider than the atlas will never fit however tall it gets.
        if (w + Padding * 2 > _width)
        {
            SignalExhausted();
            return false;
        }

        while (true)
        {
            if (TryReserveInPlace(w, h, out outX, out outY))
                return true;

            if (TryGrow())
                continue;

            SignalExhausted();
            return false;
        }
    }

    private void SignalExhausted()
    {
        if (_exhausted)
            return;

        _exhausted = true;
        Exhausted?.Invoke();
    }

    // Height doubles; the width is fixed, so a row's offset into the pixel buffer (y * width + x)
    // is unchanged by the resize and both the existing pixels and the skyline stay valid.
    private bool TryGrow()
    {
        if (_height >= _maxHeight)
            return false;

        _height = Math.Min(_height * 2, _maxHeight);
        Array.Resize(ref _pixels, _width * _height);
        _version++;
        return true;
    }

    private bool TryReserveInPlace(int w, int h, out int outX, out int outY)
    {
        outX = 0;
        outY = 0;

        var paddedW = w + Padding * 2;
        var paddedH = h + Padding * 2;

        if (paddedW > _width || paddedH > _height)
            return false;

        var bestIndex = -1;
        var bestY = int.MaxValue;
        var bestX = 0;

        for (var i = 0; i < _skyline.Count; i++)
        {
            if (!FitsAt(i, paddedW, out var y))
                continue;

            if (y + paddedH > _height)
                continue;

            if (y < bestY || (y == bestY && _skyline[i].X < bestX))
            {
                bestIndex = i;
                bestY = y;
                bestX = _skyline[i].X;
            }
        }

        if (bestIndex < 0)
            return false;

        AddSkylineLevel(bestIndex, bestX, bestY, paddedW, paddedH);

        outX = bestX + Padding;
        outY = bestY + Padding;
        return true;
    }

    public unsafe void Blit(int x, int y, int width, int height, byte* src, int srcPitch)
    {
        if (width <= 0 || height <= 0)
            return;

        for (var row = 0; row < height; row++)
        {
            var srcOffset = row * srcPitch;
            var dstOffset = (y + row) * _width + x;
            for (var col = 0; col < width; col++)
                _pixels[dstOffset + col] = src[srcOffset + col];
        }

        MarkDirty(x, y, x + width, y + height);
    }

    private void MarkDirty(int minX, int minY, int maxX, int maxY)
    {
        if (!_dirty)
        {
            _dirtyMinX = minX;
            _dirtyMinY = minY;
            _dirtyMaxX = maxX;
            _dirtyMaxY = maxY;
            _dirty = true;
            return;
        }

        if (minX < _dirtyMinX) _dirtyMinX = minX;
        if (minY < _dirtyMinY) _dirtyMinY = minY;
        if (maxX > _dirtyMaxX) _dirtyMaxX = maxX;
        if (maxY > _dirtyMaxY) _dirtyMaxY = maxY;
    }

    private bool FitsAt(int startIndex, int width, out int y)
    {
        var x = _skyline[startIndex].X;
        if (x + width > _width)
        {
            y = 0;
            return false;
        }

        var remaining = width;
        var i = startIndex;
        y = _skyline[i].Y;

        while (remaining > 0)
        {
            if (i >= _skyline.Count)
                return false;

            if (_skyline[i].Y > y)
                y = _skyline[i].Y;

            if (y + 1 > _height)
                return false;

            remaining -= _skyline[i].Width;
            i++;
        }

        return true;
    }

    private void AddSkylineLevel(int index, int x, int y, int width, int height)
    {
        var newNode = new SkylineNode { X = x, Y = y + height, Width = width };
        _skyline.Insert(index, newNode);

        for (var i = index + 1; i < _skyline.Count; i++)
        {
            var node = _skyline[i];
            var prev = _skyline[i - 1];
            if (node.X < prev.X + prev.Width)
            {
                var shrink = prev.X + prev.Width - node.X;
                node.X += shrink;
                node.Width -= shrink;
                if (node.Width <= 0)
                {
                    _skyline.RemoveAt(i);
                    i--;
                    continue;
                }
                _skyline[i] = node;
            }
            break;
        }

        for (var i = 0; i < _skyline.Count - 1; i++)
        {
            var cur = _skyline[i];
            var next = _skyline[i + 1];
            if (cur.Y == next.Y)
            {
                cur.Width += next.Width;
                _skyline[i] = cur;
                _skyline.RemoveAt(i + 1);
                i--;
            }
        }
    }

    private struct SkylineNode
    {
        public int X;
        public int Y;
        public int Width;
    }
}

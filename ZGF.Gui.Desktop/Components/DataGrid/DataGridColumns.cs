using ZGF.Geometry;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// The shared, mutable column geometry for a data grid: the single source of truth that the header, every
/// row (preview and editable), hit-testing, and the resize controller all resolve through, so the parts can
/// never drift out of alignment. Generalizes the ledger's bespoke column layout to N columns of mixed
/// <see cref="ColumnWidth"/> kinds. Columns lay out left-to-right inset by <see cref="EdgePadding"/> at each
/// end, separated by <see cref="Gap"/>; <see cref="ColumnWidthKind.Flex"/> columns split the leftover width.
/// </summary>
public sealed class DataGridColumns
{
    private readonly ColumnWidth[] _widths;
    private readonly ResizableExtent?[] _resizable;

    public DataGridColumns(IReadOnlyList<ColumnWidth> widths, float gap = 16f, float edgePadding = 14f)
    {
        Gap = gap;
        EdgePadding = edgePadding;
        _widths = new ColumnWidth[widths.Count];
        _resizable = new ResizableExtent?[widths.Count];
        for (var i = 0; i < widths.Count; i++)
        {
            var w = widths[i];
            _widths[i] = w;
            if (w.Kind == ColumnWidthKind.Resizable)
            {
                var extent = new ResizableExtent(w.Value, w.Min, w.Max);
                extent.Changed += Fire;
                _resizable[i] = extent;
            }
        }
    }

    public float Gap { get; }
    public float EdgePadding { get; }
    public int Count => _widths.Length;

    /// <summary>Raised whenever a resizable column's width actually changes.</summary>
    public event Action? Changed;

    private void Fire() => Changed?.Invoke();

    public ColumnWidthKind KindOf(int column) => _widths[column].Kind;

    /// <summary>The current resolved-independent width of a column: the fixed/resizable pixels, or the raw
    /// flex weight (flex columns only get a real pixel width from <see cref="Resolve"/>, which knows the row).</summary>
    public float CurrentWidth(int column) => _resizable[column]?.Value ?? _widths[column].Value;

    /// <summary>Sets a resizable column's width to the clamped <paramref name="rawWidth"/>, returning whether
    /// it moved. No-op (returns false) for fixed and flex columns. <paramref name="maxOverride"/> caps this
    /// call against a live neighbour.</summary>
    public bool SetWidth(int column, float rawWidth, float? maxOverride = null) =>
        _resizable[column]?.Set(rawWidth, maxOverride) ?? false;

    /// <summary>Fills <paramref name="cells"/> (length must be at least <see cref="Count"/>) with each
    /// column's on-screen rect for the given row band. Zero-alloc, suitable for the per-row hot path.</summary>
    public void Resolve(RectF rowRect, Span<RectF> cells)
    {
        var n = _widths.Length;
        var bottom = rowRect.Bottom;
        var height = rowRect.Height;
        var innerLeft = rowRect.Left + EdgePadding;
        var innerRight = rowRect.Right - EdgePadding;
        var totalGap = Gap * Math.Max(0, n - 1);

        var fixedSum = 0f;
        var flexWeight = 0f;
        for (var i = 0; i < n; i++)
        {
            if (_widths[i].Kind == ColumnWidthKind.Flex) flexWeight += _widths[i].Value;
            else fixedSum += CurrentWidth(i);
        }

        var slack = Math.Max(0f, innerRight - innerLeft - totalGap - fixedSum);

        var x = innerLeft;
        for (var i = 0; i < n; i++)
        {
            float w;
            if (_widths[i].Kind == ColumnWidthKind.Flex)
                w = flexWeight > 0f ? slack * (_widths[i].Value / flexWeight) : 0f;
            else
                w = CurrentWidth(i);

            cells[i] = new RectF(x, bottom, w, height);
            x += w + Gap;
        }
    }

    /// <summary>The column index at <paramref name="x"/> for the given row band. A point in the gap after a
    /// column folds into that (left) column; left of the first column returns 0; -1 only when there are no
    /// columns.</summary>
    public int HitTest(RectF rowRect, float x)
    {
        var n = _widths.Length;
        if (n == 0) return -1;

        Span<RectF> cells = stackalloc RectF[n];
        Resolve(rowRect, cells);

        var result = 0;
        for (var i = 0; i < n; i++)
            if (cells[i].Left <= x)
                result = i;
        return result;
    }

    /// <summary>The x of the draggable divider in the gap to the right of <paramref name="leftColumn"/>,
    /// placed at the gap's centre.</summary>
    public float BoundaryX(int leftColumn, RectF rowRect)
    {
        Span<RectF> cells = stackalloc RectF[_widths.Length];
        Resolve(rowRect, cells);
        return cells[leftColumn].Right + Gap * 0.5f;
    }

    /// <summary>The widest a resizable column may grow before the flex columns would shrink past
    /// <paramref name="minFlex"/> — the ceiling to pass to <see cref="SetWidth"/> when dragging. With no flex
    /// column the column keeps its own <see cref="ColumnWidth.Max"/>.</summary>
    public float MaxResizableWidth(int column, RectF rowRect, float minFlex)
    {
        Span<RectF> cells = stackalloc RectF[_widths.Length];
        Resolve(rowRect, cells);

        var flexTotal = 0f;
        var hasFlex = false;
        for (var i = 0; i < _widths.Length; i++)
            if (_widths[i].Kind == ColumnWidthKind.Flex) { flexTotal += cells[i].Width; hasFlex = true; }

        if (!hasFlex) return _widths[column].Max;
        return CurrentWidth(column) + Math.Max(0f, flexTotal - minFlex);
    }
}

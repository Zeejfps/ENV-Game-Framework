using ZGF.Geometry;

namespace ZGF.Gui.Desktop.Components.VirtualWidgetList;

/// <summary>
/// A vertically-scrolling, virtualized list whose rows are real <see cref="View"/> widgets.
///
/// Where <see cref="VirtualRowList.VirtualRowListView"/> paints each row immediately into a canvas
/// (and the consumer hand-hit-tests any interactive sub-element), this materializes a small recycled
/// pool of <typeparamref name="TRow"/> instances — only enough to cover the viewport plus one row of
/// overscan — and rebinds them as the list scrolls. Because the rows are real views, their own
/// controllers handle hover/click/layout: a button inside a row lights up on hover for free, with no
/// pointer-position bookkeeping here.
///
/// Uniform <see cref="RowHeight"/> only (variable height is future work). Build an empty row once in
/// <see cref="CreateRow"/> and push the per-index data in <see cref="BindRow"/>. A row instance is
/// reused for many indices over its life, so it must be safe to rebind and must route its interactions
/// through its <em>current</em> bound state — never a captured index (the same contract as
/// <c>CalendarDayCell</c>). Attach <see cref="VirtualWidgetListController"/> for wheel scrolling.
/// </summary>
public sealed class VirtualWidgetListView<TRow> : View, IWheelScrollable where TRow : View
{
    public int ItemCount { get; set; }
    public float RowHeight { get; set; } = 22f;
    public float ScrollWheelStep { get; set; } = 60f;

    /// <summary>Builds one empty, reusable row. Called lazily as the pool grows to cover the viewport;
    /// the pool size stabilizes after the first scroll. Close over whatever the row needs (canvas, input).</summary>
    public required Func<TRow> CreateRow { get; init; }

    /// <summary>Pushes the data at <paramref name="index"/> into a pooled row. Called when a row is first
    /// shown, recycled to a new index, or force-refreshed (see <see cref="RefreshRows"/>).</summary>
    public required Action<TRow, int> BindRow { get; init; }

    public float ScrollY => _scrollY;

    /// <summary>Total height of all rows in content space — <c>ItemCount * RowHeight</c>. A scrollbar
    /// sizes its thumb from this against the laid-out viewport height.</summary>
    public float ContentHeight => ItemCount * RowHeight;

    /// <summary>Fires when <see cref="ScrollY"/> changes (wheel, programmatic, clamp). Unlike
    /// <see cref="LayoutSynced"/> it does not fire on plain re-layout.</summary>
    public event Action? ScrollChanged;

    /// <summary>Fires at the end of every layout pass, after rows are recycled and positioned — the seam a
    /// scrollbar uses to resync its thumb's scale/position to the current scroll and content height.</summary>
    internal event Action? LayoutSynced;

    public override bool ClipsContent => true;

    private float _scrollY;
    private bool _contentDirty;
    private readonly List<TRow> _pool = new();
    private readonly List<int> _boundIndex = new();

    /// <summary>Programmatically sets the (clamped) scroll position, firing <see cref="ScrollChanged"/>
    /// if it actually moved.</summary>
    public void SetScrollY(float y)
    {
        var prev = _scrollY;
        _scrollY = y;
        ClampScroll();
        if (_scrollY != prev)
        {
            ScrollChanged?.Invoke();
            SetDirty();
        }
    }

    /// <summary>Call after <see cref="ItemCount"/> changes. Clamps scroll into the new range and forces a
    /// rebind of the visible rows.</summary>
    public void NotifyItemsChanged()
    {
        _contentDirty = true;
        if (ItemCount == 0) _scrollY = 0f;
        ClampScroll();
        SetDirty();
    }

    /// <summary>Force the currently visible rows to rebind on the next layout, without changing
    /// <see cref="ItemCount"/> — for row state that lives outside the item data (e.g. a keyboard highlight
    /// the consumer tracks separately).</summary>
    public void RefreshRows()
    {
        _contentDirty = true;
        SetDirty();
    }

    /// <summary>The inclusive range of row indices currently within the viewport (no overscan), or
    /// (0, -1) when nothing is visible.</summary>
    public (int First, int Last) VisibleRange()
    {
        var bodyHeight = Position.Height;
        if (ItemCount == 0 || bodyHeight <= 0f) return (0, -1);
        var first = Math.Max(0, (int)(_scrollY / RowHeight));
        var last = Math.Min(ItemCount - 1, (int)((_scrollY + bodyHeight) / RowHeight));
        return (first, last);
    }

    /// <summary>Scrolls just enough to bring the row at <paramref name="rowIndex"/> fully into view.
    /// No-op if it is already visible or out of range.</summary>
    public void EnsureRowVisible(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= ItemCount) return;
        var bodyHeight = Position.Height;
        if (bodyHeight <= 0) return;

        var rowStart = rowIndex * RowHeight;
        var rowEnd = rowStart + RowHeight;
        var target = _scrollY;
        if (rowStart < target) target = rowStart;
        else if (rowEnd > target + bodyHeight) target = rowEnd - bodyHeight;
        SetScrollY(target);
    }

    public void OnWheel(float deltaX, float deltaY)
    {
        if (ItemCount <= 0 || deltaY == 0f) return;
        var prev = _scrollY;
        _scrollY -= deltaY * ScrollWheelStep;
        ClampScroll();
        if (_scrollY != prev)
        {
            ScrollChanged?.Invoke();
            SetDirty();
        }
    }

    // Recycling happens here, not in OnDrawSelf: rows are retained views, so we position them in the
    // layout pass and let the normal draw/input traversal handle the rest.
    protected override void OnLayoutChildren()
    {
        ClampScroll();
        var pos = Position;
        var bodyHeight = pos.Height;

        int first, last;
        if (ItemCount == 0 || bodyHeight <= 0f)
        {
            first = 0;
            last = -1;
        }
        else
        {
            first = Math.Max(0, (int)(_scrollY / RowHeight) - 1);
            last = Math.Min(ItemCount - 1, (int)((_scrollY + bodyHeight) / RowHeight) + 1);
        }
        var visible = Math.Max(0, last - first + 1);

        // Grow the pool to cover the viewport. Adding a child mounts it (its controllers register), so
        // a recycled row keeps working as the list scrolls; the pool only ever grows.
        while (_pool.Count < visible)
        {
            var row = CreateRow();
            AddChildToSelf(row);
            _pool.Add(row);
            _boundIndex.Add(-1);
        }

        for (var k = 0; k < visible; k++)
        {
            var dataIndex = first + k;
            var row = _pool[k];
            if (_boundIndex[k] != dataIndex || _contentDirty)
            {
                BindRow(row, dataIndex);
                _boundIndex[k] = dataIndex;
            }

            row.IsVisible = true;
            var rowTop = pos.Top + _scrollY - dataIndex * RowHeight;
            row.LeftConstraint = pos.Left;
            row.BottomConstraint = rowTop - RowHeight;
            row.WidthConstraint = pos.Width;
            row.HeightConstraint = RowHeight;
            row.LayoutSelf();
        }

        // Park the surplus: hidden views are skipped by both draw and hit-testing, so they stay mounted
        // (no input churn) but inert. Drop their bound index so they rebind cleanly when reused.
        for (var k = visible; k < _pool.Count; k++)
        {
            if (!_pool[k].IsVisible) continue;
            _pool[k].IsVisible = false;
            _boundIndex[k] = -1;
        }

        _contentDirty = false;
        LayoutSynced?.Invoke();
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
    }

    private void ClampScroll()
    {
        if (Position.Height <= 0) return;
        var max = Math.Max(0f, ContentHeight - Position.Height);
        if (_scrollY < 0f) _scrollY = 0f;
        else if (_scrollY > max) _scrollY = max;
    }
}

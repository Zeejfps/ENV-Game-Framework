using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;

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
/// Mirrors <see cref="VirtualRowList.VirtualRowListView"/>'s geometry + interaction surface
/// (<see cref="TryGetRowRect"/>, <see cref="RowClicked"/>/<see cref="RowActivated"/>/
/// <see cref="RowContextRequested"/>, hover/context tracking, opt-in variable height via
/// <see cref="RowHeightAt"/>) so a consumer that floats overlays over rows and routes selection through
/// the list can keep that machinery while the rows become widgets. Attach
/// <see cref="VirtualWidgetListController{TRow}"/> for input.
///
/// Build an empty row once in <see cref="CreateRow"/> and push per-index data in <see cref="BindRow"/>.
/// A row instance is reused for many indices, so it must be safe to rebind and must route its
/// interactions through its <em>current</em> bound state — never a captured index.
/// </summary>
public sealed class VirtualWidgetListView<TRow> : View where TRow : View
{
    public int ItemCount { get; set; }
    public float RowHeight { get; set; } = 22f;
    public float ScrollWheelStep { get; set; } = 60f;
    public int DoubleClickThresholdMs { get; set; } = 400;

    /// <summary>
    /// Opt-in per-row height. When null (the default) every row is <see cref="RowHeight"/> and the
    /// widget uses the closed-form uniform index math, paying nothing for variable heights. When set,
    /// it builds a cached cumulative-offset table and resolves rows by binary search. Rebuilt lazily
    /// after <see cref="NotifyItemsChanged"/> or <see cref="InvalidateRowHeights"/>.
    /// </summary>
    public Func<int, float>? RowHeightAt { get; set; }

    /// <summary>Builds one empty, reusable row. Close over whatever the row needs (canvas, input).</summary>
    public required Func<TRow> CreateRow { get; init; }

    /// <summary>Pushes the data at <paramref name="index"/> into a pooled row. Called when a row is first
    /// shown, recycled to a new index, or force-refreshed. The closure may read <see cref="HoveredIndex"/>
    /// / <see cref="ContextHighlightIndex"/> to drive per-row chrome — both trigger a rebind when they change.</summary>
    public required Action<TRow, int> BindRow { get; init; }

    public float ScrollY => _scrollY;

    /// <summary>Total height of all rows in content space — <c>ItemCount * RowHeight</c> (uniform) or the
    /// sum of per-row heights (variable). A scrollbar sizes its thumb from this against the viewport.</summary>
    public float ContentHeight => RowHeightAt == null ? ItemCount * RowHeight : Offsets()[ItemCount];

    public int? HoveredIndex => _hoveredIndex < 0 ? null : _hoveredIndex;
    public int? ContextHighlightIndex => _contextHighlightIndex < 0 ? null : _contextHighlightIndex;

    /// <summary>Fires when <see cref="ScrollY"/> changes (wheel, programmatic, clamp).</summary>
    public event Action? ScrollChanged;

    /// <summary>Fires at the end of every layout pass, after rows are recycled and positioned — the seam a
    /// scrollbar uses to resync its thumb. Distinct from <see cref="ScrollChanged"/> (scroll-value only).</summary>
    internal event Action? LayoutSynced;

    /// <summary>Left-click within the widget: the hit row index, or -1 if it missed every row. Modifiers
    /// carry Shift/Ctrl/Cmd; the point is in GUI coordinates for column hit-testing. Fires only when a
    /// child widget didn't consume the press first (see <see cref="VirtualWidgetListController{TRow}"/>).</summary>
    public event Action<int, InputModifiers, PointF>? RowClicked;

    /// <summary>A left-click double-tap on the same row within the threshold.</summary>
    public event Action<int>? RowActivated;

    /// <summary>Right-click within the widget: the hit row index (or -1) and the GUI-coordinate point.</summary>
    public event Action<int, PointF>? RowContextRequested;

    public override bool ClipsContent => true;

    private float _scrollY;
    private bool _contentDirty;
    private int _hoveredIndex = -1;
    private int _contextHighlightIndex = -1;
    private bool _hasLastClick;
    private int _lastClickTickMs;
    private int _lastClickIndex = -1;
    private float[]? _offsets;
    // A reveal requested before the list had a viewport, replayed on the first layout. _pendingRevealOffset null
    // means "minimal scroll into view"; a value means "place the row that many pixels below the viewport top".
    private int _pendingRevealIndex = -1;
    private float? _pendingRevealOffset;
    private readonly List<TRow> _pool = new();
    private readonly List<int> _boundIndex = new();

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

    /// <summary>Call after <see cref="ItemCount"/> changes. Drops the variable-height cache, clamps scroll
    /// into the new range, clears stale hover/context, and forces a rebind of the visible rows.</summary>
    public void NotifyItemsChanged()
    {
        _offsets = null;
        _contentDirty = true;
        if (ItemCount == 0) _scrollY = 0f;
        if (_hoveredIndex >= ItemCount) _hoveredIndex = -1;
        if (_contextHighlightIndex >= ItemCount) _contextHighlightIndex = -1;
        ClampScroll();
        SetDirty();
    }

    /// <summary>Discards the cached variable-height offset table so it rebuilds on the next layout, and
    /// rebinds the visible rows. Call when <see cref="RowHeightAt"/> would return different values but
    /// <see cref="ItemCount"/> is unchanged (e.g. a row expands/collapses). No-op in uniform mode.</summary>
    public void InvalidateRowHeights()
    {
        if (RowHeightAt == null) return;
        _offsets = null;
        _contentDirty = true;
        ClampScroll();
        SetDirty();
    }

    /// <summary>Force the currently visible rows to rebind on the next layout, without changing
    /// <see cref="ItemCount"/> — for row state that lives outside the item data.</summary>
    public void RefreshRows()
    {
        _contentDirty = true;
        SetDirty();
    }

    /// <summary>Highlights a row as if a context menu were open over it; pass null to clear. Survives the
    /// pointer drifting off the row.</summary>
    public void SetContextHighlight(int? rowIndex)
    {
        var next = rowIndex ?? -1;
        if (_contextHighlightIndex == next) return;
        _contextHighlightIndex = next;
        RefreshRows();
    }

    /// <summary>The rect a row currently occupies, in GUI coordinates, for any in-range index — even one
    /// scrolled outside the viewport or not currently materialized. Lets a consumer float a retained
    /// editor over a row and ride scroll. False only when out of range.</summary>
    public bool TryGetRowRect(int index, out RectF rect)
    {
        if (index < 0 || index >= ItemCount)
        {
            rect = default;
            return false;
        }
        rect = RowRect(index);
        return true;
    }

    /// <summary>The row index at a GUI-coordinate point, or -1 if outside the viewport / hitting no row.</summary>
    public int RowIndexAt(PointF point) => HitTestRow(point);

    /// <summary>The inclusive range of row indices currently within the viewport (no overscan), or
    /// (0, -1) when nothing is visible.</summary>
    public (int First, int Last) VisibleRange()
    {
        var bodyHeight = Position.Height;
        if (ItemCount == 0 || bodyHeight <= 0f) return (0, -1);

        if (RowHeightAt == null)
        {
            var first = Math.Max(0, (int)(_scrollY / RowHeight));
            var last = Math.Min(ItemCount - 1, (int)((_scrollY + bodyHeight) / RowHeight));
            return (first, last);
        }

        var offsets = Offsets();
        return (
            Math.Max(0, IndexAtContentY(offsets, _scrollY)),
            Math.Min(ItemCount - 1, IndexAtContentY(offsets, _scrollY + bodyHeight)));
    }

    /// <summary>Scrolls just enough to bring the row at <paramref name="rowIndex"/> fully into view. When called
    /// before the list has a viewport — e.g. a register built already navigated to a row, whose reveal runs in the
    /// view-model constructor before the first layout — the request is remembered and applied on that first layout
    /// instead of being dropped, which would leave the navigated-to row scrolled off-screen.</summary>
    public void EnsureRowVisible(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= ItemCount) return;
        if (Position.Height <= 0f)
        {
            _pendingRevealIndex = rowIndex;
            _pendingRevealOffset = null;
            SetDirty();
            return;
        }
        _pendingRevealIndex = -1;
        SetScrollY(ScrollToReveal(rowIndex));
    }

    /// <summary>Scrolls so the row at <paramref name="rowIndex"/> sits <paramref name="viewportOffset"/> pixels
    /// below the top of the viewport — used to land a navigated-to row at the same on-screen position the source
    /// row occupied, so the jump isn't jarring. Clamped to the scroll range (a row too near the top simply lands
    /// as high as it can). Like <see cref="EnsureRowVisible"/>, deferred to the first layout if there's no viewport
    /// yet.</summary>
    public void RevealRowAtOffset(int rowIndex, float viewportOffset)
    {
        if (rowIndex < 0 || rowIndex >= ItemCount) return;
        if (Position.Height <= 0f)
        {
            _pendingRevealIndex = rowIndex;
            _pendingRevealOffset = viewportOffset;
            SetDirty();
            return;
        }
        _pendingRevealIndex = -1;
        SetScrollY(ContentTopOf(rowIndex) - viewportOffset);
    }

    /// <summary>How far (pixels) the top of the row at <paramref name="rowIndex"/> currently sits below the top of
    /// the viewport — i.e. where it is on screen. Null when the list isn't laid out yet or the index is out of
    /// range. The capture half of <see cref="RevealRowAtOffset"/>.</summary>
    public float? RowViewportOffset(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= ItemCount || Position.Height <= 0f) return null;
        return ContentTopOf(rowIndex) - _scrollY;
    }

    // Content-space top of a row: the closed-form uniform position, or the cumulative offset in variable mode.
    private float ContentTopOf(int index) => RowHeightAt == null ? index * RowHeight : Offsets()[index];

    // The scroll offset that brings row `rowIndex` fully into view with the smallest move from the current
    // offset. Assumes a valid (positive) Position.Height — callers guard that.
    private float ScrollToReveal(int rowIndex)
    {
        var rowStart = ContentTopOf(rowIndex);
        var rowEnd = RowHeightAt == null ? rowStart + RowHeight : Offsets()[rowIndex + 1];

        var target = _scrollY;
        if (rowStart < target) target = rowStart;
        else if (rowEnd > target + Position.Height) target = rowEnd - Position.Height;
        return target;
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

    internal void OnPointerMove(PointF point)
    {
        var idx = HitTestRow(point);
        if (idx == _hoveredIndex) return;
        _hoveredIndex = idx;
        RefreshRows();
    }

    internal void OnPointerExit()
    {
        if (_hoveredIndex < 0) return;
        _hoveredIndex = -1;
        RefreshRows();
    }

    internal void OnLeftClick(PointF point, InputModifiers modifiers)
    {
        if (!Position.ContainsPoint(point)) return;
        var idx = HitTestRow(point);

        RowClicked?.Invoke(idx, modifiers, point);

        if (idx < 0)
        {
            _hasLastClick = false;
            return;
        }

        var now = Environment.TickCount;
        var isDouble = _hasLastClick
            && _lastClickIndex == idx
            && unchecked(now - _lastClickTickMs) <= DoubleClickThresholdMs;
        if (isDouble)
        {
            RowActivated?.Invoke(idx);
            _hasLastClick = false;
        }
        else
        {
            _lastClickTickMs = now;
            _lastClickIndex = idx;
            _hasLastClick = true;
        }
    }

    internal void OnRightClick(PointF point)
    {
        if (!Position.ContainsPoint(point)) return;
        RowContextRequested?.Invoke(HitTestRow(point), point);
    }

    // Recycling happens here, not in OnDrawSelf: rows are retained views, so we position them in the
    // layout pass and let the normal draw/input traversal handle the rest.
    protected override void OnLayoutChildren()
    {
        // A reveal requested before the list had a viewport (a register that opens already navigated to a row,
        // laid out for the first time here) was deferred; satisfy it now, before the visible window is computed,
        // by setting the scroll directly — the LayoutSynced at the end resyncs any attached scrollbar.
        if (_pendingRevealIndex >= 0 && Position.Height > 0f)
        {
            if (_pendingRevealIndex < ItemCount)
                _scrollY = _pendingRevealOffset is { } offset
                    ? ContentTopOf(_pendingRevealIndex) - offset
                    : ScrollToReveal(_pendingRevealIndex);
            _pendingRevealIndex = -1;
            _pendingRevealOffset = null;
        }

        ClampScroll();
        var pos = Position;
        var bodyHeight = pos.Height;

        int first, last;
        if (ItemCount == 0 || bodyHeight <= 0f)
        {
            first = 0;
            last = -1;
        }
        else if (RowHeightAt == null)
        {
            first = Math.Max(0, (int)(_scrollY / RowHeight) - 1);
            last = Math.Min(ItemCount - 1, (int)((_scrollY + bodyHeight) / RowHeight) + 1);
        }
        else
        {
            var offsets = Offsets();
            first = Math.Max(0, IndexAtContentY(offsets, _scrollY) - 1);
            last = Math.Min(ItemCount - 1, IndexAtContentY(offsets, _scrollY + bodyHeight) + 1);
        }
        var visible = Math.Max(0, last - first + 1);

        // Grow the pool to cover the viewport. Adding a child mounts it (its controllers register), so a
        // recycled row keeps working as the list scrolls; the pool only ever grows.
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
            var rect = RowRect(dataIndex);
            row.LeftConstraint = rect.Left;
            row.BottomConstraint = rect.Bottom;
            row.WidthConstraint = rect.Width;
            row.HeightConstraint = rect.Height;
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

    private RectF RowRect(int index)
    {
        var pos = Position;
        if (RowHeightAt == null)
        {
            var rowTop = pos.Top + _scrollY - index * RowHeight;
            return new RectF(pos.Left, rowTop - RowHeight, pos.Width, RowHeight);
        }

        var offsets = Offsets();
        var top = pos.Top + _scrollY - offsets[index];
        var height = offsets[index + 1] - offsets[index];
        return new RectF(pos.Left, top - height, pos.Width, height);
    }

    // Builds (and caches) the cumulative top-offset table for variable-height mode. O(ItemCount) on the
    // first call after an invalidation, then O(1). Only reached when RowHeightAt is non-null.
    private float[] Offsets()
    {
        var n = ItemCount;
        if (_offsets is { } cached && cached.Length == n + 1) return cached;

        var offsets = new float[n + 1];
        var acc = 0f;
        for (var i = 0; i < n; i++)
        {
            offsets[i] = acc;
            acc += RowHeightAt!(i);
        }
        offsets[n] = acc;
        _offsets = offsets;
        return offsets;
    }

    // The row index whose band contains content-space Y; -1 above the first row, ItemCount past the last.
    private int IndexAtContentY(float[] offsets, float y)
    {
        if (y < 0f) return -1;
        if (y >= offsets[ItemCount]) return ItemCount;

        int lo = 0, hi = ItemCount;
        while (lo < hi)
        {
            var mid = (lo + hi) >> 1;
            if (offsets[mid] <= y) lo = mid + 1;
            else hi = mid;
        }
        return lo - 1;
    }

    private int HitTestRow(PointF point)
    {
        var pos = Position;
        if (point.X < pos.Left || point.X > pos.Right) return -1;
        if (point.Y < pos.Bottom || point.Y > pos.Top) return -1;
        if (ItemCount == 0) return -1;

        var contentY = pos.Top - point.Y + _scrollY;
        var idx = RowHeightAt == null ? (int)(contentY / RowHeight) : IndexAtContentY(Offsets(), contentY);
        if (idx < 0 || idx >= ItemCount) return -1;
        return idx;
    }

    private void ClampScroll()
    {
        if (Position.Height <= 0) return;
        var max = Math.Max(0f, ContentHeight - Position.Height);
        if (_scrollY < 0f) _scrollY = 0f;
        else if (_scrollY > max) _scrollY = max;
    }
}

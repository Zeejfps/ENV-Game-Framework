using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.VirtualRowList;

/// <summary>
/// State flags the widget passes to <see cref="VirtualRowListView.ItemBuilder"/> so the
/// consumer can draw row chrome (hover/context-highlight backgrounds) without owning the
/// hit-test or pointer state itself. Selection is intentionally not here — selection
/// shape varies per consumer and the consumer closes over its own selection state.
/// </summary>
public readonly record struct RowRenderState(bool IsHovered, bool IsContextHighlighted);

/// <summary>
/// A vertically-scrolling, virtualized row list. Owns scroll position, hover state,
/// hit-testing, wheel handling, and double-click detection. Knows nothing about row
/// content — the consumer supplies an <see cref="ItemBuilder"/> that draws each row
/// into a caller-owned rect.
///
/// Uniform <see cref="RowHeight"/> by default. Set <see cref="RowHeightAt"/> to opt into
/// per-row variable heights; while it is null the widget stays on the closed-form uniform
/// path and pays nothing for the variable-height machinery.
///
/// Input is routed via <see cref="VirtualRowListController"/> — attach it with
/// <c>view.UseController(ctx =&gt; new VirtualRowListController(list))</c>.
/// </summary>
public sealed class VirtualRowListView : View
{
    public int ItemCount { get; set; }
    public float RowHeight { get; set; } = 22f;

    /// <summary>
    /// Opt-in per-row height. When null (the default) every row is <see cref="RowHeight"/> and
    /// the widget uses the closed-form uniform index math, paying nothing for variable heights.
    /// When set, the widget builds a cached cumulative-offset table and resolves rows by binary
    /// search, so callers that mix row heights (e.g. an expanded accordion row) only pay for it
    /// when they opt in. The table is rebuilt lazily after <see cref="NotifyItemsChanged"/> or
    /// <see cref="InvalidateRowHeights"/>; call one of those whenever a height result changes.
    /// </summary>
    public Func<int, float>? RowHeightAt { get; set; }

    public float ScrollWheelStep { get; set; } = ScrollDefaults.WheelStep;
    public int DoubleClickThresholdMs { get; set; } = 400;

    /// <summary>
    /// Called for each visible row during draw. The widget supplies the row's rect and
    /// state flags; the consumer draws all visuals (backgrounds, text, icons, badges)
    /// into <paramref name="canvas"/> at the supplied z-index.
    /// </summary>
    public Action<ICanvas, RectF, int, RowRenderState, int>? ItemBuilder { get; set; }

    /// <summary>
    /// Drawn once per frame — after scroll is clamped, before the rows, inside the list's clip —
    /// with the viewport rect and a z-index that sits <b>below</b> row content (rows draw at
    /// <c>z + 2</c>). Lets a consumer float a single selection bar that rides scroll and can
    /// animate between rows, instead of the per-row <see cref="ItemBuilder"/> painting selection
    /// into every row. Null draws nothing.
    /// </summary>
    public Action<ICanvas, RectF, int>? SelectionOverlayBuilder { get; set; }

    /// <summary>
    /// Fires when the user left-clicks within the widget. The index is the row that was
    /// hit, or -1 if the click landed in the widget but missed every row (consumers
    /// typically use -1 to clear selection). Modifiers carry Shift/Ctrl/Cmd state for
    /// consumers that need range-extend or toggle-select semantics. The point is the
    /// click location in GUI coordinates, for consumers that hit-test columns within a
    /// row (e.g. a tree chevron).
    /// </summary>
    public event Action<int, InputModifiers, PointF>? RowClicked;

    /// <summary>Fires when a left-click double-tap lands on the same row within the threshold.</summary>
    public event Action<int>? RowActivated;

    /// <summary>
    /// Fires when the user right-clicks within the widget. The index is the row that was
    /// hit, or -1 if the click landed in the widget but missed every row (consumers use
    /// -1 to offer an empty-area menu, or ignore it). The consumer is responsible for
    /// opening any menu and calling <see cref="SetContextHighlight"/> to keep a row
    /// visually marked for the duration of the menu.
    /// </summary>
    public event Action<int, PointF>? RowContextRequested;

    /// <summary>Fires when <see cref="ScrollY"/> changes (wheel, programmatic scroll, clamp).</summary>
    public event Action? ScrollChanged;

    /// <summary>
    /// Receives the horizontal component of wheel events the widget itself doesn't act on
    /// (the list is vertical-only). Consumers that wrap the widget in a horizontally
    /// scrollable container (e.g. <c>DiffContentView</c>) hook this to apply DeltaX to
    /// their own scroll offset.
    /// </summary>
    public Action<float>? HorizontalWheelHandler { get; set; }

    public float ScrollY => _scrollY;

    /// <summary>
    /// Total height of all rows in content space — <c>ItemCount * RowHeight</c> in uniform mode,
    /// or the sum of per-row heights in variable mode. Consumers size a scrollbar from this so
    /// the thumb tracks the same content the widget scrolls against.
    /// </summary>
    public float ContentHeight => RowHeightAt == null ? ItemCount * RowHeight : Offsets()[ItemCount];

    public int? HoveredIndex => _hoveredIndex < 0 ? null : _hoveredIndex;
    public int? ContextHighlightIndex => _contextHighlightIndex < 0 ? null : _contextHighlightIndex;

    private float _scrollY;
    private int _hoveredIndex = -1;
    private int _contextHighlightIndex = -1;
    private bool _hasLastClick;
    private int _lastClickTickMs;
    private int _lastClickIndex = -1;

    // Cumulative top offsets for variable-height mode: _offsets[i] is the distance from content
    // top to the top of row i; _offsets[ItemCount] is total content height. Null in uniform mode
    // or when stale; rebuilt lazily by Offsets(). See RowHeightAt.
    private float[]? _offsets;

    public override bool ClipsContent => true;

    /// <summary>
    /// Programmatically sets the scroll position (clamped). Fires
    /// <see cref="ScrollChanged"/> if the value actually changed. Use for scrollbar
    /// sync, snapshot scroll preservation, or "scroll to" gestures.
    /// </summary>
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

    /// <summary>
    /// Scrolls just enough to bring the row at <paramref name="rowIndex"/> into the
    /// visible viewport. No-op if the row is already visible or out of range.
    /// </summary>
    public void EnsureRowVisible(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= ItemCount) return;
        var bodyHeight = Position.Height;
        if (bodyHeight <= 0) return;

        float rowStart, rowEnd;
        if (RowHeightAt == null)
        {
            rowStart = rowIndex * RowHeight;
            rowEnd = rowStart + RowHeight;
        }
        else
        {
            var offsets = Offsets();
            rowStart = offsets[rowIndex];
            rowEnd = offsets[rowIndex + 1];
        }

        var target = _scrollY;
        if (rowStart < target) target = rowStart;
        else if (rowEnd > target + bodyHeight) target = rowEnd - bodyHeight;
        SetScrollY(target);
    }

    /// <summary>
    /// Highlights a row as if a context menu were open over it; pass null to clear.
    /// Survives the pointer drifting off the row.
    /// </summary>
    public void SetContextHighlight(int? rowIndex)
    {
        var next = rowIndex ?? -1;
        if (_contextHighlightIndex == next) return;
        _contextHighlightIndex = next;
        SetDirty();
    }

    /// <summary>
    /// Call after <see cref="ItemCount"/> changes. Clamps scroll into the new valid range
    /// and clears stale hover / context-highlight indices.
    /// </summary>
    public void NotifyItemsChanged()
    {
        _offsets = null;
        if (ItemCount == 0) _scrollY = 0f;
        if (_hoveredIndex >= ItemCount) _hoveredIndex = -1;
        if (_contextHighlightIndex >= ItemCount) _contextHighlightIndex = -1;
        ClampScroll();
        SetDirty();
    }

    /// <summary>
    /// Discards the cached variable-height offset table so it rebuilds on the next draw/hit-test.
    /// Call when <see cref="RowHeightAt"/> would return different values but <see cref="ItemCount"/>
    /// is unchanged (e.g. a row expands or collapses). No-op in uniform mode.
    /// </summary>
    public void InvalidateRowHeights()
    {
        if (RowHeightAt == null) return;
        _offsets = null;
        ClampScroll();
        SetDirty();
    }

    /// <summary>
    /// Computes the rect a row currently occupies, in GUI coordinates, for any in-range
    /// index — even one scrolled outside the viewport. Returns false only when
    /// <paramref name="index"/> is out of range (covers <see cref="ItemCount"/> == 0).
    /// The result is purely a function of the index and current scroll, independent of
    /// layout readiness; callers that need on-screen-ness intersect the rect with
    /// <see cref="View.Position"/> themselves. Consumers use this to float a retained
    /// editor over a painted row and let it ride the cell as the list scrolls.
    /// </summary>
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

    /// <summary>The row index at a point in GUI coordinates, or -1 if the point is outside the viewport
    /// or hits no row. The inverse of <see cref="TryGetRowRect"/> — lets a consumer resolve a drag/drop
    /// pointer to a row without re-deriving the scroll math.</summary>
    public int RowIndexAt(PointF point) => HitTestRow(point);

    /// <summary>The inclusive range of row indices currently within the viewport (no overscan), or
    /// (0, -1) when nothing is visible. Lets a consumer drive a data window without re-deriving the
    /// height math; correct in both uniform and variable-height modes.</summary>
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

    protected override void OnDrawSelf(ICanvas c)
    {
        var pos = Position;
        var z = GetDrawZIndex();

        c.PushClip(pos);

        if (ItemCount == 0 || ItemBuilder == null)
        {
            c.PopClip();
            return;
        }

        ClampScroll();

        // Selection bar floats here, below row content (rows draw at z + 2), so a consumer can
        // animate one bar across rows independently of the per-row builder.
        SelectionOverlayBuilder?.Invoke(c, pos, z);

        var top = pos.Top;
        var bodyHeight = pos.Height;
        int firstVisible, lastVisible;
        if (RowHeightAt == null)
        {
            firstVisible = Math.Max(0, (int)(_scrollY / RowHeight) - 1);
            lastVisible = Math.Min(ItemCount - 1, (int)((_scrollY + bodyHeight) / RowHeight) + 1);
        }
        else
        {
            var offsets = Offsets();
            firstVisible = Math.Max(0, IndexAtContentY(offsets, _scrollY) - 1);
            lastVisible = Math.Min(ItemCount - 1, IndexAtContentY(offsets, _scrollY + bodyHeight) + 1);
        }

        for (var i = firstVisible; i <= lastVisible; i++)
        {
            var rowRect = RowRect(i);
            if (rowRect.Top <= pos.Bottom || rowRect.Bottom >= top) continue;

            var state = new RowRenderState(
                IsHovered: i == _hoveredIndex,
                IsContextHighlighted: i == _contextHighlightIndex);
            ItemBuilder(c, rowRect, i, state, z + 2);
        }

        c.PopClip();
    }

    internal void OnWheel(float deltaX, float deltaY)
    {
        if (ItemCount > 0 && deltaY != 0f)
        {
            var prev = _scrollY;
            _scrollY -= deltaY * ScrollWheelStep;
            ClampScroll();
            if (_scrollY != prev)
            {
                ScrollChanged?.Invoke();
                SetDirty();
            }
        }

        if (deltaX != 0f) HorizontalWheelHandler?.Invoke(deltaX);
    }

    internal void OnPointerMove(PointF point)
    {
        var idx = HitTestRow(point);
        if (idx == _hoveredIndex) return;
        _hoveredIndex = idx;
        SetDirty();
    }

    internal void OnPointerExit()
    {
        if (_hoveredIndex < 0) return;
        _hoveredIndex = -1;
        SetDirty();
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
        var idx = HitTestRow(point);
        RowContextRequested?.Invoke(idx, point);
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

    // Builds (and caches) the cumulative top-offset table for variable-height mode. O(ItemCount)
    // on the first call after an invalidation, then O(1) until ItemCount or a height changes. The
    // length guard also catches an ItemCount change that skipped NotifyItemsChanged. Only reached
    // when RowHeightAt is non-null, so the callback dereference is safe.
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

    // The row index whose band contains content-space Y; -1 above the first row, ItemCount past
    // the last. Binary search over the sorted offset table — preserves the uniform path's
    // "miss = out of range" semantics for points below the last row.
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

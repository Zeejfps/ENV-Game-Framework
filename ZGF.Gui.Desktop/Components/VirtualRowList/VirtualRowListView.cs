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
/// Fixed row height for now; variable heights can be added later via a per-index
/// callback without breaking the existing surface.
///
/// Input is routed via <see cref="VirtualRowListController"/> — attach it with
/// <c>view.UseController(ctx =&gt; new VirtualRowListController(list))</c>.
/// </summary>
public sealed class VirtualRowListView : View
{
    public int ItemCount { get; set; }
    public float RowHeight { get; set; } = 22f;
    public float ScrollWheelStep { get; set; } = 60f;
    public int DoubleClickThresholdMs { get; set; } = 400;

    /// <summary>
    /// Called for each visible row during draw. The widget supplies the row's rect and
    /// state flags; the consumer draws all visuals (backgrounds, text, icons, badges)
    /// into <paramref name="canvas"/> at the supplied z-index.
    /// </summary>
    public Action<ICanvas, RectF, int, RowRenderState, int>? ItemBuilder { get; set; }

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
    public int? HoveredIndex => _hoveredIndex < 0 ? null : _hoveredIndex;
    public int? ContextHighlightIndex => _contextHighlightIndex < 0 ? null : _contextHighlightIndex;

    private float _scrollY;
    private int _hoveredIndex = -1;
    private int _contextHighlightIndex = -1;
    private bool _hasLastClick;
    private int _lastClickTickMs;
    private int _lastClickIndex = -1;

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

        var rowStart = rowIndex * RowHeight;
        var rowEnd = rowStart + RowHeight;

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
        if (ItemCount == 0) _scrollY = 0f;
        if (_hoveredIndex >= ItemCount) _hoveredIndex = -1;
        if (_contextHighlightIndex >= ItemCount) _contextHighlightIndex = -1;
        ClampScroll();
        SetDirty();
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

        var top = pos.Top;
        var bodyHeight = pos.Height;
        var firstVisible = Math.Max(0, (int)(_scrollY / RowHeight) - 1);
        var lastVisible = Math.Min(ItemCount - 1, (int)((_scrollY + bodyHeight) / RowHeight) + 1);

        for (var i = firstVisible; i <= lastVisible; i++)
        {
            var rowTop = top + _scrollY - i * RowHeight;
            var rowBottom = rowTop - RowHeight;
            if (rowTop <= pos.Bottom || rowBottom >= top) continue;

            var rowRect = new RectF(pos.Left, rowBottom, pos.Width, RowHeight);
            var state = new RowRenderState(
                IsHovered: i == _hoveredIndex,
                IsContextHighlighted: i == _contextHighlightIndex);
            ItemBuilder(c, rowRect, i, state, z + 1);
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

    private int HitTestRow(PointF point)
    {
        var pos = Position;
        if (point.X < pos.Left || point.X > pos.Right) return -1;
        if (point.Y < pos.Bottom || point.Y > pos.Top) return -1;
        if (ItemCount == 0) return -1;

        var distFromTop = pos.Top - point.Y;
        var idx = (int)((distFromTop + _scrollY) / RowHeight);
        if (idx < 0 || idx >= ItemCount) return -1;
        return idx;
    }

    private void ClampScroll()
    {
        if (Position.Height <= 0) return;
        var max = Math.Max(0f, ItemCount * RowHeight - Position.Height);
        if (_scrollY < 0f) _scrollY = 0f;
        else if (_scrollY > max) _scrollY = max;
    }
}

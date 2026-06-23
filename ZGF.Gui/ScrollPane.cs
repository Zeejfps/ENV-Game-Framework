using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui;

/// <summary>
/// A two-axis scroll pane. Holds a single inner content (a <see cref="ColumnView"/>),
/// gives it max(viewport, natural) on each axis, and translates it by the current scroll
/// distances. Fires normalized scroll-position events for vertical and horizontal axes.
/// </summary>
public sealed class ScrollPane : View, IScrollableContent
{
    public event Action<float>? VerticalScrollPositionChanged;
    public event Action<float>? HorizontalScrollPositionChanged;

    private float _distanceFromTop;
    private float _distanceFromLeft;
    private float _maxDistanceFromTop;
    private float _maxDistanceFromLeft;

    public float VerticalScrollNormalized { get; private set; }
    public float HorizontalScrollNormalized { get; private set; }
    public float VerticalScale { get; private set; } = 1f;
    public float HorizontalScale { get; private set; } = 1f;

    private readonly ColumnView _columnView;

    public new ChildrenCollection Children => _columnView.Children;

    public override bool ClipsContent => true;

    public StyleValue<int> Gap
    {
        get => _columnView.Gap;
        set => _columnView.Gap = value;
    }

    public ScrollPane()
    {
        _columnView = new ColumnView();
        AddChildToSelf(_columnView);
    }

    protected override void OnLayoutChild(in RectF position, View child)
    {
        // Width: content as wide as the wider of viewport vs. its natural width. This lets
        // children that are narrower than the viewport stretch to fill it (no awkward gaps),
        // while wider content drives the horizontal scroll range.
        var naturalWidth = child.MeasureWidth();
        var contentWidth = Math.Max(position.Width, naturalWidth);

        // _distanceFromLeft is the distance scrolled from the leading edge. Under RTL the leading edge
        // is the right, so at rest (distance 0) the content's right edge aligns with the viewport's
        // right; scrolling then reveals the trailing (left) content. LTR keeps its left origin.
        child.LeftConstraint = IsRtl
            ? position.Right - contentWidth + _distanceFromLeft
            : position.Left - _distanceFromLeft;
        child.WidthConstraint = contentWidth;

        // Pass contentWidth so height-for-width children (wrapping text) report a height
        // that matches the width we're about to lay them out at.
        var naturalHeight = child.MeasureHeight(contentWidth);
        var contentHeight = Math.Max(position.Height, naturalHeight);

        // Bottom: positioned so the top edge of content sits at viewport.Top + distanceFromTop.
        // Increasing distanceFromTop pushes content up out of the viewport, revealing what's below.
        child.BottomConstraint = position.Top + _distanceFromTop - contentHeight;
        child.HeightConstraint = contentHeight;
        child.LayoutSelf();
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();

        var viewport = Position;
        var content = _columnView.Position;

        if (content.Height <= viewport.Height)
        {
            _maxDistanceFromTop = 0;
            VerticalScale = 1f;
            VerticalScrollNormalized = 0f;
        }
        else
        {
            _maxDistanceFromTop = content.Height - viewport.Height;
            VerticalScale = viewport.Height / content.Height;
            var scrollOffset = viewport.Bottom - content.Bottom;
            VerticalScrollNormalized = 1f - Math.Clamp(scrollOffset / _maxDistanceFromTop, 0f, 1f);
        }

        if (content.Width <= viewport.Width)
        {
            _maxDistanceFromLeft = 0;
            HorizontalScale = 1f;
            HorizontalScrollNormalized = 0f;
        }
        else
        {
            _maxDistanceFromLeft = content.Width - viewport.Width;
            HorizontalScale = viewport.Width / content.Width;
            HorizontalScrollNormalized = Math.Clamp(_distanceFromLeft / _maxDistanceFromLeft, 0f, 1f);
        }

        VerticalScrollPositionChanged?.Invoke(VerticalScrollNormalized);
        HorizontalScrollPositionChanged?.Invoke(HorizontalScrollNormalized);
    }

    public void ScrollVertical(float delta)
    {
        SetDistanceFromTop(_distanceFromTop + delta);
    }

    public void ScrollHorizontal(float delta)
    {
        SetDistanceFromLeft(_distanceFromLeft + delta);
    }

    public void ScrollToOrigin()
    {
        if (_distanceFromTop == 0 && _distanceFromLeft == 0) return;
        _distanceFromTop = 0;
        _distanceFromLeft = 0;
        SetDirty();
    }

    public void SetVerticalNormalizedScrollPosition(float normalized)
    {
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight(Position.Width);
        var range = contentHeight - viewportHeight;
        if (range <= 0)
        {
            _distanceFromTop = 0;
        }
        else
        {
            _distanceFromTop = Math.Clamp(range * normalized, 0f, range);
        }
        SetDirty();
    }

    public void SetHorizontalNormalizedScrollPosition(float normalized)
    {
        var viewportWidth = Position.Width;
        var contentWidth = _columnView.MeasureWidth();
        var range = contentWidth - viewportWidth;
        if (range <= 0)
        {
            _distanceFromLeft = 0;
        }
        else
        {
            _distanceFromLeft = Math.Clamp(range * normalized, 0f, range);
        }
        SetDirty();
    }

    private void SetDistanceFromTop(float value)
    {
        var clamped = Math.Clamp(value, 0f, _maxDistanceFromTop);
        if (Math.Abs(clamped - _distanceFromTop) < 0.0001f) return;
        _distanceFromTop = clamped;
        SetDirty();
    }

    private void SetDistanceFromLeft(float value)
    {
        var clamped = Math.Clamp(value, 0f, _maxDistanceFromLeft);
        if (Math.Abs(clamped - _distanceFromLeft) < 0.0001f) return;
        _distanceFromLeft = clamped;
        SetDirty();
    }
}

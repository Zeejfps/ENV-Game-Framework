using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui.VerticalScrollBar;

public sealed class VerticalScrollPane : View
{
    public event Action<float>? ScrollPositionChanged;

    private float _distanceFromTop;
    private float _maxDistanceFromTop;
    private readonly ColumnView _columnView;

    public float ScrollNormalized { get; private set; }
    public float Scale { get; private set; }
    public new ChildrenCollection Children => _columnView.Children;

    public override bool ClipsContent => true;

    /// <summary>
    /// When true the pane reports no intrinsic height of its own (a flex basis of 0), so a parent
    /// flex hands it only leftover space instead of its content's full height. It still scrolls its
    /// content within whatever height it is allotted. Use for a list that should absorb a
    /// container's slack rather than dictate the container's size.
    /// </summary>
    public bool FillParent { get; init; }

    /// <summary>
    /// When true the content is laid out at the viewport height whenever it is shorter than the
    /// viewport (rather than at its own smaller height), so a Grow child inside the content fills
    /// the pane. No effect once the content is taller than the viewport — then it scrolls.
    /// </summary>
    public bool StretchContent { get; init; }

    public StyleValue<int> Gap
    {
        get => _columnView.Gap;
        set => _columnView.Gap = value;
    }

    public VerticalScrollPane()
    {
        _columnView = new ColumnView();
        AddChildToSelf(_columnView);
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        if (Height.IsSet) return Height;
        return FillParent ? 0f : _columnView.MeasureHeight(availableWidth);
    }

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var childHeight = child.MeasureHeight(position.Width);
        if (StretchContent)
        {
            childHeight = Math.Max(childHeight, position.Height);
            child.HeightConstraint = childHeight;
        }
        child.BottomConstraint = position.Top + _distanceFromTop - childHeight;
        child.LeftConstraint = position.Left;
        child.WidthConstraint = position.Width;
        child.LayoutSelf();
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        // Only clip while content is actually scrolled out of view. When everything fits
        // (Scale == 1) nothing needs hiding, and clipping to the exact viewport would only
        // scissor borders and anti-aliasing flush against the edge — e.g. the right border of a
        // Browse button at the end of a dialog row. Children are forced to the viewport width, so
        // there is never horizontal overflow to clip regardless.
        if (Scale < 1f)
        {
            c.PushClip(Position);
            base.OnDrawChildren(c);
            c.PopClip();
        }
        else
        {
            base.OnDrawChildren(c);
        }
    }
    
    public void ScrollUp(float delta)
    {
        Scroll(-delta);
    }
    
    public void ScrollDown(float delta)
    {
        Scroll(delta);
    }

    public void ScrollTo(View view)
    {
        if (!Children.Contains(view))
            return;

        var viewportPosition = Position;
        var viewPosition = view.Position;
        if (viewPosition.FullyContains(viewPosition))
            return;
        
        // TODO: Finish
    }

    /// <summary>Scrolls by <paramref name="delta"/>; returns whether the offset actually moved
    /// (false when already pinned at an edge), so a wheel controller can decline to consume the
    /// event and let it bubble to an outer scrollable.</summary>
    public bool Scroll(float delta)
    {
        var previous = _distanceFromTop;
        _distanceFromTop += delta;
        if (_distanceFromTop < 0)
        {
            _distanceFromTop = 0;
        }
        else if (_distanceFromTop > _maxDistanceFromTop)
        {
            _distanceFromTop = _maxDistanceFromTop;
        }
        if (_distanceFromTop == previous) return false;
        SetDirty();
        return true;
    }

    public void ScrollToTop()
    {
        _distanceFromTop = 0;
        SetDirty();
    }
    
    public void ScrollToBottom()
    {
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight(Position.Width);

        if (contentHeight <= viewportHeight)
            return;

        var delta = _distanceFromTop + contentHeight - viewportHeight;
        Scroll(delta);
    }
    
    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        
        var viewportRect = Position;
        var contentRect = _columnView.Position;
        
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight(Position.Width);

        if (contentHeight <= viewportHeight)
        {
            _maxDistanceFromTop = 0;
            Scale = 1f;
            ScrollNormalized = 0f;
        }
        else
        {
            _maxDistanceFromTop = contentHeight - viewportHeight;
            Scale = viewportHeight / contentHeight;

            var scrollOffset = (viewportRect.Bottom - contentRect.Bottom);
            ScrollNormalized = 1f - Math.Clamp(scrollOffset / _maxDistanceFromTop, 0f, 1f);
        }

        ScrollPositionChanged?.Invoke(ScrollNormalized);
    }

    public void SetNormalizedScrollPosition(float normalizedPosition, bool notify = true)
    {
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight(Position.Width);

        var delta = contentHeight - viewportHeight;
        _distanceFromTop = delta * normalizedPosition;
        SetDirty();
    }
}
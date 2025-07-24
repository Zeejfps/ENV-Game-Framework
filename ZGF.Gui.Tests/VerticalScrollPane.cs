using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollPane : View
{
    public event Action<float>? ScrollPositionChanged;

    private float _distanceFromTop;
    private float _maxDistanceFromTop;
    private readonly ColumnView _columnView;

    public float ScrollNormalized { get; private set; }
    public float Scale { get; private set; }
    public override IComponentCollection Children => _columnView.Children;

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

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var childHeight = child.MeasureHeight();
        child.BottomConstraint = position.Top + _distanceFromTop - childHeight;
        child.LeftConstraint = position.Left;            
        child.MinWidthConstraint = position.Width;
        child.MaxWidthConstraint = position.Width;
        child.LayoutSelf();
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
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

    public void Scroll(float delta)
    {
        _distanceFromTop += delta;
        if (_distanceFromTop < 0)
        {
            _distanceFromTop = 0;
        }
        else if (_distanceFromTop > _maxDistanceFromTop)
        {
            _distanceFromTop = _maxDistanceFromTop;
        }
        SetDirty();
    }

    public void ScrollToTop()
    {
        _distanceFromTop = 0;
        SetDirty();
    }
    
    public void ScrollToBottom()
    {
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight();
        
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
        var contentHeight = _columnView.MeasureHeight();
        
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
        var contentHeight = _columnView.MeasureHeight();

        var delta = contentHeight - viewportHeight;
        _distanceFromTop = delta * normalizedPosition;
        SetDirty();
    }
}
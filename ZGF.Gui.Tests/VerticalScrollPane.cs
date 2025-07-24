using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollPane : View
{
    private float _yOffset;
    private float _yMax;
    private readonly ColumnView _columnView;

    public float ScrollNormalized { get; private set; }
    public float Scale { get; private set; }
    public override IComponentCollection Children => _columnView.Children;
    
    public VerticalScrollPane()
    {
        _columnView = new ColumnView();
        AddChildToSelf(_columnView);
    }

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var childHeight = child.MeasureHeight();
        child.BottomConstraint = position.Top + _yOffset - childHeight;
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
        _yOffset += delta;
        if (_yOffset < 0)
        {
            _yOffset = 0;           
        }
        else if (_yOffset > _yMax)
        {
            _yOffset = _yMax;
        }
        SetDirty();
    }

    public void ScrollToTop()
    {
        _yOffset = 0;
        SetDirty();
    }
    
    public void ScrollToBottom()
    {
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight();
        
        if (contentHeight <= viewportHeight)
            return;
        
        var delta = _yOffset + contentHeight - viewportHeight;
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
            _yMax = 0;
            Scale = 1f;
            ScrollNormalized = 0f;
        }
        else
        {
            _yMax = contentHeight - viewportHeight;
            Scale = viewportHeight / contentHeight;

            var scrollOffset = (viewportRect.Bottom - contentRect.Bottom);
            ScrollNormalized = 1f - Math.Clamp(scrollOffset / _yMax, 0f, 1f);
        }
    }

    public void SetNormalizedScrollPosition(float normalizedPosition, bool notify = true)
    {

    }
}
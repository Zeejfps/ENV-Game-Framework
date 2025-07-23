using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalListView : View
{
    private float _yOffset;

    public override IComponentCollection Children => _columnView.Children;
    
    private readonly ColumnView _columnView;

    public VerticalListView()
    {
        _columnView = new ColumnView();
        AddChildToSelf(_columnView);
    }

    public void ScrollUp(float delta)
    {
        Scroll(-delta);
    }
    
    public void ScrollDown(float delta)
    {
        Scroll(delta);
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

    private float _yMax;
    
    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight();
        if (contentHeight <= viewportHeight)
            _yMax = 0;
        else
            _yMax = contentHeight - viewportHeight;
    }

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var height = MeasureHeight();
        child.BottomConstraint = position.Top + _yOffset - height;
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
}
using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalListView : View
{
    private float _yOffset;
    public float YOffset
    {
        get => _yOffset;
        set => SetField(ref _yOffset, value);
    }
    
    public override IComponentCollection Children => _columnView.Children;
    
    private readonly ColumnView _columnView;

    public VerticalListView()
    {
        _columnView = new ColumnView();
        AddChildToSelf(_columnView);
    }

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var height = MeasureHeight();
        child.BottomConstraint = position.Top - _yOffset - height;
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
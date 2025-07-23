namespace ZGF.Gui.Tests;

public sealed class VerticalScrollPane : Component
{
    private float _yOffset;
    public float YOffset
    {
        get => _yOffset;
        set => SetField(ref _yOffset, value);
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        foreach (var child in Children)
        {
            child.LeftConstraint = position.Left;            
            child.MinWidthConstraint = position.Width;
            child.MaxWidthConstraint = position.Width;
            child.LayoutSelf();
        }
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
    }
}

public sealed class ScrollView : Component
{
    private readonly VerticalScrollPane _viewPort;
    
    public Component? Content { get; set; }
    
    public ScrollView()
    {
        _viewPort = new VerticalScrollPane();
        Add(_viewPort);
    }
}
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
            child.BottomConstraint = position.Bottom + _yOffset;
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

    protected override void OnDrawSelf(ICanvas c)
    {
        base.OnDrawSelf(c);
        c.AddCommand(new DrawRectCommand
        {
            Position = Position,
            Style = new RectStyle
            {
                BackgroundColor = 0x00FF00,
            },
            ZIndex = 1
        });
    }
}
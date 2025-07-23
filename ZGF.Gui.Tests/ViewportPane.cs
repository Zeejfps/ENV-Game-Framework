namespace ZGF.Gui.Tests;

public sealed class ViewportPane : Component
{
    private float _xOffset;
    public float XOffset
    {
        get => _xOffset;
        set => SetField(ref _xOffset, value);
    }

    private float _yOffset;
    public float YOffset
    {
        get => _yOffset;
        set => SetField(ref _yOffset, value);
    }
    
    public override float MeasureHeight()
    {
        var minHeight = 0f;
        if (PreferredHeight.IsSet)
        {
            minHeight = PreferredHeight;
        }

        var height = 0f;
        foreach (var child in Children)
        {
            var childHeight = child.MeasureHeight();
            height += childHeight;
        }
        return MathF.Max(minHeight, height);
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        foreach (var child in Children)
        {
            child.LeftConstraint = position.Left + _xOffset;
            child.BottomConstraint = position.Bottom + _yOffset;
            child.LayoutSelf();
        }
    }
}

public sealed class ScrollView : Component
{
    private readonly ViewportPane _viewPort;
    
    public Component? Content { get; set; }
    
    public ScrollView()
    {
        _viewPort = new ViewportPane();
        Add(_viewPort);
    }
}
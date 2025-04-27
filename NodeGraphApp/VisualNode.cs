using NodeGraphApp;

public sealed class VisualNode
{
    public ScreenRect Bounds { get; set; }

    public float Height
    {
        get => Bounds.Height;
        set => Bounds = Bounds with { Height = value };
    }
    
    public Color Color { get; set; }
    public Color BorderColor { get; set; }
    public BorderSizeStyle BorderSize { get; set; }
    public BorderRadiusStyle BorderRadius { get; set; }
    public List<VisualNode> Children { get; } = new();
}
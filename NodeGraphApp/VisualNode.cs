using NodeGraphApp;

public sealed class VisualNode
{
    public ScreenRect Bounds { get; set; }
    public Color Color { get; init; }
    public Color BorderColor { get; init; }
    public BorderSizeStyle BorderSize { get; init; }
    public BorderRadiusStyle BorderRadius { get; init; }
}
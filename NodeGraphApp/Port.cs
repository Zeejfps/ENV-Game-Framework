public sealed class Port
{
    public VisualNode VisualNode { get; }

    public bool IsHovered { get; set; }

    public Port()
    {
        var portBackgroundColor = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f);
        var portBorderColor = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f);
        var portBorderSize = BorderSizeStyle.All(0f);
        var portBorderRadius = BorderRadiusStyle.All(0f);
        var portHeight = 8f;
        
        VisualNode = new VisualNode
        {
            Height = portHeight,
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
    }
}
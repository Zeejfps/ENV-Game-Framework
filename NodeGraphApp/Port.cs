public sealed class Port
{
    private static Color HoveredBorderColor = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f);
    private static Color NormalBorderColor = Color.FromRGBA(0f, 0f, 0f, 1f);
    private static BorderSizeStyle HoveredBorderSize = BorderSizeStyle.All(0.25f);
    private static BorderSizeStyle NormalBorderSize = BorderSizeStyle.All(0f);
    
    public VisualNode VisualNode { get; }

    private bool _isHovered;
    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered == value)
                return;
            _isHovered = value;
            if (_isHovered)
            {
                VisualNode.BorderSize = HoveredBorderSize;
                VisualNode.BorderColor = HoveredBorderColor;
            }
            else
            {
                VisualNode.BorderSize = NormalBorderSize;
                VisualNode.BorderColor = NormalBorderColor;
            }
        }
    }

    public Port()
    {
        var portBackgroundColor = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f);
        var portBorderColor = NormalBorderColor;
        var portBorderSize = NormalBorderSize;
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
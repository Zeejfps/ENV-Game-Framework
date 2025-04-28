using EasyGameFramework.GUI;

namespace NodeGraphApp;

public sealed class Port
{
    private static Color HoveredBorderColor { get; } = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f);
    private static Color NormalBorderColor { get; } = Color.FromRGBA(0f, 0f, 0f, 1f);
    private static BorderSizeStyle HoveredBorderSize { get; } = BorderSizeStyle.All(0.25f);
    private static BorderSizeStyle NormalBorderSize { get; } = BorderSizeStyle.All(0f);
    
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
                _portNode.BorderColor = HoveredBorderColor;
            }
            else
            {
                VisualNode.BorderSize = NormalBorderSize;
                VisualNode.BorderColor = NormalBorderColor;
                _portNode.BorderColor = NormalBorderColor;
            }
        }
    }

    private readonly VisualNode _portNode;
    
    public Port()
    {
        var portBackgroundColor = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f);
        var portBorderColor = NormalBorderColor;
        var portBorderSize = NormalBorderSize;
        var portBorderRadius = BorderRadiusStyle.All(0f);
        var portHeight = 8f;

        var random = new Random();
        _portNode = new VisualNode
        {
            Width = 3f,
            Height = 3f,
            Color = Color.FromRGBA(0.5f, 0.5f, 0.5f, 1.0f),
            BorderColor = Color.FromRGBA(random.NextSingle() * 0.7f, random.NextSingle() * 0.7f, 0.5f, 1.0f),
            BorderRadius = BorderRadiusStyle.All(1.5f),
            BorderSize = BorderSizeStyle.All(0.35f),
        };

        var randomText = new VisualNode
        {
            Text = "Some Port 23!",
            TextVerticalAlignment = TextAlignment.Center
        };
        
        VisualNode = new VisualNode
        {
            Height = portHeight,
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius,
            BoundsChanged = bounds =>
            {
                _portNode.Bounds = _portNode.Bounds with
                {
                    Left = bounds.Left - 1.5f,
                    Bottom = bounds.Bottom + (portHeight - 3f) / 2f,
                };
                randomText.Bounds = bounds with
                {
                    Left = bounds.Left + 2.5f,
                };
            },
        };
        
        VisualNode.Children.Add(_portNode);
        VisualNode.Children.Add(randomText);
    }
}
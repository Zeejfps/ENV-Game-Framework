using NodeGraphApp;

public sealed class Node
{
    private ScreenRect _bounds;
    public ScreenRect Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds == value)
                return;
            _bounds = value;
            _flexColumn.Bounds = value;
            _background.Bounds = value;
        }
    }

    private bool _isHovered;

    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered == value)
                return;
            _isHovered = value;
            
            var borderColor = _isHovered
                ? Color.FromRGBA(0.2f, 0.6f, 0.7333f, 1.0f)
                : Color.FromRGBA(0f, 0f, 0f, 1f);

            _background.BorderColor = borderColor;
        }
    }

    public IEnumerable<VisualNode> VisualNodes => _visualNodes;
    
    private readonly List<VisualNode> _visualNodes = [];
    private readonly FlexColumn _flexColumn;
    private readonly VisualNode _background;
    
    public Node()
    {
        var portBackgroundColor = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f);
        var portBorderColor = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f);
        var portBorderSize = BorderSizeStyle.All(0.25f);
        var portBorderRadius = BorderRadiusStyle.All(1f);

        _background = new VisualNode
        {
            Color = Color.FromRGBA(0.1765f, 0.1922f, 0.2588f, 1f),
            BorderSize = BorderSizeStyle.All(0.25f),
            BorderRadius = BorderRadiusStyle.All(0.25f),
            BorderColor = Color.FromRGBA(0f, 0f, 0f, 1f)
        };
        _visualNodes.Add(_background);
        
        var header = new VisualNode
        {
            Color = Color.FromRGBA(0.2314f, 0.2588f, 0.3412f, 1.0f),
            BorderSize = BorderSizeStyle.FromLTRB(0f, 0f, 0f, 0.25f)
        };
        _visualNodes.Add(header);
        
        var port1 = new VisualNode
        {
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        _visualNodes.Add(port1);
        
        var port2 = new VisualNode
        {
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        _visualNodes.Add(port2);
        
        var port3 = new VisualNode
        {
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        _visualNodes.Add(port3);

        _flexColumn =
        [
            new FlexItem
            {
                FlexGrow = 0,
                BaseHeight = 5f,
                BoundsChanged = bounds => { header.Bounds = bounds; }
            },
            
            new FlexItem
            {
                FlexGrow = 1,
                BoundsChanged = bounds => { port1.Bounds = bounds; }
            },

            new FlexItem
            {
                FlexGrow = 1,
                BoundsChanged = bounds => { port2.Bounds = bounds; }
            },

            new FlexItem
            {
                FlexGrow = 1,
                BoundsChanged = bounds => { port3.Bounds = bounds; }
            }
        ];
    }
}


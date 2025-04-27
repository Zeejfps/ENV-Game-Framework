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
        }
    }
    public bool IsHovered { get; set; }

    public IEnumerable<VisualNode> VisualNodes => _ports;
    
    private readonly List<VisualNode> _ports = [];
    private readonly FlexColumn _flexColumn;
    
    public Node()
    {
        var portBackgroundColor = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f);
        var portBorderColor = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f);
        var portBorderSize = BorderSizeStyle.All(0.25f);
        var portBorderRadius = BorderRadiusStyle.All(1f);

        var header = new VisualNode
        {
            Color = Color.FromRGBA(0.2314f, 0.2588f, 0.3412f, 1.0f),
            BorderSize = BorderSizeStyle.FromLTRB(0f, 0f, 0f, 0.25f)
        };
        _ports.Add(header);
        
        var port1 = new VisualNode
        {
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        _ports.Add(port1);
        
        var port2 = new VisualNode
        {
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        _ports.Add(port2);
        
        var port3 = new VisualNode
        {
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        _ports.Add(port3);

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


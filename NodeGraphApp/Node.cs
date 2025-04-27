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
            VisualNode.Bounds = value;
            _column.Bounds = value;
            _column.DoLayout();
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

            VisualNode.BorderColor = borderColor;
        }
    }
    
    public VisualNode VisualNode { get; }
    
    private readonly Column _column;
    
    public Node(bool extraChild = false)
    {
        VisualNode = new VisualNode
        {
            Color = Color.FromRGBA(0.1765f, 0.1922f, 0.2588f, 1f),
            BorderSize = BorderSizeStyle.All(0.2f),
            BorderRadius = BorderRadiusStyle.All(0.5f),
            BorderColor = Color.FromRGBA(0f, 0f, 0f, 1f)
        };
        
        var header = new VisualNode
        {
            Height = 5f,
            BorderRadius = new BorderRadiusStyle
            {
                TopLeft = 0.25f,
                TopRight = 0.25f,
            },
            Color = Color.FromRGBA(0.2314f, 0.2588f, 0.3412f, 1.0f),
        };
        VisualNode.Children.Add(header);
        
        var portBackgroundColor = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f);
        var portBorderColor = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f);
        var portBorderSize = BorderSizeStyle.All(0f);
        var portBorderRadius = BorderRadiusStyle.All(0f);
        var portHeight = 8f;
        
        var port1 = new VisualNode
        {
            Height = portHeight,
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        VisualNode.Children.Add(port1);
        
        var port2 = new VisualNode
        {
            Height = portHeight,
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius
        };
        VisualNode.Children.Add(port2);
        
        var port3 = new VisualNode
        {
            Height = portHeight,
            Color = portBackgroundColor,
            BorderColor = portBorderColor,
            BorderSize = portBorderSize,
            BorderRadius = portBorderRadius with {BottomLeft = 0.25f, BottomRight = 0.25f}
        };
        VisualNode.Children.Add(port3);
        
        _column = new Column
        {
            BoundsChanged = bounds => { Bounds = bounds; },
            Padding = Padding.All(0.2f),
            ItemGap = 0.25f,
            Items =
            {
                new ColumnItem
                {
                    Bounds = header.Bounds,
                    BoundsChanged = bounds => { header.Bounds = bounds; }
                },
            
                new ColumnItem
                {
                    Bounds = port1.Bounds,
                    BoundsChanged = bounds => { port1.Bounds = bounds; }
                },

                new ColumnItem
                {
                    Bounds = port2.Bounds,
                    BoundsChanged = bounds => { port2.Bounds = bounds; }
                },

                new ColumnItem
                {
                    Bounds = port3.Bounds,
                    BoundsChanged = bounds => { port3.Bounds = bounds; }
                }
            }
        };
        
        if (extraChild)
        {
            var port4 = new VisualNode
            {
                Height = portHeight,
                Color = portBackgroundColor,
                BorderColor = portBorderColor,
                BorderSize = portBorderSize,
                BorderRadius = portBorderRadius with {BottomLeft = 0.25f, BottomRight = 0.25f}
            };
            VisualNode.Children.Add(port4);
            
            _column.Items.Add(new ColumnItem
            {
                Bounds = port4.Bounds,
                BoundsChanged = bounds => { port4.Bounds = bounds; }
            });
        }
    }
}


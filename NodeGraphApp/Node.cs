using NodeGraphApp;
using Column = NodeGraphApp.Column;
using TextAlignment = NodeGraphApp.TextAlignment;

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
    
    public string Title { get; set; }
    public VisualNode VisualNode { get; }
    public List<InputPort> InputPorts { get; } = [];
    public List<OutputPort> OutputPorts { get; } = [];

    private readonly Column _column;

    public Node()
    {
        VisualNode = new VisualNode
        {
            Color = Color.FromRGBA(0.1765f, 0.1922f, 0.2588f, 1f),
            BorderSize = BorderSizeStyle.All(0.2f),
            BorderRadius = BorderRadiusStyle.All(0.5f),
            BorderColor = Color.FromRGBA(0f, 0f, 0f, 1f)
        };

        _column = new Column
        {
            BoundsChanged = bounds => { Bounds = bounds; },
            Padding = Padding.All(0.5f),
            ItemGap = 0.25f,
        };
    }

    public void Update()
    {
        VisualNode.Children.Clear();
        _column.Items.Clear();

        var headerText = new VisualNode
        {
            Text = $"- {Title}",
            TextVerticalAlignment = TextAlignment.Center 
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
            BoundsChanged = bounds => headerText.Bounds = bounds
        };
        VisualNode.Children.Add(header);
        VisualNode.Children.Add(headerText);
        
        _column.Items.Add(new ColumnItem
        {
            Bounds = header.Bounds,
            BoundsChanged = bounds => { header.Bounds = bounds; }
        });
        
        foreach (var port in OutputPorts)
        {
            _column.Items.Add(new ColumnItem
            {
                Bounds = port.Bounds,
                BoundsChanged = bounds => { port.Bounds = bounds; }
            });
            VisualNode.Children.Add(port);
        }
        
        foreach (var port in InputPorts)
        {
            _column.Items.Add(new ColumnItem
            {
                Bounds = port.Bounds,
                BoundsChanged = bounds => { port.Bounds = bounds; }
            });
            VisualNode.Children.Add(port);
        }
        
        _column.DoLayout();
    }
}


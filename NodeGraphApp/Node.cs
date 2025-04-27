using System.Collections;
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
    public List<Port> Ports { get; } = [];

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
            Padding = Padding.All(0.2f),
            ItemGap = 0.25f,
        };
    }

    public void Update()
    {
        VisualNode.Children.Clear();
        _column.Items.Clear();
        
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
        _column.Items.Add(new ColumnItem
        {
            Bounds = header.Bounds,
            BoundsChanged = bounds => { header.Bounds = bounds; }
        });
        
        foreach (var port in Ports)
        {
            _column.Items.Add(new ColumnItem
            {
                Bounds = port.VisualNode.Bounds,
                BoundsChanged = bounds => { port.VisualNode.Bounds = bounds; }
            });
            VisualNode.Children.Add(port.VisualNode);
        }
        
        _column.DoLayout();
    }
}


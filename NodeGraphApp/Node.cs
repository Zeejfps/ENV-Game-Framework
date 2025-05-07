using System.Numerics;

namespace NodeGraphApp;

public sealed class Node : VisualNode
{
    private bool _isHovered;
    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered == value)
                return;
            _isHovered = value;
        }
    }
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
        }
    }

    public string Title
    {
        get => _header?.Text ?? string.Empty;
        set => _header.Text = $"- {value}";
    }
    
    public IEnumerable<InputPort> InputPorts => _inputPorts;
    public IEnumerable<OutputPort> OutputPorts => _outputPorts;
    public Vector2 Position
    {
        get
        { 
            return new Vector2(Bounds.Left, Bounds.Bottom);
        }
        set
        {
            Bounds = Bounds with
            {
                Left = value.X,
                Bottom = value.Y,
            };
        }
    }

    private readonly Column _column;
    private readonly ColumnColumnItem _topColumn;
    
    private readonly List<InputPort> _inputPorts = [];
    private readonly List<OutputPort> _outputPorts = [];

    private readonly VisualNode _header;
    
    public Node()
    {
        Color = Color.FromRGBA(0.1765f, 0.1922f, 0.2588f, 1f);
        BorderSize = BorderSizeStyle.All(0.2f);
        BorderRadius = BorderRadiusStyle.All(0.5f);
        BorderColor = Color.FromRGBA(0f, 0f, 0f, 1f);
        
        _column = new Column
        {
            Padding = Padding.All(0.5f),
        };
        
        _header = new VisualNode
        {
            Height = 5f,
            BorderRadius = new BorderRadiusStyle
            {
                TopLeft = 0.25f,
                TopRight = 0.25f,
            },
            Color = Color.FromRGBA(0.2314f, 0.2588f, 0.3412f, 1.0f),
            TextVerticalAlignment = TextAlignment.Center 
        };
        Hierarchy.AddChild(_header);
        
        _column.AddItem(new VisualNodeColumnItem(_header));

        _topColumn = new ColumnColumnItem(new Column());
        _column.AddItem(_topColumn);
    }

    public InputPort AddInputPort(string name)
    {
        var port = new InputPort(this)
        {
            Name = name
        };
        _inputPorts.Add(port);
        _column.AddItem(new VisualNodeColumnItem(port));
        Hierarchy.AddChild(port);
        return port;
    }
    
    public OutputPort AddOutputPort()
    {
        var port = new OutputPort(this);
        _outputPorts.Add(port);
        _topColumn.Column.AddItem(new VisualNodeColumnItem(port));
        Hierarchy.AddChild(port);
        return port;
    }
    
    public void Update()
    {
        _column.Update();
        Bounds = _column.Bounds;

        if (_isSelected)
        {
            BorderColor = Color.FromRGBA(0.0f, 0.6627f, 0.8784f, 1.0f);
        }
        else if (_isHovered)
        {
            BorderColor = Color.FromRGBA(0.1686f, 0.4863f, 0.5804f, 1.0f);
        }
        else
        {
            BorderColor = Color.FromRGBA(0f, 0f, 0f, 1f);
        }
    }
    
    protected override void OnBoundsChanged()
    {
        _column.Bounds = Bounds;
        _column.Update();
        base.OnBoundsChanged();
    }

    public Node Clone()
    {
        var node = new Node
        {
            Title = Title,
            Bounds = Bounds,
        };

        foreach (var inputPort in _inputPorts)
        {
            node.AddInputPort(inputPort.Name);
        }
        
        foreach (var outputPort in _outputPorts)
        {
            node.AddOutputPort();
        }
        
        return node;
    }
}

public sealed class VisualNodeColumnItem : ColumnItem
{
    public VisualNode Node { get; }
    public VisualNodeColumnItem(VisualNode node)
    {
        Node = node;
    }

    protected override void OnUpdate()
    {
        Bounds = Node.Bounds;
    }

    protected override void OnBoundsChanged()
    {
        Node.Bounds = Bounds;
        base.OnBoundsChanged();
    }
}

public sealed class ColumnColumnItem : ColumnItem
{
    public Column Column { get; }
    
    public ColumnColumnItem(Column column)
    {
        Column = column;
    }
    
    protected override void OnUpdate()
    {
        Column.Update();
        Bounds = Column.Bounds;
    }

    protected override void OnBoundsChanged()
    {
        Column.Bounds = Bounds;
        Column.Update();
        base.OnBoundsChanged();
    }
}
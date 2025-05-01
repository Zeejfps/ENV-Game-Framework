using NodeGraphApp;
using Column = NodeGraphApp.Column;
using TextAlignment = NodeGraphApp.TextAlignment;

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
            
            var borderColor = _isHovered
                ? Color.FromRGBA(0.2f, 0.6f, 0.7333f, 1.0f)
                : Color.FromRGBA(0f, 0f, 0f, 1f);

            BorderColor = borderColor;
        }
    }
    
    public string Title { get; set; }
    public IEnumerable<InputPort> InputPorts => _inputPorts;
    public IEnumerable<OutputPort> OutputPorts => _outputPorts;

    private readonly Column _column;
    private readonly List<InputPort> _inputPorts = [];
    private readonly List<OutputPort> _outputPorts = [];

    public Node()
    {
        Color = Color.FromRGBA(0.1765f, 0.1922f, 0.2588f, 1f);
        BorderSize = BorderSizeStyle.All(0.2f);
        BorderRadius = BorderRadiusStyle.All(0.5f);
        BorderColor = Color.FromRGBA(0f, 0f, 0f, 1f);
        
        _column = new Column
        {
            BoundsChanged = bounds => { Bounds = bounds; },
            Padding = Padding.All(0.5f),
            ItemGap = 0.25f,
        };
    }

    public InputPort AddInputPort()
    {
        var port = new InputPort(this);
        _inputPorts.Add(port);
        return port;
    }
    
    public OutputPort AddOutputPort()
    {
        var port = new OutputPort(this);
        _outputPorts.Add(port);
        return port;
    }
    
    public void Update()
    {
        Children.Clear();
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
        Children.Add(header);
        Children.Add(headerText);
        
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
            Children.Add(port);
        }
        
        foreach (var port in InputPorts)
        {
            _column.Items.Add(new ColumnItem
            {
                Bounds = port.Bounds,
                BoundsChanged = bounds => { port.Bounds = bounds; }
            });
            Children.Add(port);
        }
        
        _column.DoLayout();
    }
    
    protected override void OnBoundsChanged()
    {
        _column.Bounds = Bounds;
        _column.DoLayout();
        base.OnBoundsChanged();
    }
}


namespace NodeGraphApp;

public sealed class InputPort : VisualNode, IPort
{
    private static Color HoveredBorderColor { get; } = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f);
    private static Color NormalBorderColor { get; } = Color.FromRGBA(0f, 0f, 0f, 1f);
    private static BorderSizeStyle HoveredBorderSize { get; } = BorderSizeStyle.All(0.25f);
    private static BorderSizeStyle NormalBorderSize { get; } = BorderSizeStyle.All(0f);
    
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
                BorderSize = HoveredBorderSize;
                BorderColor = HoveredBorderColor;
                Socket.BorderColor = HoveredBorderColor;
            }
            else
            {
                BorderSize = NormalBorderSize;
                BorderColor = NormalBorderColor;
                Socket.BorderColor = NormalBorderColor;
            }
        }
    }

    public string? Name
    {
        get => _nameVisualNode.Text;
        set => _nameVisualNode.Text = value;
    }
    
    public Node Node { get; }
    public VisualNode Socket { get; }

    private readonly VisualNode _nameVisualNode;
    
    public InputPort(Node node)
    {
        Node = node;
        var portBackgroundColor = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f);
        var portBorderColor = NormalBorderColor;
        var portBorderSize = NormalBorderSize;
        var portBorderRadius = BorderRadiusStyle.All(0f);
        var portHeight = 8f;

        var random = new Random();
        Socket = new VisualNode
        {
            Width = 3f,
            Height = 3f,
            Color = Color.FromRGBA(0.5f, 0.5f, 0.5f, 1.0f),
            BorderColor = Color.FromRGBA(random.NextSingle() * 0.7f, random.NextSingle() * 0.7f, 0.5f, 1.0f),
            BorderRadius = BorderRadiusStyle.All(1.5f),
            BorderSize = BorderSizeStyle.All(0.35f),
        };
        
        _nameVisualNode = new VisualNode
        {
            //Text = string.Empty,
            TextVerticalAlignment = TextAlignment.Center
        };

        Height = portHeight;
        Color = portBackgroundColor;
        BorderColor = portBorderColor;
        BorderSize = portBorderSize;
        BorderRadius = portBorderRadius;
        BoundsChanged = bounds =>
        {
            Socket.Bounds = Socket.Bounds with
            {
                Left = bounds.Left - 1.5f,
                Bottom = bounds.Bottom + (portHeight - 3f) / 2f,
            };
            _nameVisualNode.Bounds = bounds with
            {
                Left = bounds.Left + 2.5f,
            };
        };

        Children.Add(_nameVisualNode);
        Children.Add(Socket);
    }
}
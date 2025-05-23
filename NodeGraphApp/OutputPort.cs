namespace NodeGraphApp;

public sealed class OutputPort : VisualNode, IPort
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

    public Node Node { get; }
    public VisualNode Socket { get; }
    
    public OutputPort(Node node)
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

        var randomText = new VisualNode
        {
            Text = "Output Port 23!",
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
                Left = bounds.Right - 1.5f,
                Bottom = bounds.Bottom + (portHeight - 3f) / 2f,
            };
            randomText.Bounds = bounds with
            {
                Left = bounds.Left + 2.5f,
            };
        };
        
        Hierarchy.AddChild(Socket);
        Hierarchy.AddChild(randomText);
    }
}
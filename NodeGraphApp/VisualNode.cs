using System.Numerics;
using NodeGraphApp;
using TextAlignment = NodeGraphApp.TextAlignment;

public sealed class VisualNode
{
    public Action<ScreenRect>? BoundsChanged;

    private ScreenRect _bounds;
    public ScreenRect Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds == value)
                return;
            _bounds = value;
            BoundsChanged?.Invoke(_bounds);
        }
    }

    public float Width
    {
        get => Bounds.Width;
        set => Bounds = Bounds with { Width = value };
    }
    
    public float Height
    {
        get => Bounds.Height;
        set => Bounds = Bounds with { Height = value };
    }

    public string? Text { get; set; }
    public Color Color { get; set; }
    public Color BorderColor { get; set; }
    public BorderSizeStyle BorderSize { get; set; }
    public BorderRadiusStyle BorderRadius { get; set; }
    public IList<VisualNode> Children { get; } = new List<VisualNode>();
    
    public TextAlignment TextVerticalAlignment { get; set; }
    public Vector2 CenterPosition => new(Bounds.Left + Bounds.Width*0.5f, Bounds.Bottom + Bounds.Height*0.5f);
}
using System.Numerics;
using NodeGraphApp;
using TextAlignment = NodeGraphApp.TextAlignment;

public class VisualNode : INode<VisualNode>
{
    public Action<RectF>? BoundsChanged;

    private RectF _bounds;
    public RectF Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds == value)
                return;
            _bounds = value;
            OnBoundsChanged();
        }
    }

    protected virtual void OnBoundsChanged()
    {
        BoundsChanged?.Invoke(_bounds);
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
    public VisualNode? Parent => Hierarchy.Parent;
    public IEnumerable<VisualNode> Children => Hierarchy.Children;
    public SingleParentHierarchy<VisualNode> Hierarchy { get; }
    public TextAlignment TextVerticalAlignment { get; set; }
    public Vector2 CenterPosition => new(Bounds.Left + Bounds.Width*0.5f, Bounds.Bottom + Bounds.Height*0.5f);
    
    public VisualNode()
    {
        Hierarchy = new SingleParentHierarchy<VisualNode>(this);
    }
}
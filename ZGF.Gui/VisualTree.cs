using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class VisualTree
{
    private readonly Dictionary<int, RectStyle> _rectStyleByNodeId = new();
    private readonly Dictionary<int, TextStyle> _textStyleByNodeId = new();

    private int _currentId;

    private List<List<VisualTreeNode>> _layers = new();

    public VisualTree()
    {
        _layers.Add(new List<VisualTreeNode>());
    }

    public void AddRect(RectF position, RectStyle style)
    {
        var node = new VisualTreeNode
        {
            Id = ++_currentId,
            Position = position,
            Kind = VisualTreeNodeKind.Rect
        };
        _rectStyleByNodeId[node.Id] = style;

        var layer = FindLayer(node);
        layer.Add(node);
    }

    private List<VisualTreeNode> FindLayer(VisualTreeNode node)
    {
        foreach (var layer in _layers)
        {
            if (!Intersects(layer, node))
            {
                return layer;
            }
        }

        var newLayer = new List<VisualTreeNode>();
        _layers.Add(newLayer);
        return newLayer;
    }

    private bool Intersects(List<VisualTreeNode> layer, VisualTreeNode node)
    {
        foreach (var treeNode in layer)
        {
            if (treeNode.Position.Intersects(node.Position))
            {
                return true;
            }
        }
        return false;
    }

    public void AddText(RectF position, string text, TextStyle style)
    {

    }

    public void Clear()
    {
        _rectStyleByNodeId.Clear();
        _textStyleByNodeId.Clear();
    }
}

public readonly struct VisualTreeNode
{
    public required int Id { get; init; }
    public required RectF Position { get; init; }
    public required VisualTreeNodeKind Kind { get; init; }
}

public enum VisualTreeNodeKind
{
    Rect,
    Text
}
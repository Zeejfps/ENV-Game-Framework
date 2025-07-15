using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class VisualTree
{
    private readonly Dictionary<string, RectStyle> _rectStyleByNodeId = new();
    private readonly Dictionary<string, TextStyle> _textStyleByNodeId = new();

    private VisualTreeNode _root;

    public VisualTree()
    {
    }

    public void AddRect(RectF position, RectStyle style)
    {

    }

    public void AddText(RectF position, TextStyle style)
    {

    }
}

public sealed class VisualTreeLayer
{

}

public sealed class VisualTreeNode
{
    public string Id { get; set; }

    public RectF Position { get; set; }

    public VisualTreeNodeKind Kind { get; set; }

    public List<VisualTreeNode> Children { get; } = new();
}

public enum VisualTreeNodeKind
{
    Rect,
    Text
}
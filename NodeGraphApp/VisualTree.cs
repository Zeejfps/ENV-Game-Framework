namespace NodeGraphApp;

public sealed class VisualTree
{
    public VisualNode Root { get; }

    public VisualTree(VisualNode root)
    {
        Root = root;
    }

    public IEnumerable<VisualNode> TraverseDepthFirst()
    {
        return TraverseDepthFirst(Root);
    }

    private IEnumerable<VisualNode> TraverseDepthFirst(VisualNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in TraverseDepthFirst(child))
            {
                yield return descendant;
            }
        }
    }
}

public sealed class VisualNode
{
    public float XPos { get; set; }
    public float YPos { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public bool IsHovered { get; set; }
    public IEnumerable<VisualNode> Children => _children;

    private readonly LinkedList<VisualNode> _children = new();

    public void AddChild(VisualNode child)
    {
        _children.AddLast(child);
    }

    public void RemoveChild(VisualNode child)
    {
        _children.Remove(child);
    }
}
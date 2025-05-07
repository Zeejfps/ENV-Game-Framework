using NodeGraphApp;

public sealed class NodeManager
{
    private readonly List<Node> _nodes = new();

    public IEnumerable<Node> GetAll()
    {
        return _nodes;
    }

    public void Add(Node node)
    {
        _nodes.Add(node);
    }

    public void Remove(Node selectedNode)
    {
        _nodes.Remove(selectedNode);
    }

    public void Insert(Node node, int index)
    {
        _nodes.Insert(index, node);
    }

    public void BringToFront(Node selectedNode)
    {
        if (_nodes[^1] == selectedNode)
            return;

        Remove(selectedNode);
        Add(selectedNode);
    }

    public IEnumerable<VisualNode> TraverseDepthFirstPostOrder()
    {
        for (var i = _nodes.Count - 1; i >= 0; --i)
        {
            var node = _nodes[i];
            foreach (var traversed in TraverseDepthFirstPostOrder(node))
            {
                yield return traversed;
            }
        }
    }

    private IEnumerable<VisualNode> TraverseDepthFirstPostOrder(VisualNode node)
    {
        var hierarchy = node.Hierarchy;
        for (var i = hierarchy.ChildrenCount - 1; i >= 0; --i)
        {
            var child = hierarchy[i];
            foreach (var traversed in TraverseDepthFirstPostOrder(child))
            {
                yield return traversed;
            }
        } 
        yield return node;
    }
}
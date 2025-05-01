using System.Collections;

public sealed class NodeRepo
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
        return _nodes.SelectMany(node => TraverseDepthFirstPostOrder(node.VisualNode));
    }

    private IEnumerable<VisualNode> TraverseDepthFirstPostOrder(VisualNode node)
    {
        var children = node.Children;
        for (var i = children.Count - 1; i >= 0; --i)
        {
            var child = children[i];
            foreach (var traversed in TraverseDepthFirstPostOrder(child))
            {
                yield return traversed;
            }
        } 
        yield return node;
    }
}
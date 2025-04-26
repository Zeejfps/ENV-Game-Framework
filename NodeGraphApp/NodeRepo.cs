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
}
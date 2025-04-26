public sealed class NodeRepo
{
    private readonly HashSet<Node> _nodes = new();

    public IEnumerable<Node> GetAll()
    {
        return _nodes;
    }

    public void Add(Node node)
    {
        _nodes.Add(node);
    }
}
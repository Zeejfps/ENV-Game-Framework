public sealed class NodeGraph
{
    public NodeRepo Nodes { get; } = new();
    public LinksRepo Links { get; } = new();
}
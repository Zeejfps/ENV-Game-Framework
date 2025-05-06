namespace NodeGraphApp;

public sealed class NodeGraph
{
    public NodeManager Nodes { get; } = new();
    public LinksRepo BackgroundLinks { get; } = new();
    public LinksRepo ForegroundLinks { get; } = new();
    public ConnectionManager Connections { get; } = new();
    public SelectionBox SelectionBox { get; } = new();

    public void Update()
    {
        foreach (var node in Nodes.GetAll())
        {
            node.Update();
        }
        
        var links = BackgroundLinks.GetAll()
            .Concat(ForegroundLinks.GetAll());
        foreach (var link in links)
        {
            if (Connections.TryGetOutputPortForLink(link, out var outputPort))
            {
                link.StartPosition = outputPort.Socket.CenterPosition;
            }
            if (Connections.TryGetInputPortForLink(link, out var inputPort))
            {
                link.EndPosition = inputPort.Socket.CenterPosition;
            }
        }
    }
}
namespace NodeGraphApp;

public sealed class NodeGraph
{
    public NodeRepo Nodes { get; } = new();
    public LinksRepo BackgroundLinks { get; } = new();
    public LinksRepo ForegroundLinks { get; } = new();
    public ConnectionManager Connections { get; } = new();

    public void Update()
    {
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
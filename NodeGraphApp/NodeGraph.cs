namespace NodeGraphApp;

public sealed class NodeGraph
{
    public NodeRepo Nodes { get; } = new();
    public LinksRepo BackgroundLinks { get; } = new();
    public LinksRepo ForegroundLinks { get; } = new();

    public void Update()
    {
        var links = BackgroundLinks.GetAll();
        foreach (var link in links)
        {
            if (BackgroundLinks.TryGetOutputPortForLink(link, out var outputPort))
            {
                link.StartPosition = outputPort.Socket.CenterPosition;
            }
            if (BackgroundLinks.TryGetInputPortForLink(link, out var inputPort))
            {
                link.EndPosition = inputPort.Socket.CenterPosition;
            }
        }

        var foregroundLinks = ForegroundLinks.GetAll();
        foreach (var link in foregroundLinks)
        {
            if (ForegroundLinks.TryGetOutputPortForLink(link, out var outputPort))
            {
                link.StartPosition = outputPort.Socket.CenterPosition;
            }
            if (ForegroundLinks.TryGetInputPortForLink(link, out var inputPort))
            {
                link.EndPosition = inputPort.Socket.CenterPosition;
            }
        }
    }
}
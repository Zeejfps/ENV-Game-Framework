namespace NodeGraphApp;

public sealed class NodeGraph
{
    public NodeRepo Nodes { get; } = new();
    public LinksRepo Links { get; } = new();
    
    public void Update()
    {
        var links = Links.GetAll();
        foreach (var link in links)
        {
            if (Links.TryGetOutputPortForLink(link, out var outputPort))
            {
                link.StartPosition = outputPort.Socket.CenterPosition;
            }
        }
    }
}
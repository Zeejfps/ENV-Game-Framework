using System.Diagnostics.CodeAnalysis;
using NodeGraphApp;

public sealed class LinksRepo
{
    private readonly HashSet<Link> _links = new();
    
    private Dictionary<Link, InputPort> _inputPortByLinkLookup = new();
    private Dictionary<Link, OutputPort> _outputPortByLinkLookup = new();
    
    public IEnumerable<Link> GetAll()
    {
        return _links;
    }
    
    public void Add(Link link)
    {
        _links.Add(link);
    }
    
    public void Connect(Link link, OutputPort outputPort)
    {
        _links.Add(link);
        _outputPortByLinkLookup[link] = outputPort;
    }

    public bool TryGetOutputPortForLink(Link link, [NotNullWhen(true)] out OutputPort? outputPort)
    {
        return _outputPortByLinkLookup.TryGetValue(link, out outputPort);
    }
}
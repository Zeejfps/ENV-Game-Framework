using System.Diagnostics.CodeAnalysis;
using NodeGraphApp;

public sealed class LinksRepo
{
    private readonly HashSet<Link> _links = new();
    
    private readonly Dictionary<Link, InputPort> _inputPortByLinkLookup = new();
    private readonly Dictionary<Link, OutputPort> _outputPortByLinkLookup = new();
    
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

    public void Connect(Link link, InputPort inputPort)
    {
        _links.Add(link);
        _inputPortByLinkLookup[link] = inputPort;
    }

    public bool TryGetOutputPortForLink(Link link, [NotNullWhen(true)] out OutputPort? outputPort)
    {
        return _outputPortByLinkLookup.TryGetValue(link, out outputPort);
    }

    public bool TryGetInputPortForLink(Link link, [NotNullWhen(true)] out InputPort? inputPort)
    {
        return _inputPortByLinkLookup.TryGetValue(link, out inputPort);
    }

    public void Disconnect(Link link)
    {
        _inputPortByLinkLookup.Remove(link);
        _outputPortByLinkLookup.Remove(link);
    }

    public void Remove(Link newLink)
    {
        _links.Remove(newLink);
    }
}
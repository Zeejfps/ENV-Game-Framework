using System.Diagnostics.CodeAnalysis;

namespace NodeGraphApp;

public sealed class ConnectionManager
{
    private readonly Dictionary<Link, InputPort> _inputPortByLinkLookup = new();
    private readonly Dictionary<Link, OutputPort> _outputPortByLinkLookup = new();

    public void Connect(Link link, OutputPort outputPort)
    {
        _outputPortByLinkLookup[link] = outputPort;
    }

    public void Connect(Link link, InputPort inputPort)
    {
        _inputPortByLinkLookup[link] = inputPort;
    }

    public void Connect(Link link, OutputPort outputPort, InputPort inputPort)
    {
        Connect(link, outputPort);
        Connect(link, inputPort);
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
}
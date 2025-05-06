using System.Diagnostics.CodeAnalysis;

namespace NodeGraphApp;

public sealed class ConnectionManager
{
    private readonly Dictionary<Link, InputPort> _linkByInputPortLookup = new();
    private readonly Dictionary<InputPort, Link> _inputPortByLinkLookup = new();
    private readonly Dictionary<Link, OutputPort> _linkByOutputPortLookup = new();
    private readonly Dictionary<OutputPort, Link> _outputPortByLinkLookup = new();

    public void Connect(Link link, OutputPort outputPort)
    {
        _linkByOutputPortLookup[link] = outputPort;
        _outputPortByLinkLookup[outputPort] = link;
    }

    public void Connect(Link link, InputPort inputPort)
    {
        _linkByInputPortLookup[link] = inputPort;
        _inputPortByLinkLookup[inputPort] = link;
    }

    public void Connect(Link link, OutputPort outputPort, InputPort inputPort)
    {
        Connect(link, outputPort);
        Connect(link, inputPort);
    }

    public bool TryGetOutputPortForLink(Link link, [NotNullWhen(true)] out OutputPort? outputPort)
    {
        return _linkByOutputPortLookup.TryGetValue(link, out outputPort);
    }

    public bool TryGetInputPortForLink(Link link, [NotNullWhen(true)] out InputPort? inputPort)
    {
        return _linkByInputPortLookup.TryGetValue(link, out inputPort);
    }

    public void Disconnect(Link link)
    {
        if (_linkByInputPortLookup.Remove(link, out var inputPort))
            _inputPortByLinkLookup.Remove(inputPort);
        
        if (_linkByOutputPortLookup.Remove(link, out var outputPort))
            _outputPortByLinkLookup.Remove(outputPort);
    }

    public bool TryGetLinkForInputPort(InputPort inputPort, [NotNullWhen(true)] out Link? link)
    {
        return _inputPortByLinkLookup.TryGetValue(inputPort, out link);
    }

    public bool TryGetLinkForOutputPort(OutputPort outputPort, [NotNullWhen(true)] out Link? link)
    {
        return _outputPortByLinkLookup.TryGetValue(outputPort, out link);
    }
}
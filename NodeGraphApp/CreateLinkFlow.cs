using GLFW;

namespace NodeGraphApp;

public sealed class CreateLinkFlow
{
    public bool IsStarted { get; private set; }

    private Link? _newLink;
    private Node? _linkedNode;
    private OutputPort? _linkOutputPort;
    private InputPort? _selectedInputPort;
    
    private readonly MousePicker _mousePicker;
    private readonly NodeGraph _nodeGraph;

    public CreateLinkFlow(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Start(OutputPort outputPort)
    {
        var link = new Link
        {
            EndPosition = _mousePicker.MouseWorldPosition,
        };

        _newLink = link;
        _linkedNode = outputPort.Node;

        _linkOutputPort = outputPort;
        _nodeGraph.ForegroundLinks.Add(link);
        _nodeGraph.Connections.Connect(link, outputPort);

        IsStarted = true;
    }

    public void Update()
    {
        if (_newLink == null)
        {
            IsStarted = false;
            return;
        }
        
        var mouse = _mousePicker.Mouse;
        if (mouse.IsButtonReleased(MouseButton.Left))
        {
            if (_selectedInputPort != null && _linkOutputPort != null)
            {
                _nodeGraph.BackgroundLinks.Add(_newLink);
                _nodeGraph.Connections.Connect(_newLink, _selectedInputPort);
                _selectedInputPort.IsHovered = false;
                _selectedInputPort = null;
            }
            else
            {
                _nodeGraph.Connections.Disconnect(_newLink);
            }

            _nodeGraph.ForegroundLinks.Remove(_newLink);
            _newLink = null;
            IsStarted = false;
            return;
        }

        if (_mousePicker.TryPick<InputPort>(out var inputPort) &&
            inputPort.Node != _linkedNode)
        {
            _newLink.EndPosition = inputPort.Socket.CenterPosition;

            if (_selectedInputPort != null && _selectedInputPort != inputPort)
                _selectedInputPort.IsHovered = false;

            _selectedInputPort = inputPort;
            _selectedInputPort.IsHovered = true;
            return;
        }

        if (_selectedInputPort != null)
        {
            _selectedInputPort.IsHovered = false;
            _selectedInputPort = null;
        }

        var currPos = _mousePicker.MouseWorldPosition;
        _newLink.EndPosition = currPos;
    }
}
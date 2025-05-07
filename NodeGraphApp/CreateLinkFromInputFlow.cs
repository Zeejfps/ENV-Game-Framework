using GLFW;

namespace NodeGraphApp;

public sealed class CreateLinkFromInputFlow
{
    public bool IsStarted { get; private set; }

    private Link? _newLink;
    private Node? _linkedNode;
    private InputPort? _inputPort;
    private OutputPort? _hoveredOutputPort;
    
    private readonly MousePicker _mousePicker;
    private readonly NodeGraph _nodeGraph;

    public CreateLinkFromInputFlow(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Start(InputPort inputPort)
    {
        var link = new Link
        {
            StartPosition = _mousePicker.MouseWorldPosition,
        };

        _newLink = link;
        _linkedNode = inputPort.Node;

        _inputPort = inputPort;
        _nodeGraph.ForegroundLinks.Add(link);
        _nodeGraph.Connections.Connect(link, inputPort);

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
            if (_hoveredOutputPort != null && _inputPort != null)
            {
                _nodeGraph.BackgroundLinks.Add(_newLink);
                _nodeGraph.Connections.Connect(_newLink, _hoveredOutputPort);
                _hoveredOutputPort.IsHovered = false;
                _hoveredOutputPort = null;
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

        if (_mousePicker.TryPick<OutputPort>(out var hoveredOutputPort) &&
            hoveredOutputPort.Node != _linkedNode)
        {
            _newLink.StartPosition = hoveredOutputPort.Socket.CenterPosition;

            if (_hoveredOutputPort != null && _hoveredOutputPort != hoveredOutputPort)
                _hoveredOutputPort.IsHovered = false;

            _hoveredOutputPort = hoveredOutputPort;
            _hoveredOutputPort.IsHovered = true;
            return;
        }

        if (_hoveredOutputPort != null)
        {
            _hoveredOutputPort.IsHovered = false;
            _hoveredOutputPort = null;
        }

        var currPos = _mousePicker.MouseWorldPosition;
        _newLink.StartPosition = currPos;
    }
}
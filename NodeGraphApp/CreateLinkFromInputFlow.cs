using GLFW;

namespace NodeGraphApp;

public sealed class CreateLinkFromInputFlow
{
    public bool IsInProgress { get; private set; }

    private Link? _link;
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

    public void Update()
    {
        var mouse = _mousePicker.Mouse;
        var nodeGraph = _nodeGraph;
        var hoveredInputPort = nodeGraph.HoveredInputPort;

        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            if (hoveredInputPort != null)
            {
                var link = new Link
                {
                    StartPosition = _mousePicker.MouseWorldPosition,
                };

                _link = link;
                _linkedNode = hoveredInputPort.Node;
                _inputPort = hoveredInputPort;
                _nodeGraph.ForegroundLinks.Add(link);
                _nodeGraph.Connections.Connect(link, hoveredInputPort);

                IsInProgress = true;
            }
        }
        
        if (mouse.WasButtonReleasedThisFrame(MouseButton.Left))
        {
            if (IsInProgress)
            {
                var link = _link;
                if (link == null)
                {
                    IsInProgress = false;
                    return;
                }
                
                if (_hoveredOutputPort != null && _inputPort != null)
                {
                    _nodeGraph.BackgroundLinks.Add(link);
                    _nodeGraph.Connections.Connect(link, _hoveredOutputPort);
                    _hoveredOutputPort = null;
                }
                else
                {
                    _nodeGraph.Connections.Disconnect(link);
                }

                _nodeGraph.ForegroundLinks.Remove(link);
                _link = null;
                IsInProgress = false;
                return;
            }
        }

        if (IsInProgress)
        {
            var link = _link;
            if (link == null)
            {
                IsInProgress = false;
                return;
            }
            
            if (_mousePicker.TryPick<OutputPort>(out var outputPort) &&
                outputPort.Node != _linkedNode)
            {
                link.StartPosition = outputPort.Socket.CenterPosition;

                if (_hoveredOutputPort != null && _hoveredOutputPort != outputPort)
                    _hoveredOutputPort.IsHovered = false;

                _hoveredOutputPort = outputPort;
                _hoveredOutputPort.IsHovered = true;
                return;
            }

            if (_hoveredOutputPort != null)
            {
                _hoveredOutputPort.IsHovered = false;
                _hoveredOutputPort = null;
            }

            var currPos = _mousePicker.MouseWorldPosition;
            link.StartPosition = currPos;
        }
    }
}
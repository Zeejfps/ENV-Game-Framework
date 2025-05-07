using GLFW;

namespace NodeGraphApp;

public sealed class CreateLinkFromOutputFlow
{
    public bool IsInProgress { get; private set; }

    private Link? _link;
    private Node? _linkedNode;
    private OutputPort? _outputPort;
    private InputPort? _hoveredInputPort;
    
    private readonly MousePicker _mousePicker;
    private readonly NodeGraph _nodeGraph;

    public CreateLinkFromOutputFlow(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Update()
    {
        var mouse = _mousePicker.Mouse;
        var nodeGraph = _nodeGraph;
        var hoveredOutputPort = nodeGraph.HoveredOutputPort;
        
        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            if (hoveredOutputPort != null)
            {
                var link = new Link
                {
                    EndPosition = _mousePicker.MouseWorldPosition,
                };

                _link = link;
                _linkedNode = hoveredOutputPort.Node;
                _outputPort = hoveredOutputPort;
                _nodeGraph.ForegroundLinks.Add(link);
                _nodeGraph.Connections.Connect(link, hoveredOutputPort);

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
                
                if (_hoveredInputPort != null && _outputPort != null)
                {
                    _nodeGraph.BackgroundLinks.Add(link);
                    _nodeGraph.Connections.Connect(link, _hoveredInputPort);
                    _hoveredInputPort = null;
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
            
            if (_mousePicker.TryPick<InputPort>(out var inputPort) &&
                inputPort.Node != _linkedNode)
            {
                link.EndPosition = inputPort.Socket.CenterPosition;

                if (_hoveredInputPort != null && _hoveredInputPort != inputPort)
                    _hoveredInputPort.IsHovered = false;

                _hoveredInputPort = inputPort;
                _hoveredInputPort.IsHovered = true;
                return;
            }

            if (_hoveredInputPort != null)
            {
                _hoveredInputPort.IsHovered = false;
                _hoveredInputPort = null;
            }

            var currPos = _mousePicker.MouseWorldPosition;
            link.EndPosition = currPos;
        }
    }
}
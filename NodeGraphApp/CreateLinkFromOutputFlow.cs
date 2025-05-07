using GLFW;

namespace NodeGraphApp;

public sealed class CreateLinkFromOutputFlow
{
    public bool IsInProgress { get; private set; }

    private Link? _link;
    private Node? _linkedNode;
    private OutputPort? _outputPort;
    
    private InputPort? _hoveredInputPort;
    private InputPort? HoveredInputPort
    {
        get => _hoveredInputPort;
        set
        {
            if (_hoveredInputPort == value)
                return;
            
            var prevHoveredInputPort = _hoveredInputPort;
            if (prevHoveredInputPort != null)
                prevHoveredInputPort.IsHovered = false;
            
            _hoveredInputPort = value;
            if (_hoveredInputPort != null)
                _hoveredInputPort.IsHovered = true;
        }
    }

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
                
                if (HoveredInputPort != null && _outputPort != null)
                {
                    _nodeGraph.BackgroundLinks.Add(link);
                    _nodeGraph.Connections.Connect(link, HoveredInputPort);
                    HoveredInputPort = null;
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
                HoveredInputPort = inputPort;
                return;
            }

            HoveredInputPort = null;
            var currPos = _mousePicker.MouseWorldPosition;
            link.EndPosition = currPos;
        }
    }
}
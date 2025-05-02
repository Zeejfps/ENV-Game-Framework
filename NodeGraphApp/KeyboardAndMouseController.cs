using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class KeyboardAndMouseController
{
    private readonly NodeGraph _nodeGraph;
    private readonly MousePicker _mousePicker;

    private Link? _hoveredLink = null;

    private Link? HoveredLink
    {
        get => _hoveredLink;
        set
        {
            if (_hoveredLink == value)
                return;

            var prevHoveredLink = _hoveredLink;
            _hoveredLink = value;

            if (prevHoveredLink != null)
                prevHoveredLink.IsHovered = false;

            if (_hoveredLink != null)
                _hoveredLink.IsHovered = true;
        }
    }

    private OutputPort? _hoveredOutputPort;
    private OutputPort? HoveredOutputPort
    {
        get => _hoveredOutputPort;
        set
        {
            if (_hoveredOutputPort == value)
                return;
            var prevHoveredInputPort = _hoveredOutputPort;
            _hoveredOutputPort = value;
            if (prevHoveredInputPort != null)
                prevHoveredInputPort.IsHovered = false;
            if (_hoveredOutputPort != null)
                _hoveredOutputPort.IsHovered = true;
        }
    }
    
    private Node? _hoveredNode;
    private Node? HoveredNode
    {
        get => _hoveredNode;
        set
        {
            if (_hoveredNode == value)
                return;
            var prevHoveredNode = _hoveredNode;
            _hoveredNode = value;
            if (prevHoveredNode != null)
                prevHoveredNode.IsHovered = false;
            if (_hoveredNode != null)
                _hoveredNode.IsHovered = true;
        }
    }
    
    private Vector2 _mousePos;
    private Node? _draggedNode;

    private Link? _newLink;
    private Node? _linkedNode;
    private OutputPort? _linkOutputPort;
    private InputPort? _selectedInputPort;
    
    public KeyboardAndMouseController(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Update()
    {
        if (_newLink != null)
        {
            if (_mousePicker.Mouse.IsButtonReleased(MouseButton.Left))
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
            
            return;
        }
        
        if (_draggedNode != null)
        {
            if (_mousePicker.Mouse.IsButtonReleased(MouseButton.Left))
            {
                _draggedNode = null;
                return;
            }
            
            var currPos = _mousePicker.MouseWorldPosition;
            var delta = currPos - _mousePos;
            _mousePos = currPos;
            var bounds = _draggedNode.Bounds;
            _draggedNode.Bounds = bounds with
            {
                Left = bounds.Left + delta.X,
                Bottom = bounds.Bottom + delta.Y,
            };
            
            return;   
        }

        if (_mousePicker.TryPick<OutputPort>(out var outputPort))
        {
            HoveredOutputPort = outputPort;
            HoveredNode = null;
            HoveredLink = null;
        }
        else if (_mousePicker.TryPick<Node>(out var node))
        {
            HoveredOutputPort = null;
            HoveredNode = node;
            HoveredLink = null;
        }
        else if (_mousePicker.TryPickLink(out var link))
        {
            HoveredNode = null;
            HoveredOutputPort = null;
            HoveredLink = link;
        }
        else
        {
            HoveredNode = null;
            HoveredOutputPort = null;
            HoveredLink = null;
        }

        if (_mousePicker.Mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            if (HoveredOutputPort != null)
            {
                StartCreatingLink(HoveredOutputPort);
            }
            else if (HoveredNode != null)
            {
                StartDraggingNode(HoveredNode);
            }
        }
    }

    private void StartCreatingLink(OutputPort outputPort)
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
    }

    private void StartDraggingNode(Node node)
    {
        _mousePos = _mousePicker.MouseWorldPosition;
        _draggedNode = node;
    }
}
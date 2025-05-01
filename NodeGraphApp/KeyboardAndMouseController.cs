using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class KeyboardAndMouseController
{
    private readonly NodeGraph _nodeGraph;
    private readonly MousePicker _mousePicker;

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
    private InputPort? _selectedInputPort;
    
    public KeyboardAndMouseController(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Update()
    {
        VisualNode? hoveredNode;
        
        if (_newLink != null)
        {
            if (_mousePicker.Mouse.IsButtonReleased(MouseButton.Left))
            {
                if (_selectedInputPort != null)
                {
                    _nodeGraph.Links.Connect(_newLink, _selectedInputPort);
                    _selectedInputPort.IsHovered = false;
                    _selectedInputPort = null;
                }
                else
                {
                    _nodeGraph.Links.Disconnect(_newLink);
                    _nodeGraph.Links.Remove(_newLink);
                }
                
                _newLink = null;
                return;
            }
            
            hoveredNode = _mousePicker.HoveredNode;
            if (hoveredNode != null && 
                hoveredNode.ChildOf<InputPort>(out var inputPort) &&
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
        
        hoveredNode = _mousePicker.HoveredNode;
        if (hoveredNode != null)
        {
            if (hoveredNode.ChildOf<OutputPort>(out var inputPort))
            {
                HoveredOutputPort = inputPort;
                HoveredNode = null;
            }
            else if (hoveredNode.ChildOf<Node>(out var node))
            {
                HoveredOutputPort = null;
                HoveredNode = node;
            }
        }
        else
        {
            HoveredNode = null;
            HoveredOutputPort = null;
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

        _nodeGraph.Links.Add(link);
        _nodeGraph.Links.Connect(link, outputPort);
    }

    private void StartDraggingNode(Node node)
    {
        _mousePos = _mousePicker.MouseWorldPosition;
        _draggedNode = node;
    }
}

public static class VisualNodeExtensions
{
    public static bool ChildOf<T>(this VisualNode node, [NotNullWhen(true)] out T? parentOfType) where T : VisualNode
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is T childOfType)
            {
                parentOfType = childOfType;
                return true;
            }
            parent = parent.Parent;
        }
        parentOfType = null;
        return false;
    }
}
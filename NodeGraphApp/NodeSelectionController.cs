using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class NodeSelectionController
{
    private readonly Mouse _mouse;
    private readonly Viewport _viewport;
    private readonly NodeGraph _nodeGraph;

    public NodeSelectionController(
        Viewport viewport,
        Mouse mouse,
        NodeGraph nodeGraph)
    {
        _viewport = viewport;
        _mouse = mouse;
        _nodeGraph = nodeGraph;
    }

    private Vector2 _mousePos;
    private Node? _selectedNode;

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
    
    private IPort? _hoveredPort;
    private IPort? HoveredPort
    {
        get => _hoveredPort;
        set
        {
            if (_hoveredPort == value)
                return;

            var prevHoveredPort = _hoveredPort;
            _hoveredPort = value;

            if (prevHoveredPort != null)
                prevHoveredPort.IsHovered = false;

            if (_hoveredPort != null)
                _hoveredPort.IsHovered = true;
        }
    }

    public void Update()
    {
        var mouse = _mouse;
        var viewport = _viewport;

        if (_selectedNode != null)
        {
            var currPos = viewport.ScreenToWorldPoint(mouse.Position);
            var delta = currPos - _mousePos;
            _mousePos = currPos;
            var bounds = _selectedNode.Bounds;
            _selectedNode.Bounds = bounds with
            {
                Left = bounds.Left + delta.X,
                Bottom = bounds.Bottom + delta.Y,
            };
        }

        var mousePos = mouse.Position;
        var worldCursorPos = viewport.ScreenToWorldPoint(mousePos);
        var nodes = _nodeGraph.Nodes.GetAll().Reverse();
        Node? hoveredNode = null;
        IPort? hoveredPort = null;
        foreach (var node in nodes)
        {
            foreach (var port in node.OutputPorts)
            {
                if (Overlaps(worldCursorPos, port.Socket.Bounds))
                {
                    hoveredNode = node;
                    hoveredPort = port;
                    break;
                }
            }
            
            if (hoveredNode != null)
                break;
            
            foreach (var port in node.InputPorts)
            {
                if (Overlaps(worldCursorPos, port.Socket.Bounds))
                {
                    hoveredNode = node;
                    hoveredPort = port;
                    break;
                }
            }
            
            if (hoveredNode != null)
                break;
            
            if (Overlaps(worldCursorPos, node))
            {
                hoveredNode = node;
                
                foreach (var port in hoveredNode.OutputPorts)
                {
                    if (Overlaps(worldCursorPos, port.Bounds))
                    {
                        hoveredPort = port;
                        break;
                    }
                }

                if (hoveredPort != null)
                    break;

                foreach (var port in hoveredNode.InputPorts)
                {
                    if (Overlaps(worldCursorPos, port.Bounds))
                    {
                        hoveredPort = port;
                        break;
                    }
                }
                break;
            }
        }
        HoveredNode = hoveredNode;
        HoveredPort = hoveredPort;

        if (mouse.WasButtonPressedThisFrame(MouseButton.Left) && HoveredNode != null)
        {
            _selectedNode = HoveredNode;
            _mousePos = worldCursorPos;
            _nodeGraph.Nodes.BringToFront(_selectedNode);
        }
        else if (mouse.WasButtonReleasedThisFrame(MouseButton.Left))
        {
            _selectedNode = null;
        }
    }

    private bool Overlaps(Vector2 worldCursorPos, Node node)
    {
        var bounds = node.Bounds;
        return Overlaps(worldCursorPos, bounds);
    }
    
    private bool Overlaps(Vector2 worldCursorPos, ScreenRect bounds)
    {
        if (bounds.Right < worldCursorPos.X)
            return false;
        if (bounds.Top < worldCursorPos.Y)
            return false;
        if (bounds.Left > worldCursorPos.X)
            return false;
        if (bounds.Bottom > worldCursorPos.Y)
            return false;

        return true;
    }
}
using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class NodeSelectionController
{
    private readonly Window _window;
    private readonly Mouse _mouse;
    private readonly Camera _camera;
    private readonly NodeGraph _nodeGraph;

    public NodeSelectionController(
        Window window,
        Mouse mouse,
        Camera camera,
        NodeGraph nodeGraph)
    {
        _window = window;
        _mouse = mouse;
        _camera = camera;
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
    
    private Port? _hoveredPort;
    private Port? HoveredPort
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
        var window = _window;
        var mouse = _mouse;
        var camera = _camera;

        if (_selectedNode != null)
        {
            var currPos = CoordinateUtils.ScreenToWorldPoint(window, camera, mouse.Position);;
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
        var worldCursorPos = CoordinateUtils.ScreenToWorldPoint(window, camera, mousePos);
        var nodes = _nodeGraph.Nodes.GetAll().Reverse();
        Node? hoveredNode = null;
        Port? hoveredPort = null;
        foreach (var node in nodes)
        {
            foreach (var port in node.InputPorts)
            {
                if (Overlaps(worldCursorPos, port.PortNode.Bounds))
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

                foreach (var port in hoveredNode.InputPorts)
                {
                    if (Overlaps(worldCursorPos, port.VisualNode.Bounds))
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
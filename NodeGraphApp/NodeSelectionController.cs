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
            _selectedNode.XPos += delta.X;
            _selectedNode.YPos += delta.Y;
        }

        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            var mousePos = mouse.Position;
            var worldCursorPos = CoordinateUtils.ScreenToWorldPoint(window, camera, mousePos);
            var nodes = _nodeGraph.Nodes.GetAll();
            _selectedNode = null;
            foreach (var node in nodes)
            {
                if (node.XPos + node.Width < worldCursorPos.X)
                    continue;
                if (node.YPos + node.Height < worldCursorPos.Y)
                    continue;
                if (node.XPos > worldCursorPos.X)
                    continue;
                if (node.YPos > worldCursorPos.Y)
                    continue;

                _selectedNode = node;
                _mousePos = worldCursorPos;
                break;
            }
        }
        else if (mouse.WasButtonReleasedThisFrame(MouseButton.Left))
        {
            _selectedNode = null;
        }
    }
}
using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class DragNodesFlow
{
    public bool IsStarted { get; private set; }

    private Vector2 _mousePos;

    private readonly MousePicker _mousePicker;
    private readonly NodeGraph _nodeGraph;

    public DragNodesFlow(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Start(Node node)
    {
        _mousePos = _mousePicker.MouseWorldPosition;

        if (!_nodeGraph.IsSelected(node))
        {
            _nodeGraph.ClearSelectedLinks();
            _nodeGraph.ClearSelectedNodes();
            _nodeGraph.SelectNode(node);
        }

        IsStarted = true;
    }

    public void Update()
    {
        var mouse = _mousePicker.Mouse;
        if (mouse.IsButtonReleased(MouseButton.Left))
        {
            IsStarted = false;
            return;
        }

        var currPos = _mousePicker.MouseWorldPosition;
        var delta = currPos - _mousePos;
        _mousePos = currPos;

        foreach (var selectedNode in _nodeGraph.SelectedNodes)
        {
            var bounds = selectedNode.Bounds;
            selectedNode.Bounds = bounds with
            {
                Left = bounds.Left + delta.X,
                Bottom = bounds.Bottom + delta.Y,
            };
        }
    }
}
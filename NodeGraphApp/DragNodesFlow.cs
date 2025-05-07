using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class DragNodesFlow
{
    public bool IsInProgress { get; private set; }

    private Vector2 _mousePos;
    private bool _isMouseDown;

    private readonly MousePicker _mousePicker;
    private readonly NodeGraph _nodeGraph;

    public DragNodesFlow(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Update()
    {
        var mouse = _mousePicker.Mouse;
        var mousePos = _mousePicker.MouseWorldPosition;
        var delta = mousePos - _mousePos;

        if (!IsInProgress && _isMouseDown && _nodeGraph.HoveredNode != null)
        {
            if (_nodeGraph.SelectedNodes.Any())
            {
                if (delta.LengthSquared() > 1f)
                {
                    IsInProgress = true;
                }
            }
        }
        
        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            _mousePos = _mousePicker.MouseWorldPosition;
            _isMouseDown = true;
        }

        if (mouse.WasButtonReleasedThisFrame(MouseButton.Left))
        {
            _isMouseDown = false;
            IsInProgress = false;
        }

        if (IsInProgress)
        {
            _mousePos = mousePos;
            
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
}
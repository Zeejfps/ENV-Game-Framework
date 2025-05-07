using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class BoxSelectFlow
{
    public bool IsInProgress { get; private set; }

    private readonly Mouse _mouse;
    private readonly MousePicker _mousePicker;
    private readonly NodeGraph _nodeGraph;

    private Vector2 _mousePos;
    private bool _isMouseDown;
    private bool _isSelecting;

    public BoxSelectFlow(MousePicker mousePicker, NodeGraph nodeGraph)
    {
        _mouse = mousePicker.Mouse;
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
    }

    public void Update()
    {
        var mouse = _mouse;
        var mousePosition = _mousePicker.MouseWorldPosition;
        var nodeGraph = _nodeGraph;
        var selectionBox = nodeGraph.SelectionBox;
        selectionBox.EndPosition = mousePosition;
        
        if (_isMouseDown && !IsInProgress && 
            _nodeGraph.HoveredNode == null && 
            _nodeGraph.HoveredLink == null && 
            _nodeGraph.HoveredInputPort == null && 
            _nodeGraph.HoveredOutputPort == null)
        {
            var delta = (mousePosition - _mousePos).LengthSquared();
            _mousePos = mousePosition;

            if (delta > 0.1f)
            {
                _isSelecting = true;
                IsInProgress = true;
                _nodeGraph.SelectionBox.Show(_mousePos);
            }
        }

        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            _isMouseDown = true;
            _mousePos = _mousePicker.MouseWorldPosition;
        }
        
        if (mouse.WasButtonReleasedThisFrame(MouseButton.Left))
        {
            _isMouseDown = false;
            if (_isSelecting)
            {
                _isSelecting = false;
                
                var selectionRect = selectionBox.Bounds;

                var nodes = nodeGraph.Nodes.GetAll();
                foreach (var node in nodes)
                {
                    if (selectionRect.Overlaps(node.Bounds))
                    {
                        nodeGraph.SelectNode(node);
                    }
                    else
                    {
                        nodeGraph.DeselectNode(node);
                    }
                }
                
                var links = nodeGraph.BackgroundLinks.GetAll();
                foreach (var link in links)
                {
                    var p0 = link.P0;
                    var p1 = link.P1;
                    var p2 = link.P2;
                    var p3 = link.P3;
                    if (selectionRect.Overlaps(link.Bounds) &&
                        BezierUtils.RectangleOverlapsBezier(p0, p1, p2, p3, selectionRect))
                    {
                        nodeGraph.SelectLink(link);
                    }
                    else
                    {
                        nodeGraph.DeselectLink(link);
                    }
                }

                _isSelecting = false;
            }

            selectionBox.IsVisible = false;
            IsInProgress = false;
        }
    }
}
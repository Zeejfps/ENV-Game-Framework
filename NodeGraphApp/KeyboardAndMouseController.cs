using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class KeyboardAndMouseController
{
    private readonly NodeGraph _nodeGraph;
    private readonly MousePicker _mousePicker;
    private readonly Keyboard _keyboard;

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
    
    private bool _isDragging;
    
    private readonly HashSet<Node> _selectedNodes = new();
    private readonly HashSet<Link> _selectedLinks = new();
    
    private Vector2 _mousePos;
    private Node? _draggedNode;

    private Link? _newLink;
    private Node? _linkedNode;
    private OutputPort? _linkOutputPort;
    private InputPort? _selectedInputPort;
    
    public KeyboardAndMouseController(MousePicker mousePicker, NodeGraph nodeGraph, Keyboard keyboard)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
        _keyboard = keyboard;
    }

    public void Update()
    {
        var mouse = _mousePicker.Mouse;

        if (_isDragging)
        {
            var selectionBox = _nodeGraph.SelectionBox;
            selectionBox.EndPosition = _mousePicker.MouseWorldPosition;
            if (mouse.WasButtonReleasedThisFrame(MouseButton.Left))
            {
                var selectionRect = selectionBox.Bounds;

                var nodes = _nodeGraph.Nodes.GetAll();
                foreach (var node in nodes)
                {
                    if (selectionRect.Overlaps(node.Bounds))
                    {
                        SelectNode(node);
                    }
                    else
                    {
                        DeselectNode(node);
                    }
                }
                
                var links = _nodeGraph.BackgroundLinks.GetAll();
                foreach (var link in links)
                {
                    var p0 = link.P0;
                    var p1 = link.P1;
                    var p2 = link.P2;
                    var p3 = link.P3;
                    if (selectionRect.Overlaps(link.Bounds) &&
                        BezierUtils.RectangleOverlapsBezier(p0, p1, p2, p3, selectionRect))
                    {
                        SelectLink(link);
                    }
                    else
                    {
                        DeselectLink(link);
                    }
                }

                selectionBox.IsVisible = false;
                _isDragging = false;
            }
            return;
        }
        
        if (_newLink != null)
        {
            if (mouse.IsButtonReleased(MouseButton.Left))
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
            if (mouse.IsButtonReleased(MouseButton.Left))
            {
                _draggedNode = null;
                return;
            }

            var currPos = _mousePicker.MouseWorldPosition;
            var delta = currPos - _mousePos;
            _mousePos = currPos;

            foreach (var selectedNode in _selectedNodes)
            {
                var bounds = selectedNode.Bounds;
                selectedNode.Bounds = bounds with
                {
                    Left = bounds.Left + delta.X,
                    Bottom = bounds.Bottom + delta.Y,
                };
            }
            
            return;
        }
        
        if (_selectedLinks.Count > 0 && _keyboard.WasKeyPressedThisFrame(Keys.Delete))
        {
            foreach (var selectedLink in _selectedLinks)
            {
                _nodeGraph.Connections.Disconnect(selectedLink);
                _nodeGraph.BackgroundLinks.Remove(selectedLink);
            }
            ClearSelectedLinks();
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

        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            if (HoveredOutputPort != null)
            {
                StartCreatingLink(HoveredOutputPort);
            }
            else if (HoveredNode != null)
            {
                StartDraggingNode(HoveredNode);
            }
            else if (HoveredLink != null)
            {
                ClearSelectedNodes();
                SelectLink(HoveredLink);
            }
            else
            {
                ClearSelectedNodes();
                ClearSelectedLinks();
                _isDragging = true;
                _mousePos = _mousePicker.MouseWorldPosition;
                _nodeGraph.SelectionBox.Show(_mousePos);
            }
        }
    }

    private void ClearSelectedLinks()
    {
        foreach (var link in _selectedLinks)
        {
            link.IsSelected = false;
        }
        _selectedLinks.Clear();;
    }

    private void SelectLink(Link link)
    {
        if (_selectedLinks.Add(link))
        {
            link.IsSelected = true;
        }
    }

    private void DeselectLink(Link link)
    {
        if (_selectedLinks.Remove(link))
        {
            link.IsSelected = false;
        }
    }

    private void ClearSelectedNodes()
    {
        foreach (var selectedNode in _selectedNodes)
            selectedNode.IsSelected = false;
        _selectedNodes.Clear();
    }

    private void SelectNode(Node node)
    {
        if (_selectedNodes.Add(node))
        {
            node.IsSelected = true;
        }
    }

    private void DeselectNode(Node node)
    {
        if (_selectedNodes.Remove(node))
        {
            node.IsSelected = false;
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

        if (!IsSelected(node))
        {
            ClearSelectedLinks();
            ClearSelectedNodes();
            SelectNode(node);
        }
    }

    private bool IsSelected(Node node)
    {
        return _selectedNodes.Contains(node);
    }
}
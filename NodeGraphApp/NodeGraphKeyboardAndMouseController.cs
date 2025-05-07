using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class NodeGraphKeyboardAndMouseController
{
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
        set => SetHoveredPort(ref _hoveredOutputPort, value);
    }

    private InputPort? _hoveredInputPort;
    private InputPort? HoveredInputPort
    {
        get => _hoveredInputPort;
        set => SetHoveredPort(ref _hoveredInputPort, value);
    }

    private void SetHoveredPort<T>(ref T? port, T? value) where T : class, IPort
    {
        if (port == value)
            return;
        var prevHoveredPort = port;
        port = value;

        if (prevHoveredPort != null)
            prevHoveredPort.IsHovered = false;

        if (port != null)
            port.IsHovered = true;
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
    
    private readonly NodeGraph _nodeGraph;
    private readonly MousePicker _mousePicker;
    private readonly Keyboard _keyboard;
    private readonly Camera _camera;
    private readonly NodeFactory _nodeFactory;
    private readonly CameraDragInputLayer _cameraDragInputLayer;
    private readonly CreateLinkFromOutputFlow _createLinkFromOutputFlow;
    private readonly CreateLinkFromInputFlow _createLinkFromInputFlow;
    
    public NodeGraphKeyboardAndMouseController(
        NodeGraph nodeGraph, 
        Camera camera,
        MousePicker mousePicker,
        Keyboard keyboard, NodeFactory nodeFactory,
        CameraDragInputLayer cameraDragInputLayer)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
        _keyboard = keyboard;
        _nodeFactory = nodeFactory;
        _cameraDragInputLayer = cameraDragInputLayer;
        _camera = camera;
        _createLinkFromOutputFlow = new CreateLinkFromOutputFlow(mousePicker, nodeGraph);
        _createLinkFromInputFlow = new CreateLinkFromInputFlow(mousePicker, nodeGraph);
    }

    public void Update()
    {
        var mouse = _mousePicker.Mouse;
        var keyboard = _keyboard;

        if (mouse.ScrollDelta.Y != 0)
        {
            _camera.ZoomFactor += mouse.ScrollDelta.Y * 0.05f;
        }

        if (_cameraDragInputLayer.ProcessInput())
            return;

        if (keyboard.WasKeyPressedThisFrame(Keys.A))
        {
            var mousePos = _mousePicker.MouseWorldPosition;
            _nodeFactory.CreateNodeAtPosition(mousePos);
        }
        
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

        if (_createLinkFromInputFlow.IsStarted)
        {
            Console.WriteLine("Started");
            _createLinkFromInputFlow.Update();
            return;
        }

        if (_createLinkFromOutputFlow.IsStarted)
        {
            _createLinkFromOutputFlow.Update();
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
        
        if (_keyboard.WasKeyPressedThisFrame(Keys.Delete) && (_selectedLinks.Count > 0 || _selectedNodes.Count > 0))
        {
            foreach (var selectedLink in _selectedLinks)
            {
                _nodeGraph.Connections.Disconnect(selectedLink);
                _nodeGraph.BackgroundLinks.Remove(selectedLink);
            }

            foreach (var selectedNode in _selectedNodes)
            {
                _nodeGraph.Nodes.Remove(selectedNode);
                foreach (var port in selectedNode.InputPorts)
                {
                    if (_nodeGraph.Connections.TryGetLinkForInputPort(port, out var link))
                    {
                        _nodeGraph.Connections.Disconnect(link);
                        _nodeGraph.BackgroundLinks.Remove(link);
                    }
                }
                
                foreach (var port in selectedNode.OutputPorts)
                {
                    if (_nodeGraph.Connections.TryGetLinkForOutputPort(port, out var link))
                    {
                        _nodeGraph.Connections.Disconnect(link);
                        _nodeGraph.BackgroundLinks.Remove(link);
                    }
                }
            }

            ClearSelectedLinks();
            ClearSelectedNodes();
            return;
        }

        if (_mousePicker.TryPick<OutputPort>(out var outputPort))
        {
            HoveredOutputPort = outputPort;
            HoveredInputPort = null;
            HoveredNode = null;
            HoveredLink = null;
        }
        else if (_mousePicker.TryPick<InputPort>(out var inputPort))
        {
            HoveredInputPort = inputPort;
            HoveredOutputPort = null;
            HoveredNode = null;
            HoveredLink = null;
        }
        else if (_mousePicker.TryPick<Node>(out var node))
        {
            HoveredOutputPort = null;
            HoveredInputPort = null;
            HoveredNode = node;
            HoveredLink = null;
        }
        else if (_mousePicker.TryPickLink(out var link))
        {
            HoveredNode = null;
            HoveredOutputPort = null;
            HoveredInputPort = null;
            HoveredLink = link;
        }
        else
        {
            HoveredNode = null;
            HoveredOutputPort = null;
            HoveredInputPort = null;
            HoveredLink = null;
        }

        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            if (HoveredOutputPort != null)
            {
                _createLinkFromOutputFlow.Start(HoveredOutputPort);
            }
            else if (HoveredInputPort != null)
            {
                _createLinkFromInputFlow.Start(HoveredInputPort);
            }
            else if (HoveredNode != null)
            {
                StartDraggingNode(HoveredNode);
            }
            else if (HoveredLink != null)
            {
                ClearSelectedNodes();
                ClearSelectedLinks();
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
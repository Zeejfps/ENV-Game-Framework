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
    
    private Link? _selectedLink = null;
    private Link? SelectedLink
    {
        get => _selectedLink;
        set
        {
            if (_selectedLink == value)
                return;

            var prevSelectedLink = _selectedLink;
            _selectedLink = value;

            if (prevSelectedLink != null)
                prevSelectedLink.IsSelected = false;

            if (_selectedLink != null)
                _selectedLink.IsSelected = true;
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
            if (mouse.WasButtonReleasedThisFrame(MouseButton.Left))
            {
                var endPos = _mousePicker.MouseWorldPosition;
                var left = _mousePos.X;
                if (endPos.X < left)
                    left = endPos.X;
                
                var bottom = _mousePos.Y;
                if (endPos.Y < bottom)
                    bottom = endPos.Y;
                
                var width = MathF.Abs(endPos.X - _mousePos.X);
                var height = MathF.Abs(endPos.Y - _mousePos.Y);

                var selectionRect = ScreenRect.FromLBWH(left, bottom, width, height);
                Console.WriteLine($"Selection Rect: {selectionRect}");

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
            var bounds = _draggedNode.Bounds;
            _draggedNode.Bounds = bounds with
            {
                Left = bounds.Left + delta.X,
                Bottom = bounds.Bottom + delta.Y,
            };

            return;
        }
        
        if (SelectedLink != null && _keyboard.WasKeyPressedThisFrame(Keys.Delete))
        {
            _nodeGraph.Connections.Disconnect(SelectedLink);
            _nodeGraph.BackgroundLinks.Remove(SelectedLink);
            SelectedLink = null;
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
        else if (_mousePicker.TryPickLink(out var link) && link != SelectedLink)
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
                SelectedLink = HoveredLink;
                HoveredLink = null;
            }
            else if (SelectedLink != null)
            {
                SelectedLink = null;
            }
            else
            {
                _isDragging = true;
                _mousePos = _mousePicker.MouseWorldPosition;
            }
        }
    }

    private void SelectNode(Node node)
    {
        if (_selectedNodes.Add(node))
        {
            node.IsSelected = true;
            Console.WriteLine("Selected node");
        }
    }

    private void DeselectNode(Node node)
    {
        if (_selectedNodes.Remove(node))
        {
            node.IsSelected = false;
            Console.WriteLine("Deselected node");
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
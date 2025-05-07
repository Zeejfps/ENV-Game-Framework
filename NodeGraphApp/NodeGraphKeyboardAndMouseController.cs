using GLFW;

namespace NodeGraphApp;

public sealed class NodeGraphKeyboardAndMouseController
{
    private readonly NodeGraph _nodeGraph;
    private readonly MousePicker _mousePicker;
    private readonly Keyboard _keyboard;
    private readonly Camera _camera;
    private readonly NodeFactory _nodeFactory;
    private readonly CameraDragFlow _cameraDragFlow;
    private readonly CreateLinkFromOutputFlow _createLinkFromOutputFlow;
    private readonly CreateLinkFromInputFlow _createLinkFromInputFlow;
    private readonly DragNodesFlow _dragNodesFlow;
    private readonly BoxSelectFlow _boxSelectFlow;

    private Link? HoveredLink
    {
        get => _nodeGraph.HoveredLink;
        set => _nodeGraph.HoveredLink = value;
    }
    
    private Node? HoveredNode
    {
        get => _nodeGraph.HoveredNode;
        set => _nodeGraph.HoveredNode = value;
    }
    
    private InputPort? HoveredInputPort
    {
        get => _nodeGraph.HoveredInputPort;
        set => _nodeGraph.HoveredInputPort = value;
    }
    
    private OutputPort? HoveredOutputPort
    {
        get => _nodeGraph.HoveredOutputPort;
        set => _nodeGraph.HoveredOutputPort = value;
    }
    
    public NodeGraphKeyboardAndMouseController(
        NodeGraph nodeGraph, 
        Camera camera,
        MousePicker mousePicker,
        Keyboard keyboard, NodeFactory nodeFactory,
        CameraDragFlow cameraDragFlow)
    {
        _mousePicker = mousePicker;
        _nodeGraph = nodeGraph;
        _keyboard = keyboard;
        _nodeFactory = nodeFactory;
        _cameraDragFlow = cameraDragFlow;
        _camera = camera;
        _createLinkFromOutputFlow = new CreateLinkFromOutputFlow(mousePicker, nodeGraph);
        _createLinkFromInputFlow = new CreateLinkFromInputFlow(mousePicker, nodeGraph);
        _dragNodesFlow = new DragNodesFlow(mousePicker, nodeGraph);
        _boxSelectFlow = new BoxSelectFlow(mousePicker, nodeGraph);
    }

    public void Update()
    {
        var mouse = _mousePicker.Mouse;
        var keyboard = _keyboard;
        var nodeGraph = _nodeGraph;

        if (mouse.ScrollDelta.Y != 0)
        {
            _camera.ZoomFactor += mouse.ScrollDelta.Y * 0.05f;
        }

        _boxSelectFlow.Update();
        _createLinkFromInputFlow.Update();
        _createLinkFromOutputFlow.Update();
        _dragNodesFlow.Update();
        _cameraDragFlow.Update();
        
        if (keyboard.WasKeyPressedThisFrame(Keys.A))
        {
            var mousePos = _mousePicker.MouseWorldPosition;
            _nodeFactory.CreateNodeAtPosition(mousePos);
        }
        
        if (_cameraDragFlow.IsInProgress)
            return;
        
        if (_boxSelectFlow.IsInProgress)
            return;
        
        if (_createLinkFromInputFlow.IsInProgress)
            return;

        if (_createLinkFromOutputFlow.IsInProgress)
            return;

        if (_dragNodesFlow.IsInProgress)
            return;
        
        if ((_keyboard.WasKeyPressedThisFrame(Keys.Delete) || _keyboard.WasKeyPressedThisFrame(Keys.Backspace)) 
            && (nodeGraph.SelectedNodes.Any() || nodeGraph.SelectedLinks.Any()))
        {
            foreach (var selectedLink in nodeGraph.SelectedLinks)
            {
                _nodeGraph.Connections.Disconnect(selectedLink);
                _nodeGraph.BackgroundLinks.Remove(selectedLink);
            }

            foreach (var selectedNode in nodeGraph.SelectedNodes)
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
            }
            else if (HoveredInputPort != null)
            {
            }
            else if (HoveredNode != null)
            {
                if (!_nodeGraph.IsSelected(HoveredNode))
                {
                    ClearSelectedLinks();
                    ClearSelectedNodes();
                    SelectNode(HoveredNode);
                }
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
            }
        }
    }

    private void ClearSelectedLinks()
    {
        _nodeGraph.ClearSelectedLinks();
    }

    private void SelectLink(Link link)
    {
        _nodeGraph.SelectLink(link);
    }

    private void ClearSelectedNodes()
    {
        _nodeGraph.ClearSelectedNodes();
    }

    private void SelectNode(Node node)
    {
        _nodeGraph.SelectNode(node);
    }
}
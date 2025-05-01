
using GLFW;

namespace NodeGraphApp;

public sealed class LinkPlacementController
{
    private readonly Viewport _viewport;
    private readonly Mouse _mouse;
    private readonly PortPicker _portPicker;
    private readonly NodeGraph _nodeGraph;
    public Link? Link { get; set; }

    public LinkPlacementController(
        Viewport viewport,
        Mouse mouse,
        PortPicker portPicker,
        NodeGraph nodeGraph)
    {
        _viewport = viewport;
        _mouse = mouse;
        _portPicker = portPicker;
        _nodeGraph = nodeGraph;
    }

    public void Update()
    {
        var link = Link;
        if (link == null)
            return;

        var mousePosition = _mouse.Position;
        if (!_viewport.ContainsScreenPoint(mousePosition))
        {
            return;
        }

        Node? ourNode = null;
        if (_nodeGraph.Links.TryGetOutputPortForLink(link, out var outputPort))
        {
            ourNode = outputPort.Node;
        }

        var hoveredInputPort = _portPicker.HoveredInputPort;
        if (hoveredInputPort != null && hoveredInputPort.Node != ourNode)
        {
            link.EndPosition = hoveredInputPort.Socket.CenterPosition;

            if (_mouse.WasButtonPressedThisFrame(MouseButton.Left))
            {
                _nodeGraph.Links.Connect(link, hoveredInputPort);
                Link = null;
            }

            return;
        }

        var worldPosition = _viewport.ScreenToWorldPoint(mousePosition);
        link.EndPosition = worldPosition;
    }
}
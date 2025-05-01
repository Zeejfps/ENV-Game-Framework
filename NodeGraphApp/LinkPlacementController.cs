
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

        if (_portPicker.TryPickInputPort(mousePosition, out var inputPort))
        {
            link.EndPosition = inputPort.Socket.CenterPosition;

            if (_mouse.WasButtonPressedThisFrame(MouseButton.Left))
            {
                _nodeGraph.Links.Connect(link, inputPort);
                Link = null;
            }

            return;
        }

        var worldPosition = _viewport.ScreenToWorldPoint(mousePosition);
        link.EndPosition = worldPosition;
    }
}
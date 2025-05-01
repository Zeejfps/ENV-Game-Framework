
namespace NodeGraphApp;

public sealed class LinkPlacementController
{
    private readonly Viewport _viewport;
    private readonly Mouse _mouse;
    private readonly PortPicker _portPicker;
    private readonly Link _link;

    public LinkPlacementController(Viewport viewport, Mouse mouse, Link link, PortPicker portPicker)
    {
        _viewport = viewport;
        _mouse = mouse;
        _link = link;
        _portPicker = portPicker;
    }

    public void Update()
    {
        var mousePosition = _mouse.Position;
        if (!_viewport.ContainsScreenPoint(mousePosition))
        {
            return;
        }

        if (_portPicker.TryPickInputPort(mousePosition, out var port))
        {
            _link.EndPosition = port.Socket.CenterPosition;
            return;
        }

        var worldPosition = _viewport.ScreenToWorldPoint(mousePosition);
        _link.EndPosition = worldPosition;
    }
}
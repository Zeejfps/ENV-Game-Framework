
namespace NodeGraphApp;

public sealed class LinkPlacementController
{
    private readonly Viewport _viewport;
    private readonly Mouse _mouse;
    private readonly Link _link;

    public LinkPlacementController(Viewport viewport, Mouse mouse, Link link)
    {
        _viewport = viewport;
        _mouse = mouse;
        _link = link;
    }

    public void Update()
    {
        var mousePosition = _mouse.Position;
        if (!_viewport.ContainsScreenPoint(mousePosition))
            return;
        
        var worldPosition = _viewport.ScreenToWorldPoint(mousePosition);
        _link.EndPosition = worldPosition;
    }
}
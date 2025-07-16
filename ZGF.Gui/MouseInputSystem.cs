using ZGF.Geometry;

namespace ZGF.Gui;

public interface IHoverable
{
    RectF Position { get;  }
    int ZIndex { get; }
    bool IsInFrontOf(IHoverable hoverable);
    void HandleMouseEnterEvent();
    void HandleMouseExitEvent();
}

public interface IMouseFocusable
{
    void HandleMouseButtonEvent(in MouseButtonEvent e);
    void HandleMouseWheelEvent();
    void HandleMouseMoveEvent(in MouseMoveEvent e);
}

public readonly struct MouseButtonEvent
{
    public required PointF Position { get; init; }
    public required MouseButton Button { get; init; }
    public required InputState State { get; init; }
}

public readonly struct MouseMoveEvent
{
    public required PointF MousePosition { get; init; }
}

public sealed class MouseInputSystem
{
    private readonly HashSet<IHoverable> _hoverableComponents = new();
    
    private IHoverable? _hoveredComponent;
    private IMouseFocusable? _focusedComponent;
    
    public void EnableHover(IHoverable hoverable)
    {
        _hoverableComponents.Add(hoverable);
    }

    public void HandleMouseButtonEvent(MouseButtonEvent e)
    {
        if (_focusedComponent != null)
        {
            _focusedComponent.HandleMouseButtonEvent(e);
        }
    }

    public void UpdateMousePosition(in PointF point)
    {
        var newHoveredComponent = HitTest(point);
        if (newHoveredComponent != _hoveredComponent)
        {
            var prevHoveredComponent = _hoveredComponent;
            _hoveredComponent = newHoveredComponent;

            if (prevHoveredComponent != null &&
                _hoverableComponents.TryGetValue(prevHoveredComponent, out var listener))
            {
                listener.HandleMouseExitEvent();
            }

            if (_hoveredComponent != null &&
                _hoverableComponents.TryGetValue(_hoveredComponent, out listener))
            {
                listener.HandleMouseEnterEvent();           
            }
        }

        if (_focusedComponent != null)
        {
            _focusedComponent.HandleMouseMoveEvent(new MouseMoveEvent
            {
                MousePosition = point
            });
        }
    }

    private readonly List<IHoverable> _hitTestCache = new();
    
    private IHoverable? HitTest(in PointF point)
    {
        _hitTestCache.Clear();
        var components = _hitTestCache;
        foreach (var component in _hoverableComponents)
        {
            if (component.Position.ContainsPoint(point))
            {
                components.Add(component);
            }
        }

        if (components.Count == 0)
            return null;
        
        _hitTestCache.Sort(ZIndexComparer.Instance);
        return components[0];
    }

    public void Focus(IMouseFocusable focusHandler)
    {
        _focusedComponent = focusHandler;
    }

    public bool TryFocus(IMouseFocusable captureMouse)
    {
        if (_focusedComponent == null)
        {
            _focusedComponent = captureMouse;
            return true;
        }

        return false;
    }

    public void Blur(IMouseFocusable captureMouse)
    {
        if (_focusedComponent == captureMouse)
        {
            _focusedComponent = null;       
        }
    }

    public void DisableHover(IHoverable hoverable)
    {
        _hoverableComponents.Remove(hoverable);
    }
}

sealed class ZIndexComparer : IComparer<IHoverable>
{
    public static ZIndexComparer Instance { get; } = new();

    public int Compare(IHoverable? x, IHoverable? y)
    {
        if (x == null && y == null)
            return 0;

        if (x == null)
            return 1;

        if (y == null)
            return -1;

        var result = x.ZIndex.CompareTo(y.ZIndex);
        if (result == 0)
        {
            if (x.IsInFrontOf(y))
            {
                return -1;
            }
            return 1;
        }
        return result;
    }
}
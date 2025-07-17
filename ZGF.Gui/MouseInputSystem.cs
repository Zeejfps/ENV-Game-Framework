using ZGF.Geometry;

namespace ZGF.Gui;

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
    private readonly HashSet<Component> _hoverableComponents = new();
    
    private Component? _hoveredComponent;
    private Component? _focusedComponent;
    
    public void EnableHover(Component hoverable)
    {
        _hoverableComponents.Add(hoverable);
    }

    public void DisableHover(Component hoverable)
    {
        _hoverableComponents.Remove(hoverable);
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

            if (prevHoveredComponent != null)
            {
                prevHoveredComponent.HandleMouseExitEvent();
            }

            if (_hoveredComponent != null)
            {
                _hoveredComponent.HandleMouseEnterEvent();           
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

    private readonly List<Component> _hitTestCache = new();
    
    private Component? HitTest(in PointF point)
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

    public void Focus(Component focusHandler)
    {
        _focusedComponent = focusHandler;
    }

    public bool TryFocus(Component captureMouse)
    {
        if (_focusedComponent == null)
        {
            _focusedComponent = captureMouse;
            return true;
        }

        return false;
    }

    public void Blur(Component captureMouse)
    {
        if (_focusedComponent == captureMouse)
        {
            _focusedComponent = null;       
        }
    }
}

sealed class ZIndexComparer : IComparer<Component>
{
    public static ZIndexComparer Instance { get; } = new();

    public int Compare(Component? x, Component? y)
    {
        if (x == null && y == null)
            return 0;

        if (x == null)
            return 1;

        if (y == null)
            return -1;

        var result = y.ZIndex.CompareTo(x.ZIndex);
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
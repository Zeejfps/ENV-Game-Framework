using ZGF.Geometry;

namespace ZGF.Gui;

public interface IHoverable
{
    void HandleMouseEnterEvent();
    void HandleMouseExitEvent();
}

public interface IMouseFocusable
{
    void HandleMouseButtonEvent(MouseButton button, InputState state);
    void HandleMouseWheelEvent();
    void HandleMouseMoveEvent();
}

public sealed class MouseInputSystem
{
    public static MouseInputSystem Instance { get; } = new();

    private readonly Dictionary<Component, IHoverable> _hoverableComponents = new();
    
    private Component? _hoveredComponent;
    private IMouseFocusable? _focusedComponent;
    
    public void EnableHover(Component component, IHoverable listener)
    {
        _hoverableComponents[component] = listener;
    }

    public void HandleMouseButtonEvent(MouseButton button, InputState state)
    {
        if (_focusedComponent != null)
        {
            _focusedComponent.HandleMouseButtonEvent(button, state);
        }
    }

    public void UpdateMousePosition(int x, int y)
    {
        var newHoveredComponent = HitTest(x, y);
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
    }

    private readonly List<Component> _hitTestCache = new();
    
    private Component? HitTest(int x, int y)
    {
        _hitTestCache.Clear();
        var components = _hitTestCache;
        var hitPoint = new PointF(x, y);
        foreach (var component in _hoverableComponents.Keys)
        {
            if (component.Position.ContainsPoint(hitPoint))
            {
                components.Add(component);
            }
        }

        if (components.Count == 0)
            return null;
        
        _hitTestCache.Sort(ZIndexComparer.Instance);
        return components[0];
    }

    public void Focus(Component component, IMouseFocusable captureMouse)
    {
        _focusedComponent = captureMouse;
    }

    public void Blur(Component component, IMouseFocusable captureMouse)
    {
        if (_focusedComponent == captureMouse)
        {
            _focusedComponent = null;       
        }
    }

    public void DisableHover(Component component)
    {
        _hoverableComponents.Remove(component);
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
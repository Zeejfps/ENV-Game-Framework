using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class InputSystem
{
    private readonly HashSet<Component> _hoverableComponents = new();
    
    private Component? _hoveredComponent;
    private Component? _focusedComponent;
    
    public void AddInteractable(Component hoverable)
    {
        _hoverableComponents.Add(hoverable);
    }

    public void RemoveInteractable(Component hoverable)
    {
        if (_hoverableComponents.Remove(hoverable))
        {
            if (_focusedComponent == hoverable)
            {
                _focusedComponent = null;
            }
        }
    }

    public void HandleKeyboardKeyEvent(in KeyboardKeyEvent e)
    {
        if (_focusedComponent != null)
        {
            _focusedComponent.HandleKeyboardKeyEvent(e);
        }
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

    public void StealFocus(Component focusHandler)
    {
        var prevFocusedComponent = _focusedComponent;
        _focusedComponent = focusHandler;

        if (prevFocusedComponent != null)
        {
            prevFocusedComponent.HandleFocusLost();
        }

        if (_focusedComponent != null)
        {
            _focusedComponent.HandleFocusGained();
        }
    }

    public bool TryFocus(Component captureMouse)
    {
        if (_focusedComponent == null)
        {
            _focusedComponent = captureMouse;
            captureMouse.HandleFocusGained();
            return true;
        }

        return false;
    }

    public void Blur(Component captureMouse)
    {
        if (_focusedComponent == captureMouse)
        {
            _focusedComponent = null;
            captureMouse.HandleFocusLost();
        }
    }

    public bool IsInteractable(Component component)
    {
        return _hoverableComponents.Contains(component);
    }

    public bool IsFocused(Component component)
    {
        return _focusedComponent == component;
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
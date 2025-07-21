using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class InputSystem
{
    private readonly HashSet<Component> _hoverableComponents = new();
    
    private Component? _hoveredComponent;

    private readonly LinkedList<Component> _focusQueue = new();
    
    public void AddInteractable(Component hoverable)
    {
        _hoverableComponents.Add(hoverable);
    }

    public void RemoveInteractable(Component hoverable)
    {
        _hoverableComponents.Remove(hoverable);
        _focusQueue.Remove(hoverable);
    }

    private readonly List<Component> _focusQueueCache = new();
    
    public void HandleKeyboardKeyEvent(in KeyboardKeyEvent e)
    {
        _focusQueueCache.Clear();
        _focusQueueCache.AddRange(_focusQueue);

        foreach (var target in _focusQueueCache)
        {
            var handled = target.HandleKeyboardKeyEvent(e);
            if (handled)
                break;
        }
    }

    public void HandleMouseButtonEvent(MouseButtonEvent e)
    {
        _focusQueueCache.Clear();
        _focusQueueCache.AddRange(_focusQueue);

        foreach (var target in _focusQueueCache)
        {
            var handled = target.HandleMouseButtonEvent(e);
            if (handled)
                break;
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
        
        _focusQueueCache.Clear();
        _focusQueueCache.AddRange(_focusQueue);

        var e = new MouseMoveEvent
        {
            MousePosition = point,
        };
        foreach (var target in _focusQueueCache)
        {
            var handled = target.HandleMouseMoveEvent(e);
            if (handled)
                break;
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
        
        // Console.WriteLine($"Hit: {components.Count}");
        foreach (var component in components)
        {
            Console.WriteLine(component);
        }
        
        components.Sort(ZIndexComparer.Instance);
        // Console.WriteLine($"Sorted: {components.Count}");
        foreach (var component in components)
        {
            Console.WriteLine(component);
        }
        
        return components[0];
    }

    public void StealFocus(Component component)
    {
        var prevFocusedComponent = _focusQueue.First?.Value;
        _focusQueue.AddFirst(component);
        
        if (prevFocusedComponent != null)
        {
            prevFocusedComponent.HandleFocusLost();
        }
        
        component.HandleFocusGained();
    }

    public bool RequestFocus(Component component)
    {
        var focusedComponent = _focusQueue.First?.Value;
        if (focusedComponent == null)
        {
            _focusQueue.AddFirst(component);
            component.HandleFocusGained();
            return true;
        }

        if (!_focusQueue.Contains(component))
            _focusQueue.AddLast(component);
        
        return false;
    }
    
    public void Blur(Component component)
    {
        var focusedComponent = _focusQueue.First?.Value;
        if (focusedComponent == component)
        {
            _focusQueue.RemoveFirst();
            component.HandleFocusLost();

            var nextFocus = _focusQueue.First?.Value;
            if (nextFocus != null)
            {
                nextFocus.HandleFocusGained();
            }
        }
        else
        {
            _focusQueue.Remove(component);
        }
    }

    public bool IsInteractable(Component component)
    {
        return _hoverableComponents.Contains(component);
    }

    public bool IsFocused(Component component)
    {
        var focused = _focusQueue.First;
        if (focused == null)
            return false;
        return focused.Value == component;
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
        
        // NOTE(Zee): Order is swapped here. Greater ZIndex means the value is less - meaning it should be first in list 
        var result = y.ZIndex.CompareTo(x.ZIndex);
        if (result == 0)
        {
            if (x.IsInFrontOf(y))
            {
                return -1;
            }

            if (y.IsInFrontOf(x))
            {
                return 1;
            }
            
            return 0;
        }
        return result;
    }
}
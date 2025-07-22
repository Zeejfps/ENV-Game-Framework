using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class InputSystem
{
    private readonly HashSet<Component> _hoverableComponents = new();
    
    private readonly HashSet<Component> _hoveredComponents = new();
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
    
    public void HandleKeyboardKeyEvent(in KeyboardKeyEvent e)
    {
        foreach (var target in _focusQueue)
        {
            var handled = target.HandleKeyboardKeyEvent(e);
            if (handled)
                break;
        }
    }

    public void HandleMouseButtonEvent(MouseButtonEvent e)
    {
        Console.WriteLine($"HandleMouseButtonEvent: {_focusQueue.First?.Value}");
        foreach (var target in _focusQueue)
        {
            var handled = target.HandleMouseButtonEvent(e);
            if (handled)
                break;
        }
    }

    private readonly List<Component> _removeCache = new();
    
    public void UpdateMousePosition(in PointF point)
    {
        _removeCache.Clear();
        _removeCache.AddRange(_hoveredComponents);

        for (var i = _removeCache.Count - 1; i >= 0; i--)
        {
            var hoveredComponent = _removeCache[i];
            if (!hoveredComponent.Position.ContainsPoint(point))
            {
                _hoveredComponents.Remove(hoveredComponent);
                hoveredComponent.HandleMouseExitEvent();
            }
        }
        
        var allHoveredComponents = HitTest(point);
        if (allHoveredComponents.Count > 0)
        {
            var topComponent = allHoveredComponents[0];
            for (var i = allHoveredComponents.Count - 1; i >= 0; i--)
            {
                var hoveredComponent = allHoveredComponents[i];
                if (hoveredComponent.IsAncestorOf(topComponent) && _hoveredComponents.Add(hoveredComponent))
                {
                    hoveredComponent.HandleMouseEnterEvent();
                }
            }
        }
        
        var e = new MouseMoveEvent
        {
            MousePoint = point,
        };

        foreach (var target in _focusQueue)
        {
            var handled = target.HandleMouseMoveEvent(e);
            if (handled)
                break;
        }
    }

    private readonly List<Component> _hitTestCache = new();
    
    private List<Component> HitTest(in PointF point)
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
            return components;
        
        components.Sort(ZIndexComparer.Instance);
        // Console.WriteLine("===================");
        // foreach (var component in components)
        // {
        //     Console.WriteLine(component);
        // }
        
        return components;
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

    private readonly List<Component> _componentsToAddToFocusQueue = new();
    private readonly List<Component> _componentsToRemoveFromFocusQueue = new();
    
    public void Update()
    {
        var focusedComponent = _focusQueue.First?.Value;
        var canReleaseFocus = focusedComponent?.CanReleaseFocus() ?? true;
        foreach (var component in _componentsToRemoveFromFocusQueue)
        {
            _focusQueue.Remove(component);
            _componentsToAddToFocusQueue.Remove(component);
            if (component == focusedComponent)
            {
                canReleaseFocus = true;
            }
        }
        _componentsToRemoveFromFocusQueue.Clear();
        
        foreach (var component in _componentsToAddToFocusQueue)
        {
            if (!_focusQueue.Contains(component))
            {
                Console.WriteLine($"Handling focus request: {component}");
                if (canReleaseFocus || _focusQueue.Count == 0)
                    _focusQueue.AddFirst(component);
                else
                    _focusQueue.AddAfter(_focusQueue.First!, component);
            }
        }
        _componentsToAddToFocusQueue.Clear();
        
        var newFocusedComponent = _focusQueue.First?.Value;
        if (focusedComponent != newFocusedComponent)
        {
            Console.WriteLine($"Focus changing: {focusedComponent} -> {newFocusedComponent}");
            if (focusedComponent != null)
            {
                focusedComponent.HandleFocusLost();
            }
            
            if (newFocusedComponent != null)
            {
                newFocusedComponent.HandleFocusGained();
                Console.WriteLine($"Focused: {newFocusedComponent}");
            }
        }
    }

    public bool RequestFocus(Component component)
    {
        Console.WriteLine($"Requeting focus: {component}");
        _componentsToAddToFocusQueue.Add(component);
        
        // var focusedComponent = _focusQueue.First?.Value;
        // if (focusedComponent == null)
        // {
        //     _focusQueue.AddFirst(component);
        //     component.HandleFocusGained();
        //     return true;
        // }
        //
        // if (!_focusQueue.Contains(component))
        //     _focusQueue.AddLast(component);
        //
        return false;
    }
    
    public void Blur(Component component)
    {
        _componentsToRemoveFromFocusQueue.Add(component);
        
        // var focusedComponent = _focusQueue.First?.Value;
        // if (focusedComponent == component)
        // {
        //     _focusQueue.RemoveFirst();
        //     component.HandleFocusLost();
        //
        //     var nextFocus = _focusQueue.First?.Value;
        //     if (nextFocus != null)
        //     {
        //         nextFocus.HandleFocusGained();
        //     }
        // }
        // else
        // {
        //     _focusQueue.Remove(component);
        // }
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
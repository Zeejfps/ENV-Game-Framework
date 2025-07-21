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

    public void HandleKeyboardKeyEvent(in KeyboardKeyEvent e)
    {
        var target = _focusQueue.First;
        while (target != null)
        {
            var handled = target.Value.HandleKeyboardKeyEvent(e);
            if (handled)
                break;
            
            target = target.Next;       
        }
    }

    public void HandleMouseButtonEvent(MouseButtonEvent e)
    {
        var target = _focusQueue.First;
        while (target != null)
        {
            Console.WriteLine(target);
            var handled = target.Value.HandleMouseButtonEvent(e);
            if (handled)
                break;
            
            target = target.Next;       
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

        var target = _focusQueue.First;
        var e = new MouseMoveEvent
        {
            MousePosition = point,
        };
        while (target != null)
        {
            var propagate = target.Value.HandleMouseMoveEvent(e);
            if (!propagate)
                break;
            
            target = target.Next;       
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

    public bool TryFocus(Component component)
    {
        var focusedComponent = _focusQueue.First?.Value;
        if (focusedComponent == null)
        {
            _focusQueue.AddFirst(component);
            component.HandleFocusGained();
            return true;
        }

        _focusQueue.AddLast(component);
        return false;
    }

    private bool _isHandlingEvent;
    
    public void Blur(Component component)
    {
        if (_isHandlingEvent)
        {
            
        }
        
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

    private readonly List<Action> _handlerQueue = new();
    
    private void HandleEvent(Action action)
    {
        if (_isHandlingEvent)
        {
            _handlerQueue.Add(action);
        }
        else
        {
            _isHandlingEvent = true;
            action();
            _isHandlingEvent = false;
        }
        
        var queuedActions = _handlerQueue.ToArray();
        _handlerQueue.Clear();
        foreach (var queuedAction in queuedActions)
        {
            HandleEvent(queuedAction);
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
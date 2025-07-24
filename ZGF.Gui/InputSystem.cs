using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class InputSystem : IMouse
{
    private readonly HashSet<IKeyboardMouseController> _hoverableComponents = new();
    private readonly HashSet<IKeyboardMouseController> _hoveredComponents = new();
    
    private readonly LinkedList<IKeyboardMouseController> _focusQueue = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    
    private IKeyboardMouseController? _hoveredComponent;
    private IKeyboardMouseController? _focusedComponent;
    
    public void AddInteractable(IKeyboardMouseController controller)
    {
        _hoverableComponents.Add(controller);
    }

    public void RemoveInteractable(IKeyboardMouseController controller)
    {
        _hoverableComponents.Remove(controller);
        _focusQueue.Remove(controller);
    }
    
    public void SendKeyboardKeyEvent(ref KeyboardKeyEvent e)
    {
        foreach (var ctrl in _focusQueue)
        {
            ctrl.OnKeyboardKeyStateChanged(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
        
        e.Phase = EventPhase.Bubbling;
        
        foreach (var ctrl in _focusQueue.Reverse())
        {
            ctrl.OnKeyboardKeyStateChanged(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
    }

    public void SendMouseButtonEvent(ref MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            _pressedMouseButtons.Add(e.Button);
        }
        else
        {
            _pressedMouseButtons.Remove(e.Button);
        }

        foreach (var ctrl in _focusQueue)
        {
            ctrl.OnMouseButtonStateChanged(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
        
        e.Phase = EventPhase.Bubbling;
        
        foreach (var ctrl in _focusQueue.Reverse())
        {
            ctrl.OnMouseButtonStateChanged(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
    }
    
    public void SendMouseScrollEvent(ref MouseWheelScrolledEvent e)
    {
        foreach (var ctrl in _focusQueue)
        {
            ctrl.OnMouseWheelScrolled(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
        
        e.Phase = EventPhase.Bubbling;
        
        foreach (var ctrl in _focusQueue.Reverse())
        {
            ctrl.OnMouseWheelScrolled(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
    }
    
    public void UpdateMousePosition(in PointF point)
    {
        Point = point;

        SendMouseMovedEvent();
        
        var hitComponent = HitTest(Point);
        if (_hoveredComponent != hitComponent)
        {
            var prevHoveredComponent = _hoveredComponent;
            _hoveredComponent = hitComponent;

            if (prevHoveredComponent != null)
            {
                SendMouseExitEvent();
            }

            if (_focusedComponent == null)
            {
                _focusQueue.Clear();
                if (_hoveredComponent != null)
                {
                    BuildPath(_hoveredComponent);
                    SendMouseEnterEvent();
                }
            }
            else
            {
                if (_hoveredComponent != null)
                {
                    SendMouseEnterEvent();
                }
            }
        }
    }

    private void SendMouseMovedEvent()
    {
        var e = new MouseMoveEvent
        {
            Mouse = this,
            Phase = EventPhase.Capturing
        };
                
        foreach (var ctrl in _focusQueue)
        {
            ctrl.OnMouseMoved(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
        
        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            ctrl.OnMouseMoved(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
    }

    private void SendMouseExitEvent()
    {
        var mouseExitEvent = new MouseExitEvent
        {
            Mouse = this,
            Phase = EventPhase.Capturing
        };
                
        foreach (var ctrl in _focusQueue)
        {
            ctrl.OnMouseExit(ref mouseExitEvent);
            if (mouseExitEvent.IsConsumed)
            {
                return;
            }
        }
        
        mouseExitEvent.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            ctrl.OnMouseExit(ref mouseExitEvent);
            if (mouseExitEvent.IsConsumed)
            {
                return;
            }
        }
    }

    private void SendMouseEnterEvent()
    {
        var e = new MouseEnterEvent
        {
            Mouse = this,
            Phase = EventPhase.Capturing
        };
                
        foreach (var ctrl in _focusQueue)
        {
            ctrl.OnMouseEnter(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
        
        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            ctrl.OnMouseEnter(ref e);
            if (e.IsConsumed)
            {
                return;
            }
        }
    }

    private readonly List<IKeyboardMouseController> _hitTestCache = new();
    
    private IKeyboardMouseController? HitTest(in PointF point)
    {
        _hitTestCache.Clear();
        var components = _hitTestCache;
        foreach (var controller in _hoverableComponents)
        {
            if (controller.View.Position.ContainsPoint(point))
            {
                components.Add(controller);
            }
        }

        if (components.Count == 0)
            return null;
        
        components.Sort(ZIndexComparer.Instance);
        return components[0];
    }

    public void StealFocus(IKeyboardMouseController component)
    {
        var prevFocusedComponent = _focusQueue.First?.Value;
        _focusQueue.AddFirst(component);
        
        if (prevFocusedComponent != null)
        {
            prevFocusedComponent.OnFocusLost();
        }
        
        component.OnFocusGained();
    }
    
    public void Update()
    {
  
    }

    private void BuildPath(IKeyboardMouseController current)
    {
        _focusQueue.AddFirst(current);
        var parent = current.View.Parent;
        while (parent != null)
        {
            var controller = parent.Controller as IKeyboardMouseController;
            if (controller != null && _hoverableComponents.Contains(controller))
            {
                _focusQueue.AddFirst(controller);
            }
            parent = parent.Parent;
        }
    }

    public void RequestFocus(IKeyboardMouseController component)
    {
        Console.WriteLine($"Requesting focus: {component}");
        if (_focusedComponent == null)
        {
            _focusedComponent = component;
            _focusedComponent.OnFocusGained();
            Console.WriteLine($"Focused: {component}");
        }
    }
    
    public void Blur(IKeyboardMouseController component)
    {
        if (_focusedComponent == component)
        {
            _focusedComponent?.OnFocusLost();
            _focusedComponent = null;
        }
    }

    public bool IsInteractable(IKeyboardMouseController component)
    {
        return _hoverableComponents.Contains(component);
    }

    public bool IsFocused(IKeyboardMouseController component)
    {
        var focused = _focusQueue.First;
        if (focused == null)
            return false;
        return focused.Value == component;
    }


    #region IMouse
    
    public PointF Point { get; private set; }
    
    public bool IsButtonPressed(MouseButton button)
    {
        return _pressedMouseButtons.Contains(button);
    }
    
    #endregion
}

sealed class ZIndexComparer : IComparer<IKeyboardMouseController>
{
    public static ZIndexComparer Instance { get; } = new();

    public int Compare(IKeyboardMouseController? x, IKeyboardMouseController? y)
    {
        if (x == null && y == null)
            return 0;

        if (x == null)
            return 1;

        if (y == null)
            return -1;
        
        // NOTE(Zee): Order is swapped here. Greater ZIndex means the value is less - meaning it should be first in list 
        var result = y.View.ZIndex.CompareTo(x.View.ZIndex);
        if (result == 0)
        {
            if (x.View.IsInFrontOf(y.View))
            {
                return -1;
            }

            if (y.View.IsInFrontOf(x.View))
            {
                return 1;
            }
            
            return 0;
        }
        return result;
    }
}
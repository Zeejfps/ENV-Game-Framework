using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class InputSystem : IMouse
{
    private readonly HashSet<IKeyboardMouseController> _hoverableComponents = new();
    private readonly HashSet<IKeyboardMouseController> _hoveredComponents = new();
    private readonly LinkedList<IKeyboardMouseController> _focusQueue = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    
    public void AddInteractable(IKeyboardMouseController controller)
    {
        _hoverableComponents.Add(controller);
    }

    public void RemoveInteractable(IKeyboardMouseController controller)
    {
        _hoverableComponents.Remove(controller);
        _focusQueue.Remove(controller);
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
        if (e.State == InputState.Pressed)
        {
            _pressedMouseButtons.Add(e.Button);
        }
        else
        {
            _pressedMouseButtons.Remove(e.Button);
        }
        
        foreach (var target in _focusQueue)
        {
            var handled = target.OnMouseButtonStateChanged(e);
            if (handled)
                break;
        }
    }

    private readonly List<IKeyboardMouseController> _removeCache = new();
    
    public void UpdateMousePosition(in PointF point)
    {
        Point = point;
        
        var e = new MouseMoveEvent
        {
            Mouse = this,
        };

        foreach (var target in _focusQueue)
        {
            var handled = target.OnMouseMoved(e);
            if (handled)
                break;
        }
    }

    private readonly List<IKeyboardMouseController> _hitTestCache = new();
    
    private List<IKeyboardMouseController> HitTest(in PointF point)
    {
        _hitTestCache.Clear();
        var components = _hitTestCache;
        foreach (var controller in _hoverableComponents)
        {
            if (controller.Component.Position.ContainsPoint(point))
            {
                components.Add(controller);
            }
        }

        if (components.Count == 0)
            return components;
        
        components.Sort(ZIndexComparer.Instance);
        return components;
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

    private readonly List<IKeyboardMouseController> _componentsToAddToFocusQueue = new();
    private readonly List<IKeyboardMouseController> _componentsToRemoveFromFocusQueue = new();
    
    public void Update()
    {
        _removeCache.Clear();
        _removeCache.AddRange(_hoveredComponents);

        for (var i = _removeCache.Count - 1; i >= 0; i--)
        {
            var hoveredComponent = _removeCache[i];
            if (!hoveredComponent.Component.Position.ContainsPoint(Point))
            {
                _hoveredComponents.Remove(hoveredComponent);
                hoveredComponent.OnMouseExit();
            }
        }
        
        var allHoveredComponents = HitTest(Point);
        if (allHoveredComponents.Count > 0)
        {
            var topComponent = allHoveredComponents[0];
            for (var i = allHoveredComponents.Count - 1; i >= 0; i--)
            {
                var hoveredComponent = allHoveredComponents[i];
                if (hoveredComponent.Component.IsAncestorOf(topComponent.Component) && _hoveredComponents.Add(hoveredComponent))
                {
                    hoveredComponent.OnMouseEnter();
                }
            }
        }
        
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
                //Console.WriteLine($"Handling focus request: {component}");
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
            //Console.WriteLine($"Focus changing: {focusedComponent} -> {newFocusedComponent}");
            if (focusedComponent != null)
            {
                focusedComponent.OnFocusLost();
            }
            
            if (newFocusedComponent != null)
            {
                newFocusedComponent.OnFocusGained();
                //Console.WriteLine($"Focused: {newFocusedComponent}");
            }
        }
    }

    public bool RequestFocus(IKeyboardMouseController component)
    {
        //Console.WriteLine($"Requesting focus: {component}");
        _componentsToAddToFocusQueue.Add(component);
        return false;
    }
    
    public void Blur(IKeyboardMouseController component)
    {
        _componentsToRemoveFromFocusQueue.Add(component);
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
        var result = y.Component.ZIndex.CompareTo(x.Component.ZIndex);
        if (result == 0)
        {
            if (x.Component.IsInFrontOf(y.Component))
            {
                return -1;
            }

            if (y.Component.IsInFrontOf(x.Component))
            {
                return 1;
            }
            
            return 0;
        }
        return result;
    }
}
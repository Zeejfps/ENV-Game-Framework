using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class InputSystem : IMouse
{
    private readonly record struct ControllerRegistration(
        IKeyboardMouseController Controller,
        EventPhaseFilter PhaseFilter
    );

    private readonly HashSet<IKeyboardMouseController> _hoverableComponents = new();
    private readonly LinkedList<IKeyboardMouseController> _focusQueue = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    private readonly Dictionary<View, ControllerRegistration> _viewToController = new();
    private readonly Dictionary<IKeyboardMouseController, View> _controllerToView = new();

    private IKeyboardMouseController? _hoveredComponent;
    private IKeyboardMouseController? _focusedComponent;

    public void RegisterController(View view, IKeyboardMouseController controller, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        if (_viewToController.ContainsKey(view))
        {
            UnregisterController(view);
        }
        _viewToController[view] = new ControllerRegistration(controller, phaseFilter);
        _controllerToView[controller] = view;
        AddInteractable(controller);
        controller.OnAttached();
    }

    public void UnregisterController(View view)
    {
        if (_viewToController.Remove(view, out var registration))
        {
            _controllerToView.Remove(registration.Controller);
            registration.Controller.OnDetached();
            RemoveInteractable(registration.Controller);
        }
    }

    public IKeyboardMouseController? GetController(View view)
    {
        return _viewToController.TryGetValue(view, out var reg) ? reg.Controller : null;
    }

    public View? GetView(IKeyboardMouseController controller)
    {
        return _controllerToView.TryGetValue(controller, out var view) ? view : null;
    }

    public EventPhaseFilter GetPhaseFilter(IKeyboardMouseController controller)
    {
        foreach (var kvp in _viewToController.Values)
        {
            if (kvp.Controller == controller)
                return kvp.PhaseFilter;
        }
        return EventPhaseFilter.Both;
    }

    private void AddInteractable(IKeyboardMouseController controller)
    {
        _hoverableComponents.Add(controller);
    }

    private void RemoveInteractable(IKeyboardMouseController controller)
    {
        _hoverableComponents.Remove(controller);
        _focusQueue.Remove(controller);

        if (_hoveredComponent == controller)
        {
            _hoveredComponent = null;
        }

        if (_focusedComponent == controller)
        {
            _focusedComponent = null;
        }
    }
    
    public void SendKeyboardKeyEvent(ref KeyboardKeyEvent e)
    {
        e.Phase = EventPhase.Bubbling;
        if (_focusedComponent != null)
        {
            var filter = GetPhaseFilter(_focusedComponent);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                _focusedComponent.OnKeyboardKeyStateChanged(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Capturing;
        foreach (var ctrl in _focusQueue)
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Capture))
            {
                ctrl.OnKeyboardKeyStateChanged(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                ctrl.OnKeyboardKeyStateChanged(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
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

        e.Phase = EventPhase.Bubbling;
        if (_focusedComponent != null)
        {
            var filter = GetPhaseFilter(_focusedComponent);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                _focusedComponent.OnMouseButtonStateChanged(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Capturing;
        foreach (var ctrl in _focusQueue)
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Capture))
            {
                ctrl.OnMouseButtonStateChanged(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                ctrl.OnMouseButtonStateChanged(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }
    }
    
    public void SendMouseScrollEvent(ref MouseWheelScrolledEvent e)
    {
        e.Phase = EventPhase.Bubbling;
        if (_focusedComponent != null)
        {
            var filter = GetPhaseFilter(_focusedComponent);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                _focusedComponent.OnMouseWheelScrolled(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Capturing;
        foreach (var ctrl in _focusQueue)
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Capture))
            {
                ctrl.OnMouseWheelScrolled(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                ctrl.OnMouseWheelScrolled(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }
    }
    
    public void UpdateMousePosition(in PointF point)
    {
        Point = point;

        var consumed = SendMouseMovedEvent();
        if (consumed)
            return;
        
        var hitComponent = HitTest(Point);
        if (_hoveredComponent != hitComponent)
        {
            var prevHoveredComponent = _hoveredComponent;
            _hoveredComponent = hitComponent;

            if (prevHoveredComponent != null)
            {
                SendMouseExitEvent();
            }

            _focusQueue.Clear();
            if (_hoveredComponent != null)
            {
                BuildPath(_hoveredComponent);
                SendMouseEnterEvent();
            }
        }
    }

    private bool SendMouseMovedEvent()
    {
        var e = new MouseMoveEvent
        {
            Mouse = this,
            Phase = EventPhase.Bubbling
        };

        if (_focusedComponent != null)
        {
            var filter = GetPhaseFilter(_focusedComponent);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                _focusedComponent.OnMouseMoved(ref e);
                if (e.IsConsumed)
                {
                    return true;
                }
            }
        }

        e.Phase = EventPhase.Capturing;
        foreach (var ctrl in _focusQueue)
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Capture))
            {
                ctrl.OnMouseMoved(ref e);
                if (e.IsConsumed)
                {
                    return true;
                }
            }
        }

        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                ctrl.OnMouseMoved(ref e);
                if (e.IsConsumed)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SendMouseExitEvent()
    {
        var e = new MouseExitEvent
        {
            Mouse = this,
            Phase = EventPhase.Capturing
        };

        foreach (var ctrl in _focusQueue)
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Capture))
            {
                ctrl.OnMouseExit(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                ctrl.OnMouseExit(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
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
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Capture))
            {
                ctrl.OnMouseEnter(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
            }
        }

        e.Phase = EventPhase.Bubbling;
        foreach (var ctrl in _focusQueue.Reverse())
        {
            var filter = GetPhaseFilter(ctrl);
            if (filter.HasFlag(EventPhaseFilter.Bubble))
            {
                ctrl.OnMouseEnter(ref e);
                if (e.IsConsumed)
                {
                    return;
                }
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
            var view = GetView(controller);
            if (view != null && view.Position.ContainsPoint(point))
            {
                components.Add(controller);
            }
        }

        if (components.Count == 0)
            return null;

        components.Sort((x, y) => CompareByZIndex(x, y));
        return components[0];
    }

    private int CompareByZIndex(IKeyboardMouseController? x, IKeyboardMouseController? y)
    {
        if (x == null && y == null)
            return 0;
        if (x == null)
            return 1;
        if (y == null)
            return -1;

        var viewX = GetView(x);
        var viewY = GetView(y);

        if (viewX == null && viewY == null)
            return 0;
        if (viewX == null)
            return 1;
        if (viewY == null)
            return -1;

        // NOTE: Order is swapped here. Greater ZIndex means the value is less - meaning it should be first in list
        var result = viewY.ZIndex.CompareTo(viewX.ZIndex);
        if (result == 0)
        {
            if (viewX.IsInFrontOf(viewY))
                return -1;
            if (viewY.IsInFrontOf(viewX))
                return 1;
            return 0;
        }
        return result;
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
        var view = GetView(current);
        var parent = view?.Parent;
        while (parent != null)
        {
            var controller = GetController(parent);
            if (controller != null && _hoverableComponents.Contains(controller))
            {
                _focusQueue.AddFirst(controller);
            }
            parent = parent.Parent;
        }
    }

    public void RequestFocus(IKeyboardMouseController component)
    {
        //Console.WriteLine($"Requesting focus: {component}");
        if (_focusedComponent == null)
        {
            _focusedComponent = component;
            _focusedComponent.OnFocusGained();
            //Console.WriteLine($"Focused: {component}");
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
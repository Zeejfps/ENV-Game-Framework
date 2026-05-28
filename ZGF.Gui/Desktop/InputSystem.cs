using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class InputSystem
{
    private readonly record struct ControllerRegistration(
        IKeyboardMouseController Controller,
        EventPhaseFilter PhaseFilter
    );

    private readonly HashSet<IKeyboardMouseController> _hoverableComponents = new();
    private readonly LinkedList<IKeyboardMouseController> _focusQueue = new();
    private readonly Dictionary<View, ControllerRegistration> _viewToController = new();
    private readonly Dictionary<IKeyboardMouseController, View> _controllerToView = new();

    private IKeyboardMouseController? _hoveredComponent;
    private IKeyboardMouseController? _focusedComponent;

    public void RegisterController(View view, IKeyboardMouseController controller, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        if (_viewToController.TryGetValue(view, out var existing))
        {
            Console.WriteLine(
                $"[InputSystem] Warning: registering {controller.GetType().Name} on {view.GetType().Name} " +
                $"replaces existing controller {existing.Controller.GetType().Name}. " +
                $"Only one controller per view is supported — attach the second one to a different view, " +
                $"or use UsePresenter for behaviors that only need lifecycle (no input dispatch).");
            UnregisterController(view);
        }
        _viewToController[view] = new ControllerRegistration(controller, phaseFilter);
        _controllerToView[controller] = view;
        AddInteractable(controller);
    }

    public void UnregisterController(View view)
    {
        if (_viewToController.Remove(view, out var registration))
        {
            _controllerToView.Remove(registration.Controller);
            RemoveInteractable(registration.Controller);
        }
    }

    public IKeyboardMouseController? GetController(View view)
    {
        return _viewToController.TryGetValue(view, out var reg) ? reg.Controller : null;
    }

    public View? GetView(IKeyboardMouseController controller)
    {
        return _controllerToView.GetValueOrDefault(controller);
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

            // If the focused component released focus during dispatch (e.g. a
            // text input blurring on outside-click) without consuming, the
            // _focusQueue is still the stale path to it. Rebuild from the
            // cursor so capture/bubble reaches the actual click target instead
            // of vanishing — otherwise the user has to click a second time.
            if (_focusedComponent == null)
            {
                RefreshHover(e.Mouse);
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

    public void SendMouseMovedEvent(ref MouseMoveEvent e)
    {
        // RefreshHover runs in finally so hover bookkeeping isn't skipped when a
        // controller in the path consumes the move event (e.g. a modal backdrop).
        // Otherwise hover state freezes on the consumer and elements the cursor
        // moves onto never get MouseEnter.
        try
        {
            e.Phase = EventPhase.Bubbling;
            if (_focusedComponent != null)
            {
                var filter = GetPhaseFilter(_focusedComponent);
                if (filter.HasFlag(EventPhaseFilter.Bubble))
                {
                    _focusedComponent.OnMouseMoved(ref e);
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
                    ctrl.OnMouseMoved(ref e);
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
                    ctrl.OnMouseMoved(ref e);
                    if (e.IsConsumed)
                    {
                        return;
                    }
                }
            }
        }
        finally
        {
            RefreshHover(e.Mouse);
        }
    }

    /// <summary>
    /// Re-runs hit-testing from the cursor's current position and fires
    /// MouseEnter / MouseExit events if the hovered controller changed.
    /// Call this when the view tree changes between mouse-move events
    /// (e.g. a collapsed group is replaced) so hover state catches up
    /// without the user having to wiggle the mouse.
    /// </summary>
    public void RefreshHover(IMouse mouse)
    {
        var hitComponent = HitTest(mouse.Point);
        if (_hoveredComponent == hitComponent) return;

        var prevHoveredComponent = _hoveredComponent;
        _hoveredComponent = hitComponent;

        if (prevHoveredComponent != null)
        {
            var mouseExitEvent = new MouseExitEvent
            {
                Mouse = mouse,
                Phase = EventPhase.Capturing,
            };
            SendMouseExitEvent(ref mouseExitEvent);
        }

        _focusQueue.Clear();
        if (_hoveredComponent != null)
        {
            BuildPath(_hoveredComponent);
            var mouseEnterEvent = new MouseEnterEvent
            {
                Mouse = mouse,
                Phase = EventPhase.Capturing,
            };
            SendMouseEnterEvent(ref mouseEnterEvent);
        }
    }

    private void SendMouseExitEvent(ref MouseExitEvent e)
    {
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

    private void SendMouseEnterEvent(ref MouseEnterEvent e)
    {
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
            if (view == null) continue;
            if (!view.Position.ContainsPoint(point)) continue;
            if (!IsViewAndAncestorsVisible(view)) continue;
            // Reject descendants of any clipping ancestor (e.g. ScrollPane) when the
            // cursor falls outside that ancestor's viewport — scrolled-off rows still
            // report their full positions, so without this check a row that has
            // scrolled into the header band would steal hover from header buttons.
            if (!IsPointInsideClippingAncestors(view, point)) continue;
            components.Add(controller);
        }

        if (components.Count == 0)
            return null;

        components.Sort(CompareByZIndex);
        return components[0];
    }

    private static bool IsPointInsideClippingAncestors(View view, in PointF point)
    {
        var parent = view.Parent;
        while (parent != null)
        {
            if (parent.ClipsContent && !parent.Position.ContainsPoint(point))
                return false;
            parent = parent.Parent;
        }
        return true;
    }

    // Mirrors the draw-side IsVisible check: a hidden view (or any hidden ancestor) is
    // also non-interactive. Without this, sibling overlays kept attached just for state
    // continuity (e.g. MainContentView's cached mode panels) still win hit-tests against
    // the visible side.
    private static bool IsViewAndAncestorsVisible(View view)
    {
        var current = view;
        while (current != null)
        {
            if (!current.IsVisible) return false;
            current = current.Parent;
        }
        return true;
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

    /// <summary>
    /// Take focus unconditionally, blurring whoever currently holds it.
    /// </summary>
    public void StealFocus(IKeyboardMouseController component)
    {
        if (_focusedComponent == component) return;
        var prev = _focusedComponent;
        _focusedComponent = component;
        prev?.OnFocusLost();
        component.OnFocusGained();
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

    public bool HasFocus => _focusedComponent != null;

    public bool IsFocused(IKeyboardMouseController component)
    {
        var focused = _focusQueue.First;
        if (focused == null)
            return false;
        return focused.Value == component;
    }
}
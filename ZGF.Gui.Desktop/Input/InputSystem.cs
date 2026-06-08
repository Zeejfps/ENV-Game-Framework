using ZGF.Geometry;

namespace ZGF.Gui.Desktop.Input;

public sealed class InputSystem
{
    private readonly record struct ControllerRegistration(
        IKeyboardMouseController Controller,
        EventPhaseFilter PhaseFilter
    );

    private readonly HashSet<IKeyboardMouseController> _hoverableComponents = new();
    private readonly LinkedList<IKeyboardMouseController> _focusQueue = new();
    // A view may host multiple controllers. They participate in capture/bubble dispatch in
    // registration order (capture = registration order, bubble = reverse) — see BuildPath.
    private readonly Dictionary<View, List<ControllerRegistration>> _viewToControllers = new();
    private readonly Dictionary<IKeyboardMouseController, View> _controllerToView = new();

    private IKeyboardMouseController? _hoveredComponent;
    private IKeyboardMouseController? _focusedComponent;

    public void RegisterController(View view, IKeyboardMouseController controller, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        if (!_viewToControllers.TryGetValue(view, out var list))
        {
            list = new List<ControllerRegistration>();
            _viewToControllers[view] = list;
        }
        // Append: controllers on a view dispatch in registration order (capture order).
        list.Add(new ControllerRegistration(controller, phaseFilter));
        _controllerToView[controller] = view;
        AddInteractable(controller);
    }

    /// <summary>Remove a single controller from a view, leaving any others on that view intact.</summary>
    public void UnregisterController(View view, IKeyboardMouseController controller)
    {
        if (!_viewToControllers.TryGetValue(view, out var list)) return;
        var index = list.FindIndex(r => r.Controller == controller);
        if (index < 0) return;
        list.RemoveAt(index);
        if (list.Count == 0)
            _viewToControllers.Remove(view);
        _controllerToView.Remove(controller);
        RemoveInteractable(controller);
    }

    /// <summary>Remove every controller registered on a view.</summary>
    public void UnregisterController(View view)
    {
        if (!_viewToControllers.Remove(view, out var list)) return;
        foreach (var registration in list)
        {
            _controllerToView.Remove(registration.Controller);
            RemoveInteractable(registration.Controller);
        }
    }

    /// <summary>First controller registered on the view, or null. Prefer the dispatch path for input.</summary>
    public IKeyboardMouseController? GetController(View view)
    {
        return _viewToControllers.TryGetValue(view, out var list) && list.Count > 0
            ? list[0].Controller
            : null;
    }

    public View? GetView(IKeyboardMouseController controller)
    {
        return _controllerToView.GetValueOrDefault(controller);
    }

    public EventPhaseFilter GetPhaseFilter(IKeyboardMouseController controller)
    {
        if (_controllerToView.TryGetValue(controller, out var view)
            && _viewToControllers.TryGetValue(view, out var list))
        {
            foreach (var registration in list)
            {
                if (registration.Controller == controller)
                    return registration.PhaseFilter;
            }
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
        // Exception: when the focused component itself consumes, it owns the input
        // stream (drag, popup outside-click capture); freezing main-window hover is
        // required so cursor moves over a popup don't re-hover views underneath.
        var focusedConsumed = false;
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
                        focusedConsumed = true;
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
            if (!focusedConsumed) RefreshHover(e.Mouse);
        }
    }

    public void ClearHover()
    {
        if (_hoveredComponent == null) return;

        var syntheticMouse = new Mouse { Point = new PointF(float.MinValue, float.MinValue) };
        var exitEvent = new MouseExitEvent
        {
            Mouse = syntheticMouse,
            Phase = EventPhase.Capturing,
        };
        SendMouseExitEvent(ref exitEvent);
        _hoveredComponent = null;
        // _focusQueue is intentionally left intact: ClearHover can be re-entered
        // from inside an outer iteration of this same list (StealFocus during
        // button dispatch). The next RefreshHover rebuilds it.
    }

    /// <summary>
    /// Drop all transient input state — focus, hover, and the built focus path —
    /// without firing exit/blur callbacks against the (already detached) controllers.
    /// Used when a popup window is returned to the pool: the views it dispatched to
    /// are gone, but the InputSystem instance is reused for the next popup. Without
    /// this, a leftover <see cref="_focusedComponent"/> latches <see cref="HasFocus"/>
    /// true, and DesktopInputSystem.Update short-circuits before hover/click dispatch —
    /// leaving every subsequently pooled popup dead until the app restarts.
    /// </summary>
    public void Reset()
    {
        _focusedComponent = null;
        _hoveredComponent = null;
        _focusQueue.Clear();
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

    private readonly List<View> _hitTestViews = new();

    // Returns a representative controller for the topmost view under the cursor: that view's
    // first-registered controller. Hover tracking keys off this representative, so it must be
    // stable for a given view (a view's controllers all share a z-index and would otherwise
    // tie non-deterministically). The full set of the view's controllers enters the dispatch
    // path via BuildPath.
    private IKeyboardMouseController? HitTest(in PointF point)
    {
        _hitTestViews.Clear();
        foreach (var (view, list) in _viewToControllers)
        {
            if (list.Count == 0) continue;
            if (!view.Position.ContainsPoint(point)) continue;
            if (!IsViewAndAncestorsVisible(view)) continue;
            // Reject descendants of any clipping ancestor (e.g. ScrollPane) when the
            // cursor falls outside that ancestor's viewport — scrolled-off rows still
            // report their full positions, so without this check a row that has
            // scrolled into the header band would steal hover from header buttons.
            if (!IsPointInsideClippingAncestors(view, point)) continue;
            _hitTestViews.Add(view);
        }

        if (_hitTestViews.Count == 0)
            return null;

        _hitTestViews.Sort(CompareViewsByZIndex);
        return _viewToControllers[_hitTestViews[0]][0].Controller;
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

    private static int CompareViewsByZIndex(View? x, View? y)
    {
        if (x == null && y == null)
            return 0;
        if (x == null)
            return 1;
        if (y == null)
            return -1;

        // NOTE: Order is swapped here. Greater ZIndex means the value is less - meaning it should be first in list
        var result = y.ZIndex.CompareTo(x.ZIndex);
        if (result == 0)
        {
            if (x.IsInFrontOf(y))
                return -1;
            if (y.IsInFrontOf(x))
                return 1;
            return 0;
        }
        return result;
    }

    private readonly List<View> _pathViews = new();

    // Builds the capture/bubble dispatch path for the hovered view. The path is ordered
    // ancestor-first (capture order); each view contributes all of its controllers in
    // registration order. Bubble dispatch walks the same queue in reverse.
    private void BuildPath(IKeyboardMouseController current)
    {
        var seedView = GetView(current);
        if (seedView == null)
        {
            _focusQueue.AddLast(current);
            return;
        }

        _pathViews.Clear();
        for (var view = seedView; view != null; view = view.Parent)
            _pathViews.Add(view);

        // _pathViews is seed→root; walk root→seed so the queue is ancestor-first.
        for (var i = _pathViews.Count - 1; i >= 0; i--)
        {
            if (!_viewToControllers.TryGetValue(_pathViews[i], out var list))
                continue;
            foreach (var registration in list)
            {
                if (_hoverableComponents.Contains(registration.Controller))
                    _focusQueue.AddLast(registration.Controller);
            }
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

    /// <summary>The controller currently holding keyboard focus, or null. Unlike
    /// <see cref="IsFocused"/> (which reflects hover order), this is the real keyboard-focus target.</summary>
    public IKeyboardMouseController? FocusedComponent => _focusedComponent;

    public bool IsFocused(IKeyboardMouseController component)
    {
        var focused = _focusQueue.First;
        if (focused == null)
            return false;
        return focused.Value == component;
    }
}
using System;
using System.Collections.Generic;
using ZGF.Geometry;

namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// The mobile touch input system: registry, hit-testing, and capture/bubble dispatch of
/// pointer events to <see cref="IPointerController"/>s. It is the mobile parallel to the
/// desktop's InputSystem + DesktopInputSystem combined — touch is simpler than mouse, so the
/// dispatch engine and the platform-facing glue collapse into one class.
///
/// Platform hosts (iOS UIKit, Android) feed raw touch coordinates via
/// <see cref="OnPointerDown"/> / <see cref="OnPointerMoved"/> / <see cref="OnPointerUp"/> /
/// <see cref="OnPointerCancelled"/>; everything above that — which view is hit, how the event
/// travels the tree — is platform-neutral and lives here.
///
/// Gesture capture: the controllers under the finger at touch-down form the active path, and
/// every move/release in that gesture dispatches to that same path even if the finger drifts
/// off the view. This matches platform touch semantics and removes any need for the per-frame
/// hover polling the desktop system does.
/// </summary>
public sealed class MobileInputSystem
{
    private delegate void Dispatch(IPointerController controller, ref PointerEvent e);

    private readonly RenderedCanvasBase _canvas;

    // Registration order within a view is preserved (capture order); bubble walks it reversed.
    private readonly Dictionary<View, List<IPointerController>> _viewToControllers = new();
    private readonly Dictionary<IPointerController, View> _controllerToView = new();
    private readonly Dictionary<IPointerController, PointerPhaseFilter> _filters = new();

    // The capture/bubble path for the in-flight gesture, ancestor-first. Built on pointer-down
    // and reused for the gesture's moves and release so touch stays glued to its down-target.
    private readonly List<IPointerController> _activePath = new();
    private readonly List<View> _hitTestViews = new();
    private readonly List<View> _pathViews = new();

    public Pointer Pointer { get; } = new();

    /// <summary>Raised after any pointer event is dispatched — hosts use it to request a redraw.</summary>
    public Action? OnAnyInput { get; set; }

    /// <summary>Raised on pointer-down with the topmost hit view (null when the touch lands on empty
    /// space), before the event is dispatched. Lets keyboard glue dismiss when a tap falls outside
    /// the field being edited.</summary>
    public Action<View?>? PointerPressed { get; set; }

    /// <summary>Raised on each pointer-move during a gesture; the current location is in
    /// <see cref="Pointer"/>. Lets keyboard glue dismiss on a downward swipe.</summary>
    public Action? PointerDragged { get; set; }

    public MobileInputSystem(RenderedCanvasBase canvas)
    {
        _canvas = canvas;
    }

    public void RegisterController(View view, IPointerController controller, PointerPhaseFilter phaseFilter = PointerPhaseFilter.Both)
    {
        if (!_viewToControllers.TryGetValue(view, out var list))
        {
            list = new List<IPointerController>();
            _viewToControllers[view] = list;
        }
        list.Add(controller);
        _controllerToView[controller] = view;
        _filters[controller] = phaseFilter;
    }

    public void UnregisterController(View view, IPointerController controller)
    {
        if (_viewToControllers.TryGetValue(view, out var list))
        {
            list.Remove(controller);
            if (list.Count == 0)
                _viewToControllers.Remove(view);
        }
        _controllerToView.Remove(controller);
        _filters.Remove(controller);
        _activePath.Remove(controller);
    }

    /// <summary>Drop all transient state (active gesture path, pointer down-state).</summary>
    public void Reset()
    {
        _activePath.Clear();
        Pointer.IsDown = false;
    }

    // --- Platform-facing glue -------------------------------------------------------------
    //
    // Coordinates arrive in the host surface's logical points: origin top-left, Y-down, the
    // same units the canvas lays out in. We flip Y to the canvas's Y-up space (where views
    // live) using the canvas height. Mirrors DesktopInputSystem.WindowToGuiCoords, minus the
    // window/canvas scale factor (mobile surfaces report logical points already).

    public void OnPointerDown(float x, float y)
    {
        var point = ToGuiCoords(x, y);
        Pointer.Point = point;
        Pointer.IsDown = true;

        _activePath.Clear();
        var hitView = HitTestTopView(point);
        if (hitView != null && _viewToControllers.TryGetValue(hitView, out var controllers) && controllers.Count > 0)
            BuildPath(controllers[0]);

        PointerPressed?.Invoke(hitView);

        DispatchToPath(point, static (IPointerController c, ref PointerEvent e) => c.OnPointerEntered(ref e));
        DispatchToPath(point, static (IPointerController c, ref PointerEvent e) => c.OnPointerTouched(ref e));
        OnAnyInput?.Invoke();
    }

    public void OnPointerMoved(float x, float y)
    {
        if (!Pointer.IsDown)
            return;
        var point = ToGuiCoords(x, y);
        Pointer.Point = point;
        PointerDragged?.Invoke();
        DispatchToPath(point, static (IPointerController c, ref PointerEvent e) => c.OnPointerMoved(ref e));
        OnAnyInput?.Invoke();
    }

    public void OnPointerUp(float x, float y)
    {
        var point = ToGuiCoords(x, y);
        Pointer.Point = point;
        DispatchToPath(point, static (IPointerController c, ref PointerEvent e) => c.OnPointerReleased(ref e));
        DispatchToPath(point, static (IPointerController c, ref PointerEvent e) => c.OnPointerExited(ref e));
        Pointer.IsDown = false;
        _activePath.Clear();
        OnAnyInput?.Invoke();
    }

    public void OnPointerCancelled()
    {
        DispatchToPath(Pointer.Point, static (IPointerController c, ref PointerEvent e) => c.OnPointerExited(ref e));
        Pointer.IsDown = false;
        _activePath.Clear();
        OnAnyInput?.Invoke();
    }

    private PointF ToGuiCoords(float x, float y) => new(x, _canvas.Height - y);

    // --- Dispatch -------------------------------------------------------------------------

    private void DispatchToPath(PointF position, Dispatch fn)
    {
        if (_activePath.Count == 0)
            return;

        var e = new PointerEvent
        {
            Pointer = Pointer,
            Position = position,
            Phase = PointerPhase.Capturing,
        };

        // Capture: ancestor → target.
        for (var i = 0; i < _activePath.Count; i++)
        {
            var controller = _activePath[i];
            if (_filters.GetValueOrDefault(controller, PointerPhaseFilter.Both).HasFlag(PointerPhaseFilter.Capture))
            {
                fn(controller, ref e);
                if (e.IsConsumed)
                    return;
            }
        }

        // Bubble: target → ancestor.
        e.Phase = PointerPhase.Bubbling;
        for (var i = _activePath.Count - 1; i >= 0; i--)
        {
            var controller = _activePath[i];
            if (_filters.GetValueOrDefault(controller, PointerPhaseFilter.Both).HasFlag(PointerPhaseFilter.Bubble))
            {
                fn(controller, ref e);
                if (e.IsConsumed)
                    return;
            }
        }
    }

    // --- Hit testing ----------------------------------------------------------------------

    // The topmost view with a controller under the point, honoring visibility, clipping and
    // z-order. Returned to PointerPressed so keyboard glue can tell taps inside vs. outside a field.
    private View? HitTestTopView(in PointF point)
    {
        _hitTestViews.Clear();
        foreach (var (view, list) in _viewToControllers)
        {
            if (list.Count == 0) continue;
            if (!view.Position.ContainsPoint(point)) continue;
            if (!IsViewAndAncestorsVisible(view)) continue;
            if (!IsPointInsideClippingAncestors(view, point)) continue;
            _hitTestViews.Add(view);
        }

        if (_hitTestViews.Count == 0)
            return null;

        _hitTestViews.Sort(CompareViewsByZIndex);
        return _hitTestViews[0];
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

    // Greater ZIndex sorts first; ties broken by paint order (IsInFrontOf). Mirrors the
    // desktop InputSystem's comparator so touch and mouse pick the same topmost view.
    private static int CompareViewsByZIndex(View? x, View? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        var result = y.ZIndex.CompareTo(x.ZIndex);
        if (result == 0)
        {
            if (x.IsInFrontOf(y)) return -1;
            if (y.IsInFrontOf(x)) return 1;
            return 0;
        }
        return result;
    }

    // Builds the active path for the gesture: every controller from the hit view up to the
    // root, ancestor-first, each view contributing its controllers in registration order.
    private void BuildPath(IPointerController seed)
    {
        var seedView = _controllerToView.GetValueOrDefault(seed);
        if (seedView == null)
        {
            _activePath.Add(seed);
            return;
        }

        _pathViews.Clear();
        for (var view = seedView; view != null; view = view.Parent)
            _pathViews.Add(view);

        // _pathViews is seed → root; walk root → seed so the path is ancestor-first.
        for (var i = _pathViews.Count - 1; i >= 0; i--)
        {
            if (!_viewToControllers.TryGetValue(_pathViews[i], out var list))
                continue;
            foreach (var controller in list)
            {
                if (_filters.ContainsKey(controller))
                    _activePath.Add(controller);
            }
        }
    }
}

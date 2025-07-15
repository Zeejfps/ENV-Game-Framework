using ZGF.Geometry;

namespace ZGF.Gui;

public interface IMouseListener
{
    void HandleMouseEnterEvent();
    void HandleMouseExitEvent();
}

public interface ICaptureMouse
{
    void HandleMouseButtonEvent();
    void HandleMouseWheelEvent();
    void HandleMouseMoveEvent();
}

public sealed class MouseInputSystem
{
    public static MouseInputSystem Instance { get; } = new();

    private readonly Dictionary<Component, IMouseListener> _listenersByComponentLookup = new();
    
    private Component? _hoveredComponent;
    private ICaptureMouse? _focusedComponent;
    
    public void RegisterListener(Component component, IMouseListener listener)
    {
        _listenersByComponentLookup[component] = listener;
    }

    public void HandleMouseButtonEvent()
    {
        if (_focusedComponent != null)
        {
            _focusedComponent.HandleMouseButtonEvent();
        }
    }

    public void UpdateMousePosition(int x, int y)
    {
        var newHoveredComponent = HitTest(x, y);
        if (newHoveredComponent != _hoveredComponent)
        {
            var prevHoveredComponent = _hoveredComponent;
            _hoveredComponent = newHoveredComponent;

            if (prevHoveredComponent != null &&
                _listenersByComponentLookup.TryGetValue(prevHoveredComponent, out var listener))
            {
                listener.HandleMouseExitEvent();
            }

            if (_hoveredComponent != null &&
                _listenersByComponentLookup.TryGetValue(_hoveredComponent, out listener))
            {
                listener.HandleMouseEnterEvent();           
            }
        }
    }

    private readonly SortedSet<Component> _hitTestCache = new(new ZIndexComparer());
    
    private Component? HitTest(int x, int y)
    {
        // TODO: Fix
        _hitTestCache.Clear();
        var components = _hitTestCache;
        var hitPoint = new PointF(x, y);
        //Console.WriteLine($"Compoennts: {_listenersByComponentLookup.Count}");
        foreach (var component in _listenersByComponentLookup.Keys)
        {
            if (component.Position.ContainsPoint(hitPoint))
            {
                //Console.WriteLine($"Hit: {component.GetHashCode()}");
                components.Add(component);
            }
        }

        //Console.WriteLine($"ComponentsHit: {components.Count}");
        if (components.Count == 0)
            return null;
        
        return components.Min;
    }

    public void CaptureMouse(Component component, ICaptureMouse captureMouse)
    {
        _focusedComponent = captureMouse;
    }

    public void ReleaseMouse(Component component, ICaptureMouse captureMouse)
    {
        if (_focusedComponent == captureMouse)
        {
            _focusedComponent = null;       
        }
    }
    
    sealed class ZIndexComparer : IComparer<Component>
    {
        public int Compare(Component? x, Component? y)
        {
            return x.ZIndex.CompareTo(y.ZIndex);       
        }
    }
}
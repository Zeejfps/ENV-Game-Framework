using ZGF.Geometry;

namespace ZGF.Gui;

public interface IMouseListener
{
    void HandleMouseEnterEvent();
    void HandleMouseExitEvent();
}

public interface IMouseFocusable
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
    private IMouseFocusable? _focusedComponent;
    
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

    private readonly List<Component> _hitTestCache = new();
    
    private Component? HitTest(int x, int y)
    {
        _hitTestCache.Clear();
        var components = _hitTestCache;
        var hitPoint = new PointF(x, y);
        foreach (var component in _listenersByComponentLookup.Keys)
        {
            if (component.Position.ContainsPoint(hitPoint))
            {
                components.Add(component);
            }
        }

        if (components.Count == 0)
            return null;
        
        _hitTestCache.Sort(ZIndexComparer.Instance);
        return components.Last();
    }

    public void Focus(Component component, IMouseFocusable captureMouse)
    {
        _focusedComponent = captureMouse;
    }

    public void Blur(Component component, IMouseFocusable captureMouse)
    {
        if (_focusedComponent == captureMouse)
        {
            _focusedComponent = null;       
        }
    }
    
    sealed class ZIndexComparer : IComparer<Component>
    {
        public static ZIndexComparer Instance { get; } = new();
        
        public int Compare(Component? x, Component? y)
        {
            var result = x.ZIndex.CompareTo(y.ZIndex);
            if (result == 0)
            {
                if (x.IsInFrontOf(y))
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
                //TODO: Sort them based on hierchy?
            }
            return result;
        }
    }
}
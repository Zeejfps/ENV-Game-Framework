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
    
    public void RegisterListener(Component component, IMouseListener listener)
    {
        _listenersByComponentLookup[component] = listener;
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

    private Component? HitTest(int x, int y)
    {
        var components = new SortedList<int, Component>();
        foreach (var component in _listenersByComponentLookup.Keys)
        {
            if (component.Position.ContainsPoint(new PointF(x, y)))
            {
                components.Add(component.ZIndex, component);
            }
        }

        if (components.Count == 0)
            return null;
        
        return components.Values.First();
    }
}
using ZGF.Geometry;

namespace ZGF.Gui;

public abstract class Layout : ILayout
{
    private readonly List<Component> _components = new();

    public bool IsDirty => _components.Any(component => component.IsDirty);

    public void Add(Component component)
    {
        _components.Add(component);
    }
    
    public RectF DoLayout(RectF position)
    {
        return OnDoLayout(position, _components);
    }

    public void ApplyStyleSheet(StyleSheet styleSheet)
    {
        foreach (var component in _components)
        {
            component.ApplyStyle(styleSheet);
        }
    }

    public void DrawSelf(ICanvas canvas)
    {
        foreach (var component in _components)
        {
            component.DrawSelf(canvas);
        }
    }

    protected abstract RectF OnDoLayout(RectF position, IReadOnlyList<Component> components);
}
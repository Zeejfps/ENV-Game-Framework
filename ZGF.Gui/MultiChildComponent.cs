namespace ZGF.Gui;

public abstract class MultiChildComponent : Component
{
    private readonly List<Component> _children = new();

    public IReadOnlyList<Component> Children => _children;
    public override bool IsDirty => _children.Any(component => component.IsDirty) || base.IsDirty;

    public void Add(Component component)
    {
        _children.Add(component);
    }

    protected override void OnLayoutSelf()
    {
        Console.WriteLine(Position);
        foreach (var child in _children)
        {
            child.Position = Position;
            child.LayoutSelf();
        }
    }

    protected override void OnApplyStyleSheet(StyleSheet styleSheet)
    {
        foreach (var child in _children)
        {
            child.ApplyStyleSheet(styleSheet);
        }
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        foreach (var component in _children)
        {
            component.DrawSelf(c);
        }
    }
}
using ZGF.Geometry;

namespace ZGF.Gui.Layouts;

public struct FlexStyle
{
    public StyleValue<float> Flex { get; set; }
    public StyleValue<float> Grow { get; set; }
    public StyleValue<float> Shrink { get; set; }
}

public sealed class FlexRow : Component
{
    public float ItemGap { get; set; }
    
    private readonly Dictionary<Component, FlexStyle> _flexStyleByComponent = new();
    
    public void Add(Component component, FlexStyle style)
    {
        var added = _flexStyleByComponent.TryAdd(component, style);
        if (!added)
            throw new Exception("Component already added");
        
        Add(component);
    }

    public void UpdateStyle(Component component, FlexStyle style)
    {
        _flexStyleByComponent[component] = style;
        SetDirty();
    }

    protected override void OnComponentRemoved(Component component)
    {
        _flexStyleByComponent.Remove(component);
        base.OnComponentRemoved(component);
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var children = Children;
        var leftOffset = 0f;
        foreach (var child in children)
        {
            var style = _flexStyleByComponent.GetValueOrDefault(child);
            var childWidth = child.Constraints.Width;
            child.Constraints = new RectF
            {
                Left = position.Left + leftOffset,
                Bottom = position.Bottom,
                Width = childWidth,
                Height = position.Height,
            };
            child.LayoutSelf();
            leftOffset += childWidth;
        }
    }
}
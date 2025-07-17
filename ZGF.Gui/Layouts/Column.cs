namespace ZGF.Gui.Layouts;

public sealed class Column : Component
{
    private int _gap;
    public int Gap
    {
        get => _gap;
        set => SetField(ref _gap, value);
    }
    
    protected override void OnLayoutChildren()
    {
        var position = Position;
        var components = Children;
        var componentCount = components.Count;
        if (componentCount == 0)
        {
            return;
        }

        var spacing = (componentCount - 1) * Gap;
        var totalHeight = position.Height - spacing;
        var componentHeight = totalHeight / componentCount;
        var bottom = position.Top;
        foreach (var component in components)
        {
            bottom -= componentHeight;
            component.LeftConstraint = position.Left;
            component.MinWidthConstraint = position.Width;
            component.MaxWidthConstraint = position.Width;
            component.BottomConstraint = bottom;
            component.MaxHeightConstraint = componentHeight;
            component.LayoutSelf();
            bottom -= Gap;
        }
    }

    public override float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight;

        var totalHeight = 0f;
        foreach (var child in Children)
        {
            totalHeight += child.MeasureHeight();
        }
        var spacing = (Children.Count - 1) * Gap;

        return totalHeight + spacing;
    }
}
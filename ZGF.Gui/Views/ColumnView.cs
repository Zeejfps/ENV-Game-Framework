namespace ZGF.Gui.Layouts;

public sealed class ColumnView : MultiChildView
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

        var bottom = position.Top;
        foreach (var component in components)
        {
            component.LeftConstraint = position.Left;

            var componentHeight = component.MeasureHeight(position.Width);
            bottom -= componentHeight;
            component.BottomConstraint = bottom;
            component.WidthConstraint = position.Width;
            component.HeightConstraint = componentHeight;
            component.LayoutSelf();
            bottom -= Gap;
        }
    }

    public override float MeasureHeight(float availableWidth)
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight;

        var totalHeight = 0f;
        foreach (var child in Children)
        {
            totalHeight += child.MeasureHeight(availableWidth);
        }
        var spacing = (Children.Count - 1) * Gap;

        var height = totalHeight + spacing;
        return height;
    }
}
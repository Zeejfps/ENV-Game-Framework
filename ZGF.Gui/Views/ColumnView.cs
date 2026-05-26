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
        if (components.Count == 0)
        {
            return;
        }

        var bottom = position.Top;
        var first = true;
        foreach (var component in components)
        {
            if (!component.IsVisible) continue;
            if (!first) bottom -= Gap;
            component.LeftConstraint = position.Left;

            var componentHeight = component.MeasureHeight(position.Width);
            bottom -= componentHeight;
            component.BottomConstraint = bottom;
            component.WidthConstraint = position.Width;
            component.HeightConstraint = componentHeight;
            component.LayoutSelf();
            first = false;
        }
    }

    public override float MeasureHeight(float availableWidth)
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight;

        var totalHeight = 0f;
        var visibleCount = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            totalHeight += child.MeasureHeight(availableWidth);
            visibleCount++;
        }
        var spacing = visibleCount > 0 ? (visibleCount - 1) * Gap : 0;

        var height = totalHeight + spacing;
        return height;
    }
}
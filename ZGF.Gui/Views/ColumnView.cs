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
            // Set cross-axis width before measuring height so height-for-width children
            // (e.g. wrapping text) can return the correct height.
            component.LeftConstraint = position.Left;
            component.WidthConstraint = position.Width;

            var componentHeight = component.MeasureHeight();
            bottom -= componentHeight;
            component.BottomConstraint = bottom;
            component.HeightConstraint = componentHeight;
            component.LayoutSelf();
            bottom -= Gap;
        }
    }

    public override float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight;

        // Propagate the width we'll lay out at to height-for-width children.
        var width = WidthConstraint.IsSet ? WidthConstraint.Value
                  : PreferredWidth.IsSet ? PreferredWidth.Value
                  : 0f;

        var totalHeight = 0f;
        foreach (var child in Children)
        {
            if (width > 0f)
            {
                child.WidthConstraint = width;
            }
            totalHeight += child.MeasureHeight();
        }
        var spacing = (Children.Count - 1) * Gap;

        var height = totalHeight + spacing;
        return height;
    }
}
namespace ZGF.Gui.Layouts;

public sealed class FlexColumnView : MultiChildView
{
    public float Gap
    {
        get;
        set => SetField(ref field, value);
    }

    public CrossAxisAlignment CrossAxisAlignment
    {
        get;
        set => SetField(ref field, value);
    }

    public MainAxisAlignment MainAxisAlignment
    {
        get;
        set => SetField(ref field, value);
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
        var spacing = visibleCount > 0 ? (visibleCount - 1) * Gap : 0f;
        return totalHeight + spacing;
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var children = Children;
        if (children.Count == 0)
        {
            return;
        }

        var totalChildrenInitialHeight = 0f;
        var totalFlexGrow = 0f;
        var visibleCount = 0;

        foreach (var child in children)
        {
            if (!child.IsVisible) continue;
            var grow = child is FlexItem item ? (float)item.Grow : 0f;
            // Pass the column's cross-axis width so height-for-width children (wrapping text)
            // return the correct height in the first measurement pass.
            totalChildrenInitialHeight += child.MeasureHeight(position.Width);
            totalFlexGrow += grow;
            visibleCount++;
        }

        if (visibleCount == 0) return;

        var totalGap = Gap * (visibleCount - 1);
        var totalContentHeight = totalChildrenInitialHeight + totalGap;
        var remainingSpace = position.Height - totalContentHeight;

        var topOffset = 0f;
        var interItemSpacing = 0f;

        if (remainingSpace > 0)
        {
            switch (MainAxisAlignment)
            {
                case MainAxisAlignment.End:
                    topOffset = remainingSpace;
                    break;
                case MainAxisAlignment.Center:
                    topOffset = remainingSpace / 2f;
                    break;
                case MainAxisAlignment.SpaceBetween:
                    interItemSpacing = visibleCount > 1 ? remainingSpace / (visibleCount - 1) : 0;
                    break;
                case MainAxisAlignment.SpaceAround:
                    interItemSpacing = remainingSpace / visibleCount;
                    topOffset = interItemSpacing / 2f;
                    break;
                case MainAxisAlignment.SpaceEvenly:
                    interItemSpacing = remainingSpace / (visibleCount + 1);
                    topOffset = interItemSpacing;
                    break;
            }
        }

        var currentTop = position.Top - topOffset;

        foreach (var child in children)
        {
            if (!child.IsVisible) continue;
            var grow = child is FlexItem item ? (float)item.Grow : 0f;
            var childInitialWidth = child.MeasureWidth();

            float finalChildWidth;
            float childLeft;

            switch (CrossAxisAlignment)
            {
                case CrossAxisAlignment.Stretch:
                    finalChildWidth = position.Width;
                    childLeft = position.Left;
                    break;

                case CrossAxisAlignment.End:
                    finalChildWidth = childInitialWidth;
                    childLeft = position.Right - finalChildWidth;
                    break;

                case CrossAxisAlignment.Start:
                    finalChildWidth = childInitialWidth;
                    childLeft = position.Left;
                    break;

                case CrossAxisAlignment.Center:
                default:
                    finalChildWidth = childInitialWidth;
                    var horizontalPadding = (position.Width - finalChildWidth) / 2f;
                    childLeft = position.Left + horizontalPadding;
                    break;
            }

            var finalChildHeight = child.MeasureHeight(finalChildWidth);
            // Grow items absorb remaining space; when remainingSpace is negative (children
            // overflow the parent) they shrink proportionally so the layout stays bounded.
            // Without this, a Grow-1 child like a scroll pane with tall content keeps its
            // natural height and overflows its container instead of being clipped+scrolled.
            if (grow > 0 && totalFlexGrow > 0)
            {
                finalChildHeight += (grow / totalFlexGrow) * remainingSpace;
                if (finalChildHeight < 0) finalChildHeight = 0;
            }

            child.LeftConstraint = childLeft;
            child.BottomConstraint = currentTop - finalChildHeight;
            child.WidthConstraint = finalChildWidth;
            child.HeightConstraint = finalChildHeight;
            child.LayoutSelf();

            currentTop -= finalChildHeight + Gap + interItemSpacing;
        }
    }
}

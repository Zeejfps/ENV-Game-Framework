namespace ZGF.Gui.Views;

public enum MainAxisAlignment
{
    Start,    // Pack items to the start (left)
    Center,       // Pack items in the center
    End,      // Pack items to the end (right)
    SpaceBetween, // Evenly distribute items, first at start, last at end
    SpaceAround,  // Evenly distribute items with half-size spaces at the ends
    SpaceEvenly   // Evenly distribute items with equal space all around
}

public enum CrossAxisAlignment
{
    Start, // Align to the top
    Center,    // Align to the vertical center
    End,   // Align to the bottom
    Stretch    // Stretch to fill the container's height
}

public sealed class FlexRowView : MultiChildView
{
    private float _gap;
    public float Gap
    {
        get => _gap;
        set => SetField(ref _gap, value);
    }
    
    private CrossAxisAlignment _crossAxisAlignment;
    public CrossAxisAlignment CrossAxisAlignment
    {
        get => _crossAxisAlignment;
        set => SetField(ref _crossAxisAlignment, value);
    }

    private MainAxisAlignment _mainAxisAlignment;
    public MainAxisAlignment MainAxisAlignment
    {
        get => _mainAxisAlignment;
        set => SetField(ref _mainAxisAlignment, value);
    }

    public override float MeasureWidth()
    {
        if (Width.IsSet)
            return Width;

        var totalWidth = 0f;
        var visibleCount = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            totalWidth += child.MeasureWidth();
            visibleCount++;
        }
        var spacing = visibleCount > 0 ? (visibleCount - 1) * Gap : 0f;
        return totalWidth + spacing;
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var children = Children;
        if (children.Count == 0)
        {
            return;
        }

        var totalChildrenInitialWidth = 0f;
        var totalFlexGrow = 0f;
        var visibleCount = 0;

        foreach (var child in children)
        {
            if (!child.IsVisible) continue;
            var grow = child is FlexItem item ? (float)item.Grow : 0f;
            totalChildrenInitialWidth += child.MeasureWidth();
            totalFlexGrow += grow;
            visibleCount++;
        }

        if (visibleCount == 0) return;

        var totalGap = Gap * (visibleCount - 1);
        var totalContentWidth = totalChildrenInitialWidth + totalGap;
        var remainingSpace = position.Width - totalContentWidth;

        var leftOffset = 0f;
        var interItemSpacing = 0f;

        if (remainingSpace > 0)
        {
            switch (MainAxisAlignment)
            {
                case MainAxisAlignment.End:
                    leftOffset = remainingSpace;
                    break;
                case MainAxisAlignment.Center:
                    leftOffset = remainingSpace / 2f;
                    break;
                case MainAxisAlignment.SpaceBetween:
                    interItemSpacing = visibleCount > 1 ? remainingSpace / (visibleCount - 1) : 0;
                    break;
                case MainAxisAlignment.SpaceAround:
                    interItemSpacing = remainingSpace / visibleCount;
                    leftOffset = interItemSpacing / 2f;
                    break;
                case MainAxisAlignment.SpaceEvenly:
                    interItemSpacing = remainingSpace / (visibleCount + 1);
                    leftOffset = interItemSpacing;
                    break;
            }
        }

        var currentLeft = position.Left + leftOffset;

        List<View>? deferredFlexChildren = null;

        foreach (var child in children)
        {
            if (!child.IsVisible) continue;
            var grow = child is FlexItem item ? (float)item.Grow : 0f;
            var childSize = child.MeasureSelf();
            var childInitialWidth = childSize.Width;
            var childInitialHeight = childSize.Height;

            // Calculate final width based on FlexGrow. Negative remainingSpace (children
            // overflow the parent) shrinks Grow items proportionally so the layout stays
            // bounded — see the matching comment in FlexColumnView for the rationale.
            var finalChildWidth = childInitialWidth;
            if (grow > 0 && totalFlexGrow > 0)
            {
                finalChildWidth += (grow / totalFlexGrow) * remainingSpace;
                if (finalChildWidth < 0) finalChildWidth = 0;
            }

            var crossxisAlignment = CrossAxisAlignment;

            // Calculate final height and vertical position
            float finalChildHeight;
            float childBottom; // Y-coordinate of the child's bottom edge

            switch (crossxisAlignment)
            {
                case CrossAxisAlignment.Stretch:
                    finalChildHeight = position.Height;
                    childBottom = position.Bottom;
                    break;

                case CrossAxisAlignment.Start: // Align to Top
                    finalChildHeight = childInitialHeight;
                    childBottom = position.Top - finalChildHeight;
                    break;

                case CrossAxisAlignment.End: // Align to Bottom
                    finalChildHeight = childInitialHeight;
                    childBottom = position.Bottom;
                    break;

                case CrossAxisAlignment.Center:
                default:
                    finalChildHeight = childInitialHeight;
                    var verticalPadding = (position.Height - finalChildHeight) / 2f;
                    childBottom = position.Bottom + verticalPadding;
                    break;
            }

            child.LeftConstraint = currentLeft;
            child.BottomConstraint = childBottom;
            child.WidthConstraint = finalChildWidth;
            child.HeightConstraint = finalChildHeight;

            if (grow > 0)
            {
                (deferredFlexChildren ??= new List<View>()).Add(child);
            }
            else
            {
                child.LayoutSelf();
            }

            currentLeft += finalChildWidth + Gap + interItemSpacing;
        }

        if (deferredFlexChildren != null)
        {
            foreach (var flexChild in deferredFlexChildren)
                flexChild.LayoutSelf();
        }
    }
}
using ZGF.Geometry;

namespace ZGF.Gui.Layouts;

public sealed class FlexColumnView : MultiChildView
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

        foreach (var child in children)
        {
            var grow = child is FlexItem item ? (float)item.Grow : 0f;
            totalChildrenInitialHeight += child.MeasureHeight();
            totalFlexGrow += grow;
        }

        var totalGap = Gap * (children.Count - 1);
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
                    interItemSpacing = children.Count > 1 ? remainingSpace / (children.Count - 1) : 0;
                    break;
                case MainAxisAlignment.SpaceAround:
                    interItemSpacing = remainingSpace / children.Count;
                    topOffset = interItemSpacing / 2f;
                    break;
                case MainAxisAlignment.SpaceEvenly:
                    interItemSpacing = remainingSpace / (children.Count + 1);
                    topOffset = interItemSpacing;
                    break;
            }
        }

        var currentTop = position.Top - topOffset;

        foreach (var child in children)
        {
            var grow = child is FlexItem item ? (float)item.Grow : 0f;
            var childSize = child.MeasureSelf();
            var childInitialWidth = childSize.Width;
            var childInitialHeight = childSize.Height;

            var finalChildHeight = childInitialHeight;
            if (remainingSpace > 0 && grow > 0 && totalFlexGrow > 0)
            {
                finalChildHeight += (grow / totalFlexGrow) * remainingSpace;
            }

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

            child.LeftConstraint = childLeft;
            child.BottomConstraint = currentTop - finalChildHeight;
            child.MinWidthConstraint = finalChildWidth;
            child.MaxWidthConstraint = finalChildWidth;
            child.MaxHeightConstraint = finalChildHeight;
            child.LayoutSelf();

            currentTop -= finalChildHeight + Gap + interItemSpacing;
        }
    }
}

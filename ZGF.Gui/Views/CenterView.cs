namespace ZGF.Gui.Views;

/// <summary>
/// Centers each child within its own bounds. Each child is measured at its
/// intrinsic size and then positioned at the center of this view. Because the
/// measured size is assigned straight to the child's width/height constraint,
/// this view also floors that size by the child's Min*Constraint — the raw
/// measurement doesn't.
/// </summary>
public sealed class CenterView : MultiChildView
{
    protected override void OnLayoutChildren()
    {
        var position = Position;

        foreach (var child in Children)
        {
            var childWidth = child.MeasureWidth();
            if (child.MinWidthConstraint.IsSet && childWidth < child.MinWidthConstraint)
                childWidth = child.MinWidthConstraint;

            var childHeight = child.MeasureHeight(childWidth);
            if (child.MinHeightConstraint.IsSet && childHeight < child.MinHeightConstraint)
                childHeight = child.MinHeightConstraint;

            child.LeftConstraint = position.Left + (position.Width - childWidth) / 2f;
            child.BottomConstraint = position.Bottom + (position.Height - childHeight) / 2f;
            child.WidthConstraint = childWidth;
            child.HeightConstraint = childHeight;
            child.LayoutSelf();
        }
    }
}

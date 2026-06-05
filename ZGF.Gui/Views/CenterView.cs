namespace ZGF.Gui.Views;

/// <summary>
/// Centers each child within its own bounds. Each child is measured at its
/// intrinsic size and then positioned at the center of this view. Because the
/// measured size is assigned straight to the child's width/height constraint,
/// this view also floors that size by the child's Min*Constraint — the raw
/// measurement doesn't.
///
/// The measured size is also capped to this view's own bounds (less
/// <see cref="Margin"/>), so a child can never be laid out larger than the
/// space available to it. A child whose content exceeds that cap must scroll
/// internally; without the cap it would simply overflow and spill past the
/// viewport. This is what guarantees a centered modal never grows past the
/// window it sits in.
/// </summary>
public sealed class CenterView : MultiChildView
{
    /// <summary>
    /// Gap kept between a clamped child and the viewport edge, so a child forced
    /// down to the cap doesn't sit flush against the window border.
    /// </summary>
    public float Margin
    {
        get;
        set => SetField(ref field, value);
    } = 24f;

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var maxWidth = Math.Max(0f, position.Width - Margin * 2f);
        var maxHeight = Math.Max(0f, position.Height - Margin * 2f);

        foreach (var child in Children)
        {
            var childWidth = child.MeasureWidth();
            if (child.MinWidthConstraint.IsSet && childWidth < child.MinWidthConstraint)
                childWidth = child.MinWidthConstraint;
            if (childWidth > maxWidth)
                childWidth = maxWidth;

            var childHeight = child.MeasureHeight(childWidth);
            if (child.MinHeightConstraint.IsSet && childHeight < child.MinHeightConstraint)
                childHeight = child.MinHeightConstraint;
            if (childHeight > maxHeight)
                childHeight = maxHeight;

            child.LeftConstraint = position.Left + (position.Width - childWidth) / 2f;
            child.BottomConstraint = position.Bottom + (position.Height - childHeight) / 2f;
            child.WidthConstraint = childWidth;
            child.HeightConstraint = childHeight;
            child.LayoutSelf();
        }
    }
}

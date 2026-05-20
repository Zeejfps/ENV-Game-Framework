namespace ZGF.Gui.Views;

/// <summary>
/// Centers each child within its own bounds. Each child is measured at its
/// intrinsic size and then positioned at the center of this view.
/// </summary>
public sealed class CenterView : MultiChildView
{
    protected override void OnLayoutChildren()
    {
        var position = Position;

        foreach (var child in Children)
        {
            var childWidth = child.MeasureWidth();
            child.WidthConstraint = childWidth;
            var childHeight = child.MeasureHeight();

            child.LeftConstraint = position.Left + (position.Width - childWidth) / 2f;
            child.BottomConstraint = position.Bottom + (position.Height - childHeight) / 2f;
            child.HeightConstraint = childHeight;
            child.LayoutSelf();
        }
    }
}

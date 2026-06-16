namespace ZGF.Gui.Views;

/// <summary>
/// Insets its children by a <see cref="PaddingStyle"/>. No background, border, or
/// drawing — use this for pure spacing. Reach for <see cref="RectView"/> only when
/// you also need to paint a box.
/// </summary>
public sealed class PaddingView : View
{
    public new ChildrenCollection Children => base.Children;

    private PaddingStyle _padding;

    public PaddingStyle Padding
    {
        get => _padding;
        set => SetField(ref _padding, value);
    }

    protected override float MeasureWidthIntrinsic()
    {
        var width = base.MeasureWidthIntrinsic();
        width += _padding.Left + _padding.Right;
        return width;
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        var childAvailableWidth = availableWidth > 0f
            ? availableWidth - _padding.Left - _padding.Right
            : availableWidth;
        var height = base.MeasureHeightIntrinsic(childAvailableWidth);
        height += _padding.Top + _padding.Bottom;
        return height;
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var left = position.Left + _padding.Left;
        var right = position.Right - _padding.Right;
        var top = position.Top - _padding.Top;
        var bottom = position.Bottom + _padding.Bottom;

        foreach (var child in Children)
        {
            child.LeftConstraint = left;
            child.BottomConstraint = bottom;
            child.WidthConstraint = right - left;
            child.HeightConstraint = top - bottom;
            child.LayoutSelf();
        }
    }
}

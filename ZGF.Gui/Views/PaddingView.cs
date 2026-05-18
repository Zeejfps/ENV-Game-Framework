namespace ZGF.Gui.Layouts;

/// <summary>
/// Insets its children by a <see cref="PaddingStyle"/>. No background, border, or
/// drawing — use this for pure spacing. Reach for <see cref="RectView"/> only when
/// you also need to paint a box.
/// </summary>
public sealed class PaddingView : MultiChildView
{
    private PaddingStyle _padding;

    public PaddingStyle Padding
    {
        get => _padding;
        set => SetField(ref _padding, value);
    }

    public override float MeasureWidth()
    {
        var width = base.MeasureWidth();
        width += _padding.Left + _padding.Right;
        return width;
    }

    public override float MeasureHeight()
    {
        var height = base.MeasureHeight();
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

namespace ZGF.Gui;

public class RectView : View
{
    private readonly RectStyle _style = new();

    public StyleValue<uint> BackgroundColor
    {
        get => _style.BackgroundColor;
        set => SetField(ref _style.BackgroundColor, value);
    }

    public PaddingStyle Padding
    {
        get => _style.Padding;
        set => SetField(ref _style.Padding, value);
    }

    public BorderColorStyle BorderColor
    {
        get => _style.BorderColor;
        set => SetField(ref _style.BorderColor, value);
    }

    public BorderSizeStyle BorderSize
    {
        get => _style.BorderSize;
        set => SetField(ref _style.BorderSize, value);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        c.AddCommand(new DrawRectCommand
        {
            Position = Position,
            Style = _style,
            ZIndex = ZIndex
        });
    }

    public override float MeasureWidth()
    {
        var width= base.MeasureWidth();
        var padding = Padding;
        var borderSize = _style.BorderSize;
        width += padding.Left + padding.Right + borderSize.Left + borderSize.Right;
        return width;
    }

    public override float MeasureHeight()
    {
        var height = base.MeasureHeight();
        var padding = Padding;
        var borderSize = _style.BorderSize;
        height += padding.Top + padding.Bottom + borderSize.Top + borderSize.Bottom;
        return height;
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var padding = _style.Padding;
        var border = _style.BorderSize;
        
        var left = position.Left + padding.Left + border.Left;
        var right = position.Right - padding.Right - border.Right;
        var top = position.Top - padding.Top - border.Top;
        var bottom = position.Bottom + padding.Bottom + border.Bottom;
        
        foreach (var child in Children)
        {
            child.LeftConstraint = left;
            child.BottomConstraint = bottom;
            child.MinWidthConstraint = right - left;
            child.MaxWidthConstraint = right - left;
            child.MaxHeightConstraint = top - bottom;
            child.LayoutSelf();
        }
    }

    protected override void OnApplyStyle(Style style)
    {
        base.OnApplyStyle(style);
        _style.Apply(style);
    }
}
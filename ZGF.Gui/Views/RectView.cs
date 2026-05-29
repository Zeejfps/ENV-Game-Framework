namespace ZGF.Gui;

public class RectView : MultiChildView
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

    public BorderRadiusStyle BorderRadius
    {
        get => _style.BorderRadius;
        set => SetField(ref _style.BorderRadius, value);
    }

    public BoxShadowStyle BoxShadow
    {
        get => _style.BoxShadow;
        set => SetField(ref _style.BoxShadow, value);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var z = GetDrawZIndex();

        if (_style.BoxShadow.IsActive)
        {
            c.DrawBoxShadow(new DrawBoxShadowInputs
            {
                Position = Position,
                BorderRadius = _style.BorderRadius,
                Shadow = _style.BoxShadow,
                ZIndex = z,
            });
        }

        c.DrawRect(new DrawRectInputs
        {
            Position = Position,
            Style = _style,
            ZIndex = z,
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

    public override float MeasureHeight(float availableWidth)
    {
        var padding = Padding;
        var borderSize = _style.BorderSize;
        var horizontalChrome = padding.Left + padding.Right + borderSize.Left + borderSize.Right;
        // Subtract our own chrome from the width available to children so they wrap correctly.
        // A non-positive availableWidth means "unconstrained" — leave it as-is so the convention
        // propagates down to descendants.
        var childAvailableWidth = availableWidth > 0f ? availableWidth - horizontalChrome : availableWidth;
        var height = base.MeasureHeight(childAvailableWidth);
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
            child.WidthConstraint = right - left;
            child.HeightConstraint = top - bottom;
            child.LayoutSelf();
        }
    }
}
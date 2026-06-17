namespace ZGF.Gui.Views;

public class RectView : View
{
    public new ChildrenCollection Children => base.Children;

    private readonly RectStyle _style = new();

    public uint BackgroundColor
    {
        get => _style.BackgroundColor;
        set => _style.BackgroundColor = value;
    }

    public BorderColorStyle BorderColor
    {
        get => _style.BorderColor;
        set => _style.BorderColor = value;
    }

    public BorderSizeStyle BorderSize
    {
        get => _style.BorderSize;
        set => SetField(ref _style.BorderSize, value);
    }

    public BorderRadiusStyle BorderRadius
    {
        get => _style.BorderRadius;
        set => _style.BorderRadius = value;
    }

    public BoxShadowStyle BoxShadow
    {
        get => _style.BoxShadow;
        set => _style.BoxShadow = value;
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

    protected override float MeasureWidthIntrinsic()
    {
        // An explicit Width is the outer (border-box) size, matching how ResolveWidth
        // consumes it — the border lives inside it, so don't add it on top.
        if (Width.IsSet) return Width;

        var width= base.MeasureWidthIntrinsic();
        var borderSize = _style.BorderSize;
        width += borderSize.Left + borderSize.Right;
        return width;
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        // An explicit Height is the outer (border-box) size, matching how ResolveHeight
        // consumes it. Adding the border here would make a parent reserve more than the view
        // lays out at, leaving a gap on the anchored edge.
        if (Height.IsSet) return Height;

        var borderSize = _style.BorderSize;
        var horizontalBorder = borderSize.Left + borderSize.Right;
        // Subtract our own border from the width available to children so they wrap correctly.
        // A non-positive availableWidth means "unconstrained" — leave it as-is so the convention
        // propagates down to descendants.
        var childAvailableWidth = availableWidth > 0f ? availableWidth - horizontalBorder : availableWidth;
        var height = base.MeasureHeightIntrinsic(childAvailableWidth);
        height += borderSize.Top + borderSize.Bottom;
        return height;
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var border = _style.BorderSize;

        var left = position.Left + border.Left;
        var right = position.Right - border.Right;
        var top = position.Top - border.Top;
        var bottom = position.Bottom + border.Bottom;

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
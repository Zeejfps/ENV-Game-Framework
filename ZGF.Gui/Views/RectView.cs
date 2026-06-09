using ZGF.Geometry;

namespace ZGF.Gui.Views;

public class RectView : LayoutView
{
    private readonly RectStyle _style = new();

    public uint BackgroundColor
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

    protected override Size MeasureContent(Constraints c)
    {
        var hChrome = _style.Padding.Left + _style.Padding.Right + _style.BorderSize.Left + _style.BorderSize.Right;
        var vChrome = _style.Padding.Top + _style.Padding.Bottom + _style.BorderSize.Top + _style.BorderSize.Bottom;
        var inner = c.Deflate(hChrome, vChrome);

        var w = 0f;
        var h = 0f;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            var s = child.Measure(inner);
            if (s.Width > w) w = s.Width;
            if (s.Height > h) h = s.Height;
        }
        return new Size(w + hChrome, h + vChrome);
    }

    protected override void ArrangeContent(RectF bounds)
    {
        var padding = _style.Padding;
        var border = _style.BorderSize;

        var left = bounds.Left + padding.Left + border.Left;
        var bottom = bounds.Bottom + padding.Bottom + border.Bottom;
        var width = Math.Max(0f, bounds.Width - padding.Left - padding.Right - border.Left - border.Right);
        var height = Math.Max(0f, bounds.Height - padding.Top - padding.Bottom - border.Top - border.Bottom);
        var inner = new RectF(left, bottom, width, height);

        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            child.Measure(Constraints.Tight(width, height));
            child.Arrange(inner);
        }
    }
}
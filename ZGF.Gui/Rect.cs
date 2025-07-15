using ZGF.Geometry;

namespace ZGF.Gui;

public class Rect : Component
{
    private RectStyle _style = new();
    public RectStyle Style
    {
        get => _style;
        set => SetField(ref _style, value);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        c.AddCommand(new DrawRectCommand
        {
            Position = Position,
            Style = Style
        });
        base.OnDrawSelf(c);
    }

    protected override void OnLayoutSelf()
    {
        Position = Constraints;
        var position = Position;
        var padding = Style.Padding;
        var left = position.Left + padding.Left;
        var right = position.Right - padding.Right;
        var top = position.Top - padding.Top;
        var bottom = position.Bottom + padding.Bottom;
        var width = right - left;
        var height = top - bottom;
        foreach (var child in Children)
        {
            child.Constraints = new RectF
            {
                Left = left,
                Bottom = bottom,
                Width = width,
                Height = height,
            };
            child.LayoutSelf();
        }
    }

    protected override void OnApplyStyleSheet(StyleSheet styleSheet)
    {
        if (styleSheet.TryGetByClass(ClassId, out var style))
        {

        }
    }
}
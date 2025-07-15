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
        var border = Style.BorderSize;
        
        var left = position.Left + padding.Left + border.Left;
        var right = position.Right - padding.Right - border.Right;
        var top = position.Top - padding.Top - border.Top;
        var bottom = position.Bottom + padding.Bottom + border.Bottom;
        
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
        if (styleSheet.TryGetByClass(ClassId, out var classStyle))
        {

        }
        
        if (styleSheet.TryGetById(Id, out var idStyle))
        {
            idStyle.Padding.Apply(ref Style.Padding);
            idStyle.BorderSize.Apply(ref Style.BorderSize);
        }
        
        SetDirty();
    }
}
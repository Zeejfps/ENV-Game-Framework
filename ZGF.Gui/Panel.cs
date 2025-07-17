using ZGF.Geometry;

namespace ZGF.Gui;

public class Panel : Component
{
    private RectStyle _style = new();
    public RectStyle Style => _style;

    public StyleValue<uint> BackgroundColor
    {
        get => _style.BackgroundColor;
        set => SetField(ref _style.BackgroundColor, value);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        c.AddCommand(new DrawRectCommand
        {
            Position = Position,
            Style = Style
        });
    }

    protected override void OnLayoutChildren()
    {
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

    protected override void OnStyleSheetApplied(StyleSheet styleSheet)
    {
        foreach (var styleClass in StyleClasses)
        {
            if (styleSheet.TryGetByClass(styleClass, out var classStyle))
            {
                Style.Apply(classStyle);
            }
        }
        
        if (styleSheet.TryGetById(Id, out var idStyle))
        {
            Style.Apply(idStyle);
        }
        
        base.OnStyleSheetApplied(styleSheet);
    }
}
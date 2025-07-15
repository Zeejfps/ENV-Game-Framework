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

    protected override void OnStyleSheetApplied(StyleSheet styleSheet)
    {
        Console.WriteLine($"Applying style sheet to {GetType()}");
        if (styleSheet.TryGetByClass(ClassId, out var classStyle))
        {
            if (classStyle.BackgroundColor.IsSet)
                Style.BackgroundColor = classStyle.BackgroundColor.Value;
            
            classStyle.Padding.ApplyTo(ref Style.Padding);
            classStyle.BorderSize.ApplyTo(ref Style.BorderSize);
            classStyle.BorderColor.ApplyTo(ref Style.BorderColor);
        }
        
        if (styleSheet.TryGetById(Id, out var idStyle))
        {
            if (idStyle.BackgroundColor.IsSet)
                Style.BackgroundColor = idStyle.BackgroundColor.Value;
            
            idStyle.Padding.ApplyTo(ref Style.Padding);
            idStyle.BorderSize.ApplyTo(ref Style.BorderSize);
            idStyle.BorderColor.ApplyTo(ref Style.BorderColor);
        }
        
        SetDirty();
        
        base.ApplyStyleSheetToChildren(styleSheet);
    }
}
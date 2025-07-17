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

    public PaddingStyle Padding
    {
        get => _style.Padding;
        set => SetField(ref _style.Padding, value);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        c.AddCommand(new DrawRectCommand
        {
            Position = Position,
            Style = Style
        });
    }

    public override float MeasureWidth()
    {
        var width= base.MeasureWidth();
        var padding = Padding;
        var borderSize = Style.BorderSize;
        width += padding.Left + padding.Right + borderSize.Left + borderSize.Right;
        return width;
    }

    public override float MeasureHeight()
    {
        var height = base.MeasureHeight();
        var padding = Padding;
        var borderSize = Style.BorderSize;
        height += padding.Top + padding.Bottom + borderSize.Top + borderSize.Bottom;
        return height;
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
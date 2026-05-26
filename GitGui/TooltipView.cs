using ZGF.Geometry;
using ZGF.Gui;

namespace GitGui;

public sealed class TooltipView : MultiChildView
{
    private const int HorizontalPadding = 8;
    private const int VerticalPadding = 4;

    public TooltipView(string text)
    {
        var background = new RectView
        {
            Padding = new PaddingStyle
            {
                Left = HorizontalPadding,
                Right = HorizontalPadding,
                Top = VerticalPadding,
                Bottom = VerticalPadding,
            },
            Children =
            {
                new TextView { Text = text },
            },
        };
        background.StyleClasses.Add(StyleClassNames.Tooltip);
        ((TextView)background.Children[0]).StyleClasses.Add(StyleClassNames.TooltipText);
        AddChildToSelf(background);
    }

    protected override void OnLayoutSelf()
    {
        var width = MeasureWidth();
        var height = MeasureHeight(width);
        Position = new RectF { Left = 0, Bottom = 0, Width = width, Height = height };
    }
}

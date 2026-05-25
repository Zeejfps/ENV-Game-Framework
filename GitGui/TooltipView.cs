using ZGF.Geometry;
using ZGF.Gui;

namespace GitGui;

public sealed class TooltipView : MultiChildView
{
    private const int HorizontalPadding = 8;
    private const int VerticalPadding = 4;
    private const float CornerRadius = 4f;

    public TooltipView(string text)
    {
        AddChildToSelf(new RectView
        {
            BackgroundColor = TooltipPalette.Background,
            BorderColor = BorderColorStyle.All(TooltipPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(CornerRadius),
            Padding = new PaddingStyle
            {
                Left = HorizontalPadding,
                Right = HorizontalPadding,
                Top = VerticalPadding,
                Bottom = VerticalPadding,
            },
            BoxShadow = new BoxShadowStyle
            {
                OffsetX = 0f,
                OffsetY = -4f,
                Blur = 16f,
                Spread = 0f,
                Color = TooltipPalette.ShadowColor,
            },
            Children =
            {
                new TextView
                {
                    Text = text,
                    TextColor = TooltipPalette.Text,
                    FontSize = 12,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            },
        });
    }

    protected override void OnLayoutSelf()
    {
        var width = MeasureWidth();
        var height = MeasureHeight(width);
        Position = new RectF { Left = 0, Bottom = 0, Width = width, Height = height };
    }
}

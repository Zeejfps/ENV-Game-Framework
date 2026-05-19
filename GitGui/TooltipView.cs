using ZGF.Geometry;
using ZGF.Gui;

namespace GitGui;

public sealed class TooltipView : MultiChildView
{
    private const float Gap = 8f;
    private const float EdgeMargin = 4f;
    private const int HorizontalPadding = 8;
    private const int VerticalPadding = 4;
    private const float CornerRadius = 4f;

    private RectF _anchorRect;
    public RectF AnchorRect
    {
        get => _anchorRect;
        set => SetField(ref _anchorRect, value);
    }

    public TooltipView(string text)
    {
        ZIndex = 1;

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
        var height = MeasureHeight();
        var anchor = _anchorRect;

        var left = anchor.Center.X - width * 0.5f;
        var bottom = anchor.Bottom - Gap - height;

        if (Parent is { } parent)
        {
            var bounds = parent.Position;

            // If the natural placement (below the anchor) would clip the bottom edge,
            // flip above so the tooltip stays fully on-screen.
            if (bottom < bounds.Bottom + EdgeMargin
                && anchor.Top + Gap + height <= bounds.Top - EdgeMargin)
            {
                bottom = anchor.Top + Gap;
            }

            // Horizontal clamp: prefer keeping the tooltip on-screen over keeping
            // it centered when the anchor is near a side.
            var minLeft = bounds.Left + EdgeMargin;
            var maxLeft = bounds.Right - EdgeMargin - width;
            if (maxLeft < minLeft) maxLeft = minLeft;
            if (left < minLeft) left = minLeft;
            else if (left > maxLeft) left = maxLeft;

            // Vertical clamp as a final guard if neither side fits the tooltip.
            var minBottom = bounds.Bottom + EdgeMargin;
            var maxBottom = bounds.Top - EdgeMargin - height;
            if (maxBottom < minBottom) maxBottom = minBottom;
            if (bottom < minBottom) bottom = minBottom;
            else if (bottom > maxBottom) bottom = maxBottom;
        }

        Position = new RectF
        {
            Left = left,
            Bottom = bottom,
            Width = width,
            Height = height,
        };
    }
}

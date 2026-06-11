using ZGF.Gui.Components;
using ZGF.Gui.Views;

namespace ZGF.Gui.Sandbox;

public sealed record WindowTitleBar : Widget
{
    public required string Title { get; init; }

    protected override View CreateView(Context ctx)
    {
        var button = new RectView
        {
            Width = 10f,
            BackgroundColor = 0xFF000000,
            Padding = PaddingStyle.All(1),
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0xFF9C9C9C, Top = 0xFF9C9C9C,
                Right = 0xFFFFFFFF, Bottom = 0xFFFFFFFF
            },
        };

        var button2 = new RectView
        {
            Width = 10f,
            BackgroundColor = 0xFF000000,
            Padding = PaddingStyle.All(1),
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0xFF9C9C9C, Top = 0xFF9C9C9C,
                Right = 0xFFFFFFFF, Bottom = 0xFFFFFFFF
            },
        };

        var titleTextView = new TextView(ctx.Canvas)
        {
            Text = Title,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var row = new FlexRowView
        {
            Gap = 3,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                button,
                new FlexItem { Grow = 1f, Child = titleTextView },
                button2,
            }
        };

        return new RectView
        {
            Height = 30f,
            BackgroundColor = 0xFFCECECE,
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFFFF,
                Left = 0xFFFFFFFF,
                Right = 0xFF9C9C9C,
            },
            BorderSize = new BorderSizeStyle
            {
                Top = 1,
                Left = 1,
                Right = 1,
            },
            Padding = PaddingStyle.All(3),
            Children =
            {
                row
            }
        };
    }
}

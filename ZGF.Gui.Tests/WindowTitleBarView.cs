using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

public sealed class WindowTitleBarView : MultiChildView
{
    private readonly TextView _titleTextView;

    public string? TitleText
    {
        get => _titleTextView.Text;
        set => _titleTextView.Text = value;
    }

    public WindowTitleBarView(string title)
    {
        Height = 30f;
        
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

        _titleTextView = new TextView
        {
            Text = title,
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
                new FlexItem { Grow = 1f, Child = _titleTextView },
                button2,
            }
        };
        
        var background = new RectView
        {
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
        
        AddChildToSelf(background);
    }

    public override string ToString()
    {
        return $"TitleBar - {TitleText} - {ZIndex}";
    }
}
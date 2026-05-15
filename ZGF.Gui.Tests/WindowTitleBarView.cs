using ZGF.Gui.Layouts;

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
        PreferredHeight = 30f;
        
        var button = new RectView
        {
            StyleClasses =
            {
                "inset_panel",
                "window_button"
            }
        };

        var button2 = new RectView
        {
            StyleClasses =
            {
                "inset_panel",
                "window_button"
            }
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
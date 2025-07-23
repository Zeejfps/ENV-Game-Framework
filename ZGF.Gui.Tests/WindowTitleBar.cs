using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class WindowTitleBar : Component
{
    private readonly Label _titleLabel;

    public string? TitleText
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public WindowTitleBar(string title)
    {
        PreferredHeight = 30f;
        
        var button = new Panel
        {
            StyleClasses =
            {
                "inset_panel",
                "window_button"
            }
        };

        var button2 = new Panel
        {
            StyleClasses =
            {
                "inset_panel",
                "window_button"
            }
        };

        _titleLabel = new Label
        {
            Text = title,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var row = new FlexRow
        {
            Gap = 3,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                button,
                _titleLabel,
                button2
            }
        };
        
        row.UpdateStyle(_titleLabel, new FlexStyle
        {
            Grow = 1f,
        });
        
        var background = new Panel
        {
            BackgroundColor = 0xCECECE,
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFF,
                Left = 0xFFFFFF,
                Right = 0x9C9C9C,
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
        
        Add(background);
    }

    public override string ToString()
    {
        return $"TitleBar - {TitleText} - {ZIndex}";
    }
}
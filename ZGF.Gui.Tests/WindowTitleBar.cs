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
            Padding = PaddingStyle.All(3)
        };

        var row = new FlexRow
        {
            Gap = 3,
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };
        background.Add(row);


        var button = new Panel();
        button.AddStyleClass("inset_panel");
        button.AddStyleClass("window_button");

        var button2 = new Panel();
        button2.AddStyleClass("inset_panel");
        button2.AddStyleClass("window_button");

        _titleLabel = new Label
        {
            Text = title,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        row.Add(button);
        row.Add(_titleLabel, new FlexStyle
        {
            Grow = 1f,
        });
        row.Add(button2);

        Add(background);
    }

    public override string ToString()
    {
        return $"TitleBar - {TitleText} - {ZIndex}";
    }
}
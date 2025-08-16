using ZGF.Gui;

namespace LLMit.Views;

public sealed class TabView : View
{
    private bool _isHighlighted;
    private readonly TextView _text;

    public string? Text
    {
        get => _text.Text;
        set => _text.Text = value;
    }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value);
    }

    public TabView()
    {
        PreferredWidth = 150;

        _text = new TextView
        {
            Text = "New Chat",
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = 0xFFFFFFFF
        };

        var bg = new RectView
        {
            BackgroundColor = 0xFF212121,
            Padding = PaddingStyle.All(6),
            BorderSize = new BorderSizeStyle
            {
                Top = 1,
                Right = 1,
                Left = 1
            },
            BorderColor = BorderColorStyle.All(0xFF4f4f4f),
            Children =
            {
                _text
            }
        };

        AddChildToSelf(bg);
        ZIndex = 10;
    }

    protected override void OnLayoutSelf()
    {
        base.OnLayoutSelf();
        Position = Position with { Bottom = Position.Bottom - 1, Height = Position.Height + 1 };
    }
}
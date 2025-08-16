using ZGF.Gui;

namespace LLMit.Views;

public sealed class TabView : View
{
    private bool _isHighlighted;
    private readonly TextView _text;
    private readonly RectView _bg;

    public string? Text
    {
        get => _text.Text;
        set => _text.Text = value;
    }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (SetField(ref _isHighlighted, value))
            {
                if (_isHighlighted)
                {
                    ZIndex = 10;
                    _bg.BackgroundColor = 0xFF212121;
                    _bg.BorderSize = new BorderSizeStyle
                    {
                        Top = 1,
                        Right = 1,
                        Left = 1
                    };
                    _bg.BorderColor = BorderColorStyle.All(0xFF4f4f4f);
                }
                else
                {
                    ZIndex = 0;
                    _bg.BackgroundColor = 0xFF111111;
                    _bg.BorderSize = new BorderSizeStyle
                    {
                        Top = 1,
                        Right = 1,
                        Left = 0
                    };
                    _bg.BorderColor = BorderColorStyle.All(0xFF4f4f4f);
                }
            }
        }
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

        _bg = new RectView
        {
            BackgroundColor = 0xFF111111,
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

        AddChildToSelf(_bg);
    }

    protected override void OnLayoutSelf()
    {
        base.OnLayoutSelf();
        if (IsHighlighted)
        {
            Position = Position with { Bottom = Position.Bottom - 1, Height = Position.Height + 1 };
        }
        else
        {
            Position = Position with { Bottom = Position.Bottom + 1, Height = Position.Height - 2 };
        }
    }
}
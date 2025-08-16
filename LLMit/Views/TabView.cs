using ZGF.Gui;

namespace LLMit.Views;

public sealed class TabView : View
{
    private bool _isActive;
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
        set => SetField(ref _isHighlighted, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetField(ref _isActive, value))
            {

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

        if (_isActive)
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
            Position = Position with { Bottom = Position.Bottom - 1, Height = Position.Height + 1 };
        }
        else
        {
            ZIndex = 0;
            _bg.BackgroundColor = 0xFF111111;
            _bg.BorderSize = new BorderSizeStyle
            {
                Top = 1,
                Right = 1,
                Left = 1
            };
            _bg.BorderColor = BorderColorStyle.All(0xFF4f4f4f);
            Position = Position with { Bottom = Position.Bottom, Height = Position.Height - 2 };
        }

        if (_isHighlighted)
        {
            _bg.BackgroundColor = 0xFF4F4F4F;
        }
    }
}
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemData
{
    public string Text { get; set; }
}

public sealed class ContextMenuItem : MultiChildView
{
    private StyleValue<uint> _normalBackgroundColor;
    public StyleValue<uint> NormalBackgroundColor
    {
        get => _normalBackgroundColor;
        set
        {
            _normalBackgroundColor = value;
            if (!_isSelected)
            {
                _bg.BackgroundColor = _normalBackgroundColor;
            }
        }
    }
    
    private StyleValue<uint> _selectedBackgroundColor;
    public StyleValue<uint> SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set => SetField(ref _selectedBackgroundColor, value);
    }

    private readonly RectView _bg;
    private readonly ImageView _arrowIcon;
    private readonly TextView _iconView;
    private readonly TextView _textView;

    public string? Text
    {
        get => _textView.Text;
        set => _textView.Text = value;
    }

    public string? Icon
    {
        get => _iconView.Text;
        set => _iconView.Text = value;
    }

    public StyleValue<string> IconFontFamily
    {
        get => _iconView.FontFamily;
        set => _iconView.FontFamily = value;
    }

    public StyleValue<uint> TextColor
    {
        get => _textView.TextColor;
        set
        {
            _textView.TextColor = value;
            _iconView.TextColor = value;
        }
    }

    public StyleValue<BorderColorStyle> BorderColor
    {
        get => _bg.BorderColor;
        set => _bg.BorderColor = value;
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetField(ref _isSelected, value))
            {
                if (_isSelected)
                {
                    _bg.BackgroundColor = SelectedBackgroundColor;
                }
                else
                {
                    _bg.BackgroundColor = NormalBackgroundColor;
                }
            }
        }
    }
    
    private bool _isArrowVisible;
    public bool IsArrowVisible
    {
        get => _isArrowVisible;
        set
        {
            if (SetField(ref _isArrowVisible, value))
            {
                if (_isArrowVisible)
                {
                    _arrowIcon.ImageId = "Assets/Icons/arrow_right.png";
                }
                else
                {
                    _arrowIcon.ImageId = null;
                }
            }
        }
    }
    
    public ContextMenuItem()
    {
        ZIndex = 2;
        _selectedBackgroundColor= 0xFFE6E6E6;

        _arrowIcon = new ImageView
        {
            PreferredWidth = 20,
            PreferredHeight = 20,
            TintColor = 0x0
        };

        _iconView = new TextView
        {
            FontSize = 14,
            PreferredWidth = 16,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        _textView = new TextView
        {
            VerticalTextAlignment = TextAlignment.Center,
        };

        var row = new RowView
        {
            Gap = 6,
            Children =
            {
                _iconView,
                _textView,
                _arrowIcon,
            }
        };
        
        _bg = new RectView
        {
            BackgroundColor = 0xFFDEDEDE,
            Padding = PaddingStyle.All(6),
            Children =
            {
                row
            }
        };

        AddChildToSelf(_bg);
    }
}
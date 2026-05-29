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
    private RowView _row = null!;
    private MultiChildView _labelView = null!;

    public string? Text
    {
        get => _textView.Text;
        set => _textView.Text = value;
    }

    public string? Icon
    {
        get => _iconView.Text;
        set
        {
            _iconView.Text = value;
            // Detach the icon view entirely when no icon is provided so the menu doesn't
            // reserve a 16-px column of empty space on the left for icon-less items.
            var hasIcon = !string.IsNullOrEmpty(value);
            var isInRow = _row.Children.Count > 0 && ReferenceEquals(_row.Children[0], _iconView);
            if (hasIcon && !isInRow)
                _row.Children.Insert(0, _iconView);
            else if (!hasIcon && isInRow)
                _row.Children.Remove(_iconView);
        }
    }

    public StyleValue<string> IconFontFamily
    {
        get => _iconView.FontFamily;
        set => _iconView.FontFamily = value;
    }

    private StyleValue<uint> _textColor;
    public StyleValue<uint> TextColor
    {
        get => _textColor;
        set
        {
            _textColor = value;
            ApplyForegroundColor();
        }
    }

    private StyleValue<uint> _disabledTextColor = 0x80B5B9C0;
    public StyleValue<uint> DisabledTextColor
    {
        get => _disabledTextColor;
        set
        {
            _disabledTextColor = value;
            ApplyForegroundColor();
        }
    }

    public StyleValue<BorderColorStyle> BorderColor
    {
        get => _bg.BorderColor;
        set => _bg.BorderColor = value;
    }

    private bool _isEnabled = true;
    // Disabled items render with DisabledTextColor and skip hover/click handling
    // (see ContextMenuItemDefaultKbmController). Background stays at NormalBackgroundColor.
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                ApplyForegroundColor();
                if (!_isEnabled && _isSelected)
                {
                    _isSelected = false;
                    _bg.BackgroundColor = NormalBackgroundColor;
                }
            }
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!_isEnabled) value = false;
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

    private void ApplyForegroundColor()
    {
        var color = _isEnabled ? _textColor : _disabledTextColor;
        _textView.TextColor = color;
        _iconView.TextColor = color;
    }

    public void SetLabelView(MultiChildView labelView)
    {
        var idx = -1;
        for (var i = 0; i < _row.Children.Count; i++)
        {
            if (ReferenceEquals(_row.Children[i], _labelView)) { idx = i; break; }
        }
        if (idx < 0) return;
        _row.Children.Remove(_labelView);
        _row.Children.Insert(idx, labelView);
        _labelView = labelView;
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
            Width = 20,
            Height = 20,
            TintColor = 0x0
        };

        _iconView = new TextView
        {
            FontSize = 14,
            Width = 16,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        _textView = new TextView
        {
            VerticalTextAlignment = TextAlignment.Center,
        };

        _labelView = _textView;
        _row = new RowView
        {
            Gap = 6,
            Children =
            {
                _iconView,
                _labelView,
                _arrowIcon,
            }
        };

        _bg = new RectView
        {
            BackgroundColor = 0xFFDEDEDE,
            Padding = PaddingStyle.All(6),
            Children =
            {
                _row
            }
        };

        AddChildToSelf(_bg);
    }
}
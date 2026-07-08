using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.ContextMenu;

public sealed class ContextMenuItemData
{
    public string Text { get; set; }
}

public sealed class ContextMenuItem : View
{
    private uint _normalBackgroundColor;
    public uint NormalBackgroundColor
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
    
    private uint _selectedBackgroundColor;
    public uint SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set => SetField(ref _selectedBackgroundColor, value);
    }

    private readonly RectView _bg;
    private readonly TextView _arrowView;
    private readonly TextView _iconView;
    private readonly TextView _textView;
    private readonly TextView _shortcutView;
    private FlexRowView _row = null!;
    private View _labelView = null!;

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
            SyncIconColumn();
        }
    }

    private bool _reserveIconColumn;
    // Keeps the leading icon column present even for an icon-less row, so the labels of a
    // menu that mixes checked and unchecked items stay aligned and don't jump as the
    // check appears and disappears.
    public bool ReserveIconColumn
    {
        get => _reserveIconColumn;
        set
        {
            _reserveIconColumn = value;
            SyncIconColumn();
        }
    }

    // Detach the icon view entirely when there's no icon and no reserved column, so a plain
    // menu doesn't carry a 16-px gap on the left; keep it otherwise for alignment.
    private void SyncIconColumn()
    {
        var present = _reserveIconColumn || !string.IsNullOrEmpty(_iconView.Text);
        var isInRow = _row.Children.Count > 0 && ReferenceEquals(_row.Children[0], _iconView);
        if (present && !isInRow)
            _row.Children.Insert(0, _iconView);
        else if (!present && isInRow)
            _row.Children.Remove(_iconView);
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

    // Rounds the row's fill so the hover and active-selection highlights read as soft pills
    // rather than full-bleed rectangles. Defaults to square.
    public StyleValue<BorderRadiusStyle> BackgroundCornerRadius
    {
        get => _bg.BorderRadius;
        set => _bg.BorderRadius = value;
    }

    // Accelerator hint (e.g. "Enter", "Delete") shown just after the label. Null/empty
    // hides it so plain items keep their tight, content-sized width.
    private string? _shortcut;
    public string? Shortcut
    {
        get => _shortcut;
        set
        {
            _shortcut = value;
            _shortcutView.Text = value;
            var hasShortcut = !string.IsNullOrEmpty(value);
            var isInRow = _row.Children.Contains(_shortcutView);
            if (hasShortcut && !isInRow)
            {
                var insertAt = IndexInRow(_arrowView);
                if (insertAt < 0) insertAt = _row.Children.Count;
                _row.Children.Insert(insertAt, _shortcutView);
            }
            else if (!hasShortcut && isInRow)
            {
                _row.Children.Remove(_shortcutView);
            }
        }
    }

    private StyleValue<uint> _shortcutColor = 0x80B5B9C0;
    public StyleValue<uint> ShortcutColor
    {
        get => _shortcutColor;
        set
        {
            _shortcutColor = value;
            _shortcutView.TextColor = value;
        }
    }

    public bool HasShortcut => !string.IsNullOrEmpty(_shortcut);

    // Intrinsic label width with any column override removed, so the owning menu can find
    // the widest label among shortcut items and align them all into one shortcut column.
    public float MeasureLabelWidth()
    {
        _labelView.Width = StyleValue<float>.Unset;
        return _labelView.MeasureWidth();
    }

    // Fixes the label cell to a shared width so shortcuts line up in a column rather than
    // hugging each individual label.
    public void SetLabelColumnWidth(float width) => _labelView.Width = width;

    private int IndexInRow(View child)
    {
        for (var i = 0; i < _row.Children.Count; i++)
            if (ReferenceEquals(_row.Children[i], child)) return i;
        return -1;
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
        _arrowView.TextColor = color;
    }

    public void SetLabelView(View labelView)
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
    
    // Submenu indicator glyph (e.g. a chevron), shown right-aligned when IsArrowVisible.
    // Rendered as text rather than an image so it follows the icon font in use — set
    // ArrowFontFamily to the same font the glyph comes from.
    public string? ArrowGlyph { get; set; }

    public StyleValue<string> ArrowFontFamily
    {
        get => _arrowView.FontFamily;
        set => _arrowView.FontFamily = value;
    }

    private bool _isArrowVisible;
    public bool IsArrowVisible
    {
        get => _isArrowVisible;
        set
        {
            if (SetField(ref _isArrowVisible, value))
            {
                _arrowView.Text = _isArrowVisible ? ArrowGlyph : null;
                var isInRow = IndexInRow(_arrowView) >= 0;
                if (_isArrowVisible && !isInRow)
                    _row.Children.Add(_arrowView);
                else if (!_isArrowVisible && isInRow)
                    _row.Children.Remove(_arrowView);
            }
        }
    }

    public ContextMenuItem(ICanvas canvas)
    {
        ZIndex = 2;
        _selectedBackgroundColor= 0xFFE6E6E6;

        _arrowView = new TextView(canvas)
        {
            FontSize = 12,
            Width = 16,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        _iconView = new TextView(canvas)
        {
            FontSize = 14,
            Width = 16,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        _textView = new TextView(canvas)
        {
            VerticalTextAlignment = TextAlignment.Center,
        };

        _shortcutView = new TextView(canvas)
        {
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = _shortcutColor,
        };

        _labelView = _textView;
        _row = new FlexRowView
        {
            Gap = 6,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                _iconView,
                _labelView,
            }
        };

        _bg = new RectView
        {
            BackgroundColor = 0xFFDEDEDE,
            Children =
            {
                new PaddingView
                {
                    Padding = PaddingStyle.All(6),
                    Children = { _row },
                }
            }
        };

        AddChildToSelf(_bg);
    }
}
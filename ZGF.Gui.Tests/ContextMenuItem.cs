using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemData
{
    public string Text { get; set; }
}

public sealed class ContextMenuItem : View
{
    public StyleValue<uint> BackgroundColor
    {
        get => _bg.BackgroundColor;
        set => _bg.BackgroundColor = value;
    }
    
    private readonly RectView _bg;
    private readonly ImageView _arrowIcon;
    private readonly TextView _textView;
    
    public string? Text
    {
        get => _textView.Text;
        set => _textView.Text = value;
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
                    BackgroundColor = 0xFF9C9CCE;
                }
                else
                {
                    BackgroundColor = 0xFFDEDEDE;
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

        _arrowIcon = new ImageView
        {
            PreferredWidth = 20,
            PreferredHeight = 20,
            TintColor = 0x0
        };

        _textView = new TextView
        {
            VerticalTextAlignment = TextAlignment.Center,
        };

        var row = new RowView
        {
            Gap = 5,
            Children =
            {
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
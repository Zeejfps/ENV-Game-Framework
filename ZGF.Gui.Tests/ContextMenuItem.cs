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
    private readonly Image _arrowIcon;
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
                    BackgroundColor = 0x9C9CCE;
                }
                else
                {
                    BackgroundColor = 0xDEDEDE;
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
                    _arrowIcon.ImageUri = "Assets/Icons/arrow_right.png";
                }
                else
                {
                    _arrowIcon.ImageUri = null;
                }
            }
        }
    }
    
    public ContextMenuItem()
    {
        ZIndex = 2;

        _arrowIcon = new Image
        {
            PreferredWidth = 20,
            PreferredHeight = 20
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
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(6),
            Children =
            {
                row
            }
        };

        AddChildToSelf(_bg);
    }
}
namespace ZGF.Gui.Tests;

public sealed class MenuItem : View
{
    private readonly RectView _background;
    private readonly TextView _textView;

    public string? Text
    {
        get => _textView.Text;
        set => _textView.Text = value;
    }

    private bool _isDisabled;
    public bool IsDisabled
    {
        get => _isDisabled;
        set
        {
            if (SetField(ref _isDisabled, value))
            {
                if (_isDisabled)
                {
                    _textView.StyleClasses.Add("disabled");
                }
                else
                {
                    _textView.StyleClasses.Remove("disabled");
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
            if (SetField(ref _isSelected, value))
            {
                if (_isSelected)
                {
                    _background.BackgroundColor = 0x9C9CCE;
                }
                else
                {
                    _background.BackgroundColor = 0xFFDEDEDE;
                }
            }
        }
    }

    public MenuItem()
    {
        _textView = new TextView
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        
        _background = new RectView
        {
            BackgroundColor = 0xFFDEDEDE,
            Padding = PaddingStyle.All(3),
            Children =
            {
                _textView,
            }
        };
        
        AddChildToSelf(_background);
    }
}
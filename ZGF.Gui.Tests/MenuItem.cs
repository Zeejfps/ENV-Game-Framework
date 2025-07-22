namespace ZGF.Gui.Tests;

public sealed class MenuItem : Component
{
    private readonly Panel _background;
    private readonly Label _label;

    public string? Text
    {
        get => _label.Text;
        set => _label.Text = value;
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
                    _label.AddStyleClass("disabled");
                }
                else
                {
                    _label.RemoveStyleClass("disabled");
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
                    _background.BackgroundColor = 0xDEDEDE;
                }
            }
        }
    }

    public MenuItem()
    {
        _background = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(3)
        };
        
        _label = new Label
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        
        _background.Add(_label);
        Add(_background);
    }
}
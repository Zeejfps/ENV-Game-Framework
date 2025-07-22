using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemData
{
    public string Text { get; set; }
}

public sealed class ContextMenuItem : Component
{
    public StyleValue<uint> BackgroundColor
    {
        get => _bg.BackgroundColor;
        set => _bg.BackgroundColor = value;
    }
    
    private readonly Panel _bg;
    private readonly Image _arrowIcon;
    private readonly Label _label;
    
    public string? Text
    {
        get => _label.Text;
        set => _label.Text = value;
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

        _bg = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(6)
        };

        _arrowIcon = new Image
        {
            PreferredWidth = 20,
            PreferredHeight = 20
        };

        _label = new Label
        {
            VerticalTextAlignment = TextAlignment.Center,
        };

        var row = new Row
        {
            _label,
            _arrowIcon,
        };
        row.Gap = 5;

        _bg.Add(row);
        Add(_bg);
    }
}
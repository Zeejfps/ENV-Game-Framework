using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowResizer : Component
{
    public StyleValue<uint> BackgroundColor
    {
        get => _background.BackgroundColor;
        set => _background.BackgroundColor = value;
    }
    
    private readonly Panel _background;
    
    public WindowResizer()
    {
        _background = new Panel
        {
            BackgroundColor = 0xCECECE,
            BorderSize = new BorderSizeStyle
            {
                Left = 1,
                Top = 1,
            },
            BorderColor = new BorderColorStyle
            {
                Left = 0xFFFFFF,
                Top = 0xFFFFFF,
            }
        };
        
        Add(_background);
    }

    protected override void OnLayoutSelf()
    {
        var width = 16f;
        var height = 16f;
        var left = RightConstraint - width - 6;
        var bottom = BottomConstraint + 6;
        Position = new RectF
        {
            Left = left,
            Bottom = bottom,
            Width = width,
            Height = height
        };
    }
}
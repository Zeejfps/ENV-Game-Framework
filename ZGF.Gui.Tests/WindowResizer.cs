using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowResizer : Component
{
    private readonly Window _window;

    private readonly Panel _background;
    
    public WindowResizer(Window window)
    {
        _window = window;
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

        IsInteractable = true;
    }

    protected override void OnMouseEnter()
    {
        _background.BackgroundColor = 0x9C9CCE;
        RequestFocus();
    }

    protected override void OnMouseExit()
    {
        _background.BackgroundColor = 0xCECECE;
        Blur();
    }

    protected override bool OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        _window.BringToFront();
        return base.OnMouseButtonStateChanged(e);
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